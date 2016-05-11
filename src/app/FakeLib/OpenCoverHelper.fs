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
      WorkingDir : string 
      /// This option is used to merge the coverage results for an assembly regardless of where it was loaded 
      /// assuming the assembly has the same file-hash in each location. 
      MergeByHash : bool
      /// This options is used to add additional optional arguments, could be somthing like "-returntargetcode "
      OptionalArguments : string }

/// OpenCover default parameters
let OpenCoverDefaults = 
    { ExePath = if isMono then String.Empty else environVar "LOCALAPPDATA" @@ "Apps" @@ "OpenCover" @@ "OpenCover.Console.exe"
      TestRunnerExePath = if isMono then String.Empty else ProgramFiles @@ "NUnit" @@ "bin" @@ "nunit-console.exe"
      Output = String.Empty
      Register = Manual
      Filter = String.Empty
      TimeOut = TimeSpan.FromMinutes 5.
      WorkingDir = currentDirectory
      MergeByHash = false
      OptionalArguments = String.Empty }

/// Builds the command line arguments from the given parameter record
/// [omit]
let buildOpenCoverArgs param targetArgs = 
        let quote arg = sprintf "\"%s\"" arg

        new StringBuilder()
        |> appendWithoutQuotes (sprintf "-target:%s" (quote (param.TestRunnerExePath |> FullName)))
        |> appendWithoutQuotes (sprintf "-targetargs:%s" (quote targetArgs))
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty param.Output) (sprintf "-output:%s" (quote param.Output))
        |> appendWithoutQuotes (match param.Register with
                                | Manual -> String.Empty
                                | Register -> "-register "
                                | RegisterUser -> "-register:user")
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty param.Filter) (sprintf "-filter:%s" (quote param.Filter))
        |> appendIfTrueWithoutQuotes param.MergeByHash "-mergebyhash"
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty param.OptionalArguments) param.OptionalArguments
        |> toText
    

/// Runs OpenCover on a group of assemblies.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default OpenCover parameters.
///  - `targetArgs` - Test runner arguments.
///
/// ## Sample
///
///      OpenCover (fun p -> { p with TestRunnerExePath = "./Tools/NUnit/nunit-console.exe" }) 
///         "project-file.nunit /config:Release /noshadow /xml:artifacts/nunit.xml /framework:net-4.0"
let OpenCover setParams targetArgs = 
    let taskName = "OpenCover"
    let description = "Gathering coverage statistics"
    traceStartTask taskName description
    let param = setParams OpenCoverDefaults
    
    let processArgs = buildOpenCoverArgs param targetArgs
    tracefn "OpenCover command\n%s %s" param.ExePath processArgs
    let ok = 
        execProcess (fun info -> 
            info.FileName <- param.ExePath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- processArgs) param.TimeOut
    if not ok then failwithf "OpenCover reported errors."
    traceEndTask taskName description