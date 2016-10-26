/// Allows to execute processes as unit tests.
module Fake.ProcessTestRunner

open System
open System.IO
open System.Text

/// The ProcessTestRunner parameter type.
type ProcessTestRunnerParams = 
    { /// The working directory (optional).
      WorkingDir : string
      /// If the timeout is reached the xUnit task will be killed. Default is 5 minutes.
      TimeOut : TimeSpan
      /// Option which allows to specify if a test runner error should break the build.
      ErrorLevel : TestRunnerErrorLevel }

/// The ProcessTestRunner defaults.
let ProcessTestRunnerDefaults = 
    { WorkingDir = null
      TimeOut = TimeSpan.FromMinutes 5.
      ErrorLevel = TestRunnerErrorLevel.Error }

/// Runs the given process and returns the process result.
let RunConsoleTest parameters fileName args = 
    let taskName = sprintf "Run_%s" fileName
    let result = ref None
    try 
        let exitCode = 
            ExecProcess (fun info -> 
                info.WorkingDirectory <- parameters.WorkingDir
                info.FileName <- fileName
                info.Arguments <- args) parameters.TimeOut
        if exitCode <> 0 then result := Some(sprintf "Exit code %d" exitCode)
    with exn -> 
        let message = ref exn.Message
        if exn.InnerException <> null then message := !message + Environment.NewLine + exn.InnerException.Message
        result := Some(!message)
    !result

/// Runs the given processes and returns the process result messages.
let runConsoleTests parameters processes = 
    processes
    |> Seq.map (fun (fileName, args) -> 
           fileName, args, 
           match RunConsoleTest parameters fileName args with
           | Some m' -> m'
           | _ -> "")
    |> Seq.filter (fun (_, _, m) -> m <> "")

/// Runs the given processes and returns the process results.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default parameter value.
///  - `processes` - Sequence of one or more filenames and arguments to run.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         ["process1.exe","argument1"
///          "process2.exe","argument2"]
///           |> RunConsoleTests (fun p -> {p with TimeOut = TimeSpan.FromMinutes 1. })
///     )
let RunConsoleTests setParams processes = 
    traceStartTask "RunConsoleTests" ""
    let parameters = setParams ProcessTestRunnerDefaults
    
    let execute() = 
        runConsoleTests parameters processes
        |> Seq.map (fun (f, a, m) -> sprintf "Process %s %s terminated with %s" f a m)
        |> toLines
    match parameters.ErrorLevel with
    | TestRunnerErrorLevel.DontFailBuild -> execute() |> trace
    | TestRunnerErrorLevel.Error -> 
        let msg = execute()
        if msg <> "" then failwith msg
    | TestRunnerErrorLevel.FailOnFirstError -> 
        for fileName, args in processes do
            match RunConsoleTest parameters fileName args with
            | Some error -> failwithf "Process %s %s terminated with %s" fileName args error
            | _ -> ()
    traceEndTask "RunConsoleTests" ""
