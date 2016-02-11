[<AutoOpen>]
/// Contains tasks to run [NUnit](http://www.nunit.org/) unit tests in parallel.
module Fake.NUnitParallel

open System
open System.IO
open System.Text
open System.Xml.Linq
open System.Linq

type private NUnitParallelResult = 
    { AssemblyName : string
      ErrorOut : StringBuilder
      StandardOut : StringBuilder
      ReturnCode : int
      OutputFile : string }

type private AggFailedResult = 
    { WorseReturnCode : int
      Messages : string list }
    static member Empty = 
        { WorseReturnCode = Int32.MaxValue
          Messages = [] }

/// Runs NUnit in parallel on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default [NUnitParams](fake-nunitcommon-nunitparams.html) value.
///  - `assemblies` - Sequence of one or more assemblies containing NUnit unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\Test.*.dll") 
///           |> NUnitParallel (fun p -> { p with ErrorLevel = DontFailBuild })
///     )
let NUnitParallel (setParams : NUnitParams -> NUnitParams) (assemblies : string seq) = 
    let details = assemblies |> separated ", "
    traceStartTask "NUnitParallel" details
    let parameters = NUnitDefaults |> setParams
    let tool = parameters.ToolPath @@ parameters.ToolName
    
    let runSingleAssembly parameters name outputFile = 
        let args = buildNUnitdArgs { parameters with OutputFile = outputFile } [ name ]
        let errout = StringBuilder()
        let stdout = StringBuilder()
        tracefn "Run NUnit tests from %s." name
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        
        let result = 
            ExecProcessWithLambdas (fun info -> 
                info.FileName <- tool
                info.WorkingDirectory <- getWorkingDir parameters
                info.Arguments <- args) parameters.TimeOut true (fun e -> errout.Append(e) |> ignore) 
                (fun s -> stdout.Append(s) |> ignore)
        stopwatch.Stop()
        tracefn "NUnit tests from %s finished in %O with result code %d." name stopwatch.Elapsed result
        { AssemblyName = name
          ErrorOut = errout
          StandardOut = stdout
          ReturnCode = result
          OutputFile = outputFile }
    enableProcessTracing <- false
    let testRunResults = 
        assemblies.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)
                  .Select(fun asm -> runSingleAssembly parameters asm (Path.GetTempFileName())) |> Seq.toList
    enableProcessTracing <- true
    // Read all valid results
    let docs = 
        testRunResults
        |> List.filter (fun x -> x.ReturnCode >= 0)
        |> List.map (fun x -> x.OutputFile)
        |> List.map (File.ReadAllText >> XDocument.Parse)
    match docs with
    | [] -> ()
    | _ -> 
        File.WriteAllText(getWorkingDir parameters @@ parameters.OutputFile, sprintf "%O" (NUnitMerge.mergeXDocs docs))
        sendTeamCityNUnitImport parameters.OutputFile
    // Make sure we delete the temp files
    testRunResults
    |> List.map (fun x -> x.OutputFile)
    |> List.iter File.Delete
    // Report results
    let formatErrorMessages r = 
        [ if r.ReturnCode < 0 then 
              yield sprintf "NUnit test run for %s returned error code %d, output to stderr was:" r.AssemblyName 
                        r.ReturnCode
              yield sprintf "%O" r.ErrorOut
          else 
              yield sprintf "NUnit test run for %s reported failed tests, check output file %s for details." 
                        r.AssemblyName parameters.OutputFile ]
    match List.filter (fun r -> r.ReturnCode <> 0) testRunResults with
    | [] -> traceEndTask "NUnitParallel" details
    | failedResults -> 
        let aggResult = 
            List.fold (fun acc x -> 
                { acc with WorseReturnCode = min acc.WorseReturnCode x.ReturnCode
                           Messages = acc.Messages @ formatErrorMessages x }) AggFailedResult.Empty failedResults

        let fail() = 
            List.iter traceError aggResult.Messages
            raise (FailedTestsException (sprintf "NUnitParallel test runs failed (%d of %d assemblies are failed)." 
                    (List.length failedResults) (List.length testRunResults)))

        match parameters.ErrorLevel with
        | DontFailBuild -> 
            match aggResult.WorseReturnCode with
            | OK | TestsFailed -> traceEndTask "NUnit" details
            | _ -> fail()
        | Error | FailOnFirstError -> 
            match aggResult.WorseReturnCode with
            | OK -> traceEndTask "NUnit" details
            | _ -> fail()
