/// Contains a task which can be used to run dotnet CLI commands.
module Fake.DotNet

open Fake
open System
open System.IO
open System.Text
open Newtonsoft.Json.Linq

/// The dotnet command name
let commandName = "dotnet"

/// DotNet logger verbosity
type Verbosity =
| Debug
| Verbose
| Information
| Minimal
| Warning
| Error

/// The default log verbosity
let DefaultVerbosity = Minimal

let private verbosityString v =
    match v with
    | Debug -> "Debug"
    | Verbose -> "Verbose"
    | Information -> "Information"
    | Minimal -> "Minimal"
    | Warning -> "Warning"
    | Error -> "Error"

/// Gets the installed dotnet version
let getVersion() = 
    let processResult = 
        ExecProcessAndReturnMessages (fun info ->  
          info.FileName <- commandName
          info.WorkingDirectory <- Environment.CurrentDirectory
          info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

    processResult.Messages |> separated ""

/// Checks wether the dotnet CLI is installed
let isInstalled() =
    let processResult = 
        ExecProcessAndReturnMessages (fun info ->  
          info.FileName <- commandName
          info.WorkingDirectory <- Environment.CurrentDirectory
          info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

    processResult.OK

/// DotNet restore parameters
type RestoreParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// Whether to use the NuGet cache.
    NoCache : bool

    /// Log Verbosity.
    Verbosity : Verbosity
}

let private DefaultRestoreParams : RestoreParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    NoCache = false
    TimeOut = TimeSpan.FromMinutes 30.
    Verbosity = DefaultVerbosity
}

/// Runs the dotnet "restore" command.
/// ## Parameters
///
///  - `setRestoreParams` - Function used to overwrite the restore default parameters.
///
/// ## Sample
///
///     DotNet.Restore 
///         (fun p -> 
///              { p with 
///                   NoCache = true })
let Restore (setRestoreParams: RestoreParams -> RestoreParams) =
    traceStartTask "DotNet.Restore" ""

    try
        let parameters = setRestoreParams DefaultRestoreParams
        let args =
            new StringBuilder()
            |> append "restore"
            |> appendIfTrue parameters.NoCache "--no-cache"
            |> appendWithoutQuotes (sprintf "--verbosity %s" (verbosityString parameters.Verbosity))
            |> toText

        if 0 <> ExecProcess (fun info ->  
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
        then
            failwithf "Restore failed on %s" args
    finally
        traceEndTask "DotNet.Restore" ""

/// DotNet build parameters
type BuildParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// The build configuration.
    Configuration : string

    /// Allows to build for a specific framework
    Framework : string

    /// Allows to build for a specific runtime
    Runtime : string
}

let private DefaultBuildParams : BuildParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Framework = ""
    Runtime = ""
}

/// Runs the dotnet "build" command.
/// ## Parameters
///
///  - `setBuildParams` - Function used to overwrite the build default parameters.
///
/// ## Sample
///
///     !! "src/test/project.json"
///     |> DotNet.Build
///         (fun p -> 
///              { p with 
///                   Configuration = "Release" })
let Build (setBuildParams: BuildParams -> BuildParams) projects =
    traceStartTask "DotNet.Build" ""

    try
        for project in projects do
            let parameters = setBuildParams DefaultBuildParams
            let args =
                new StringBuilder()
                |> append "build"
                |> append project                
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
                |> toText

            if 0 <> ExecProcess (fun info ->  
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
            then
                failwithf "Build failed on %s" args
    finally
        traceEndTask "DotNet.Build" ""


/// DotNet test parameters
type TestParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// The build configuration.
    Configuration : string

    /// Allows to test a specific framework
    Framework : string

    /// Allows to test a specific runtime
    Runtime : string
}

let private DefaultTestParams : TestParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Framework = ""
    Runtime = ""
}

/// Runs the dotnet "test" command.
/// ## Parameters
///
///  - `setTestParams` - Function used to overwrite the test default parameters.
///
/// ## Sample
///
///     !! "src/test/project.json"
///     |> DotNet.Test
///         (fun p -> 
///              { p with 
///                   Configuration = "Release" })
let Test (setTestParams: TestParams -> TestParams) projects =
    traceStartTask "DotNet.Test" ""

    try
        for project in projects do
            let parameters = setTestParams DefaultTestParams
            let args =
                new StringBuilder()
                |> append "test"
                |> append project                
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
                |> toText

            if 0 <> ExecProcess (fun info ->  
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
            then
                failwithf "Test failed on %s" args
    finally
        traceEndTask "DotNet.Test" ""


/// DotNet pack parameters
type PackParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Optional output path.
    OutputPath: string

    /// Optional version suffix.
    VersionSuffix: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// The build configuration.
    Configuration : string
}

let private DefaultPackParams : PackParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    OutputPath = ""
    VersionSuffix = ""
    TimeOut = TimeSpan.FromMinutes 30.
}

/// Runs the dotnet "pack" command.
/// ## Parameters
///
///  - `setPackParams` - Function used to overwrite the pack default parameters.
///
/// ## Sample
///
///     !! "src/test/project.json"
///     |> DotNet.Pack
///         (fun p -> 
///              { p with 
///                   Configuration = "Release" })
let Pack (setPackParams: PackParams -> PackParams) projects =
    traceStartTask "DotNet.Pack" ""

    try
        for project in projects do
            let parameters = setPackParams DefaultPackParams
            let args =
                new StringBuilder()
                |> append "pack"
                |> append project
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.OutputPath) (sprintf "--output %s"  parameters.OutputPath)
                |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.VersionSuffix) (sprintf "--version-suffix %s"  parameters.VersionSuffix)
                |> toText

            if 0 <> ExecProcess (fun info ->  
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
            then
                failwithf "Pack failed on %s" args
    finally
        traceEndTask "DotNet.Pack" ""

/// Sets version in project.json
let SetVersionInProjectJson (version:string) fileName = 
    traceStartTask "DotNet.SetVersion" fileName
    try
        let original = File.ReadAllText fileName
        let p = JObject.Parse(original)
        p.["version"] <- JValue version
        let newText = p.ToString()
        if newText <> original then
            File.WriteAllText(fileName,newText)
    finally
        traceEndTask "DotNet.SetVersion" fileName