/// Contains a task which can be used to run [OpenCover](https://github.com/sawilde/opencover) on .NET assemblies.
module Fake.OpenCoverHelper

open System
open System.Text

type RegisterType = 
    | Manual
    | Register
    | RegisterUser

/// OpenCover parameters, for more details see: https://github.com/OpenCover/opencover/wiki/Usage#console-application-usage.
type OpenCoverParams = 
    { /// (Required) Path to the OpenCover console application
      ExePath : string
      /// (Required) Path to the NUnit/XUnit console runner
      TestRunnerExePath : string
      /// The location and name of the output xml file. 
      /// If no value is supplied then the current directory 
      /// will be used and the output filename will be results.xml.
      Output : string
      /// Use this to register and de-register the code coverage profiler.
      Register : RegisterType
      /// A list of filters to apply to selectively include or exclude assemblies and classes from coverage results.
      Filter : string
      /// The timeout for the OpenCover process.
      TimeOut : TimeSpan
      /// The directory where the OpenCover process will be started.
      WorkingDir : string }

/// OpenCover default parameters
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
///
/// ## Sample
///
///  OpenCover (fun p -> { p with TestRunnerExePath = "./Tools/NUnit/nunit-console.exe" }) 
///     "project-file.nunit /config:Release /noshadow /xml:artifacts/nunit.xml /framework:net-4.0"
let OpenCover setParams targetArgs = 
    let taskName = "OpenCover"
    let description = "Gathering coverage statistics"
    traceStartTask taskName description
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
    traceEndTask taskName description