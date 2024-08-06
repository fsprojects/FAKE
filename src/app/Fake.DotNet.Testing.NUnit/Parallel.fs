namespace Fake.DotNet.Testing.NUnit

/// <summary>
/// Contains tasks to run <a href="http://www.nunit.org/">NUnit</a> unit tests in parallel.
/// </summary>
module Parallel =

    open Fake.Testing.Common
    open Fake.IO.FileSystemOperators
    open Fake.Core
    open System
    open System.IO
    open System.Text
    open System.Linq
    open Fake.DotNet.Testing.NUnit.Xml
    open Fake.DotNet.Testing.NUnit.Common
    open System.Xml.Linq

    type private NUnitParallelResult =
        { AssemblyName: string
          ErrorOut: StringBuilder
          StandardOut: StringBuilder
          ReturnCode: int
          OutputFile: string }

    type private AggFailedResult =
        { WorseReturnCode: int
          Messages: string list }

        static member Empty = { WorseReturnCode = Int32.MaxValue; Messages = [] }

    /// <summary>
    /// Runs NUnit in parallel on a group of assemblies.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default <c>NUnitParams</c> value.</param>
    /// <param name="assemblies">Sequence of one or more assemblies containing NUnit unit tests.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "Test" (fun _ ->
    ///         !! (testDir + @"\Test.*.dll")
    ///           |> NUnitParallel (fun p -> { p with ErrorLevel = DontFailBuild })
    ///     )
    /// </code>
    /// </example>
    let run (setParams: NUnitParams -> NUnitParams) (assemblies: string seq) =
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "NUnitParallel" details
        let parameters = NUnitDefaults |> setParams
        let tool = parameters.ToolPath @@ parameters.ToolName

        let runSingleAssembly parameters name outputFile =
            let args = buildArgs { parameters with OutputFile = outputFile } [ name ]
            let errout = StringBuilder()
            let stdout = StringBuilder()
            Trace.tracefn "Run NUnit tests from %s." name
            let stopwatch = System.Diagnostics.Stopwatch.StartNew()

            let errorF (msg: string) = errout.Append(msg) |> ignore

            let messageF (msg: string) = stdout.Append(msg) |> ignore

            let processResult =
                CreateProcess.fromRawCommandLine tool args
                |> CreateProcess.withWorkingDirectory (getWorkingDir parameters)
                |> CreateProcess.withTimeout parameters.TimeOut
                |> CreateProcess.withFramework
                |> CreateProcess.redirectOutput
                |> CreateProcess.withOutputEventsNotNull messageF errorF
                |> Proc.run

            stopwatch.Stop()

            Trace.tracefn
                "NUnit tests from %s finished in %O with result code %d."
                name
                stopwatch.Elapsed
                processResult.ExitCode

            { AssemblyName = name
              ErrorOut = errout
              StandardOut = stdout
              ReturnCode = processResult.ExitCode
              OutputFile = outputFile }

        let before = Process.shouldEnableProcessTracing ()

        let testRunResults =
            try
                Process.setEnableProcessTracing false

                assemblies
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .Select(fun asm -> runSingleAssembly parameters asm (Path.GetTempFileName()))
                |> Seq.toList
            finally
                Process.setEnableProcessTracing before

        // Read all valid results
        let docs =
            testRunResults
            |> List.filter (fun x -> x.ReturnCode >= 0)
            |> List.map (fun x -> x.OutputFile)
            |> List.map (File.ReadAllText >> XDocument.Parse)

        match docs with
        | [] -> ()
        | _ ->
            File.WriteAllText(
                getWorkingDir parameters @@ parameters.OutputFile,
                sprintf "%O" (NUnitMerge.mergeXDocs docs)
            )
        //sendTeamCityNUnitImport parameters.OutputFile
        // Make sure we delete the temp files
        testRunResults |> List.map (fun x -> x.OutputFile) |> List.iter File.Delete
        // Report results
        let formatErrorMessages r =
            [ if r.ReturnCode < 0 then
                  yield
                      sprintf
                          "NUnit test run for %s returned error code %d, output to stderr was:"
                          r.AssemblyName
                          r.ReturnCode

                  yield sprintf "%O" r.ErrorOut
              else
                  yield
                      sprintf
                          "NUnit test run for %s reported failed tests, check output file %s for details."
                          r.AssemblyName
                          parameters.OutputFile ]

        match List.filter (fun r -> r.ReturnCode <> 0) testRunResults with
        | [] -> ()
        | failedResults ->
            let aggResult =
                List.fold
                    (fun acc x ->
                        { acc with
                            WorseReturnCode = min acc.WorseReturnCode x.ReturnCode
                            Messages = acc.Messages @ formatErrorMessages x })
                    AggFailedResult.Empty
                    failedResults

            let fail () =
                List.iter Trace.traceError aggResult.Messages

                raise (
                    FailedTestsException(
                        sprintf
                            "NUnitParallel test runs failed (%d of %d assemblies are failed)."
                            (List.length failedResults)
                            (List.length testRunResults)
                    )
                )

            match parameters.ErrorLevel with
            | DontFailBuild ->
                match aggResult.WorseReturnCode with
                | OK
                | TestsFailed -> ()
                | _ -> fail ()
            | Error
            | FailOnFirstError ->
                match aggResult.WorseReturnCode with
                | OK -> ()
                | _ -> fail ()

        __.MarkSuccess()
