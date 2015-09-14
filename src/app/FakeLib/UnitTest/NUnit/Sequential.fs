[<AutoOpen>]
/// Contains tasks to run [NUnit](http://www.nunit.org/) unit tests.
module Fake.NUnitSequential

/// Runs NUnit on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default [NUnitParams](fake-nunitcommon-nunitparams.html) value.
///  - `assemblies` - Sequence of one or more assemblies containing NUnit unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\Test.*.dll") 
///           |> NUnit (fun p -> { p with ErrorLevel = DontFailBuild })
///     )
let NUnit (setParams : NUnitParams -> NUnitParams) (assemblies : string seq) =
    let details = assemblies |> separated ", "
    traceStartTask "NUnit" details
    let parameters = NUnitDefaults |> setParams
    let assemblies = assemblies |> Seq.toArray
    if Array.isEmpty assemblies then failwith "NUnit: cannot run tests (the assembly list is empty)."
    let tool = parameters.ToolPath @@ parameters.ToolName
    let args = buildNUnitdArgs parameters assemblies
    trace (tool + " " + args)
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- tool
            info.WorkingDirectory <- getWorkingDir parameters
            info.Arguments <- args) parameters.TimeOut
    sendTeamCityNUnitImport parameters.OutputFile
    let errorDescription error = 
        match error with
        | OK -> "OK"
        | TestsFailed -> sprintf "NUnit test failed (%d)." error
        | FatalError x -> sprintf "NUnit test failed. Process finished with exit code %s (%d)." x error
    match parameters.ErrorLevel with
    | DontFailBuild -> 
        match result with
        | OK | TestsFailed -> traceEndTask "NUnit" details
        | _ -> raise (FailedTestsException(errorDescription result))
    | Error | FailOnFirstError -> 
        match result with
        | OK -> traceEndTask "NUnit" details
        | _ -> raise (FailedTestsException(errorDescription result))
