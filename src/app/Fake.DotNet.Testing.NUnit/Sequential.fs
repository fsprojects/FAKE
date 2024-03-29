namespace Fake.DotNet.Testing.NUnit

/// <summary>
/// Contains tasks to run <a href="http://www.nunit.org/">NUnit</a> unit tests.
/// </summary>
module Sequential =

    open Fake.Testing.Common
    open Fake.IO.FileSystemOperators
    open Fake.Core
    open Fake.DotNet.Testing.NUnit.Common

    /// <summary>
    /// Runs NUnit on a group of assemblies.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default <c>NUnitParams</c> value.</param>
    /// <param name="assemblies">Sequence of one or more assemblies containing NUnit unit tests.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "Test" (fun _ ->
    ///         !! (testDir + @"\Test.*.dll")
    ///           |> NUnit (fun p -> { p with ErrorLevel = DontFailBuild })
    ///     )
    /// </code>
    /// </example>
    let run (setParams: NUnitParams -> NUnitParams) (assemblies: string seq) =
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "NUnit" details
        let parameters = NUnitDefaults |> setParams
        let assemblies = assemblies |> Seq.toArray

        if Array.isEmpty assemblies then
            failwith "NUnit: cannot run tests (the assembly list is empty)."

        let tool = parameters.ToolPath @@ parameters.ToolName
        let args = buildArgs parameters assemblies
        Trace.trace (tool + " " + args)

        let processResult =
            CreateProcess.fromRawCommandLine tool args
            |> CreateProcess.withWorkingDirectory (getWorkingDir parameters)
            |> CreateProcess.withTimeout parameters.TimeOut
            |> CreateProcess.withFramework
            |> Proc.run

        let errorDescription error =
            match error with
            | OK -> "OK"
            | TestsFailed -> sprintf "NUnit test failed (%d)." error
            | FatalError x -> sprintf "NUnit test failed. Process finished with exit code %s (%d)." x error

        match parameters.ErrorLevel with
        | DontFailBuild ->
            match processResult.ExitCode with
            | OK
            | TestsFailed -> ()
            | _ -> raise (FailedTestsException(errorDescription processResult.ExitCode))
        | Error
        | FailOnFirstError ->
            match processResult.ExitCode with
            | OK -> ()
            | _ -> raise (FailedTestsException(errorDescription processResult.ExitCode))

        __.MarkSuccess()
