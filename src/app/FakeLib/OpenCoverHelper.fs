/// Contains a task which can be used to run [OpenCover](https://github.com/sawilde/opencover) on .NET assemblies.
module Fake.OpenCoverHelper

open System
open System.Text

type RegisterType = 
    | Manual
    | Register
    | RegisterUser

// Open Cover parameter type
type OpenCoverParams = 
    { ExePath : string
      TestRunnerExePath : string
      Output : string
      Register : RegisterType
      Filter : string
      TimeOut : TimeSpan
      WorkingDir : string }

// Open Cover default parameters
let OpenCoverDefaults = 
    { ExePath = environVar "LOCALAPPDATA" @@ "Apps" @@ "OpenCover" @@ "OpenCover.Console.exe"
      TestRunnerExePath = ProgramFiles @@ "NUnit" @@ "bin" @@ "nunit-console.exe"
      Output = String.Empty
      Register = Manual
      Filter = String.Empty
      TimeOut = TimeSpan.FromMinutes 5.
      WorkingDir = currentDirectory }

/// Runs OpenCover on a group of assemblies.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default OpenCover parameters.
///  - `targetArgs` - Test runner arguments.
let OpenCover setParams targetArgs = 
    let param = setParams OpenCoverDefaults
    
    let processArgs = 
        let args = ref (new StringBuilder())
        let append (s : string) = args := (!args).Append(s)
        let appendQuoted (s : string) = args := (!args).Append("\"").Append(s).Append("\" ")
        append "-target:"
        param.TestRunnerExePath
        |> FullName
        |> appendQuoted
        append "-targetargs:"
        appendQuoted targetArgs
        if param.Output <> String.Empty then 
            append "-output:"
            appendQuoted param.Output
        append (match param.Register with
                | Manual -> String.Empty
                | Register -> "-register "
                | RegisterUser -> "-register:user ")
        if param.Filter <> String.Empty then 
            append "-filter:"
            appendQuoted param.Filter
        (!args).ToString()
    tracefn "OpenCover command\n%s %s" param.ExePath processArgs
    let ok = 
        execProcess (fun info -> 
            info.FileName <- param.ExePath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- processArgs) param.TimeOut
    if not ok then failwithf "OpenCover reported errors."