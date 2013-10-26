[<AutoOpen>]
/// Contains tasks to run [NUnit](http://www.nunit.org/) unit tests in parallel.
module Fake.NUnitParallel

open System
open System.IO
open System.Text
open System.Xml.Linq

type private NUnitParallelResult = {
    AssemblyName : string
    ErrorOut : StringBuilder
    StandardOut : StringBuilder
    ReturnCode : int
    OutputFile : string }

/// Runs NUnit in parallel on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NUnitParams value.
///  - `assemblies` - Sequence of one or more assemblies containing NUnit unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\Test.*.dll") 
///           |> NUnitParallel (fun p -> { p with ErrorLevel = DontFailBuild })
///     )
let NUnitParallel (setParams: NUnitParams -> NUnitParams) (assemblies: string seq) =
    let details = assemblies |> separated ", "
    traceStartTask "NUnitParallel" details
    let parameters = NUnitDefaults |> setParams
    let assemblies = assemblies |> Seq.toArray
    let tool = parameters.ToolPath @@ parameters.ToolName

    let runSingleAssembly parameters (name, outputFile) =
        let args = commandLineBuilder { parameters with OutputFile = outputFile } [name]
        let errout = StringBuilder()
        let stdout = StringBuilder()
        let result =
            ExecProcessWithLambdas (fun info ->  
                info.FileName <- tool
                info.WorkingDirectory <- getWorkingDir parameters
                info.Arguments <- args) 
                parameters.TimeOut
                true
                (fun e -> errout.Append(e) |> ignore)                
                (fun s -> stdout.Append(s) |> ignore)
        { AssemblyName = name; ErrorOut = errout; StandardOut = stdout; ReturnCode = result; OutputFile = outputFile }

    enableProcessTracing <- false
    let testRunResults =
        assemblies
        |> Seq.map (fun asm -> asm, Path.GetTempFileName())
        |> doParallelWithThrottle Environment.ProcessorCount (runSingleAssembly parameters)
        |> Seq.toList
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
        File.WriteAllText (getWorkingDir parameters @@ parameters.OutputFile, sprintf "%O" (NUnitMerge.mergeXDocs docs))
        sendTeamCityNUnitImport (getWorkingDir parameters @@ parameters.OutputFile)

    // Make sure we delete the temp files
    testRunResults 
    |> List.map (fun x -> x.OutputFile)
    |> List.iter File.Delete

    // Deal with errors
    match testRunResults |> List.filter (fun r -> r.ReturnCode <> 0) with
    | [] -> traceEndTask "NUnitParallel" details
    | xs -> 
        xs 
        |> List.collect (function
                | r when r.ReturnCode < 0 ->
                        [ sprintf "NUnit test run for %s returned error code %d, output to stderr was:" r.AssemblyName r.ReturnCode
                          sprintf "%O" r.ErrorOut ]
                | r -> [ sprintf "NUnit test run for %s reported failed tests, check outputfile %s for details." r.AssemblyName parameters.OutputFile ])
        |> List.iter traceError
        failwith "NUnitParallel test runs failed."
