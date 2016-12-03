/// Contains a task which can be used to run dotnet CLI commands.
module Fake.DotNetCli

open Fake
open System
open System.IO
open System.Text
open Newtonsoft.Json.Linq

/// The dotnet command name
let commandName = "dotnet"

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
    try
        let processResult =
            ExecProcessAndReturnMessages (fun info ->  
              info.FileName <- commandName
              info.WorkingDirectory <- Environment.CurrentDirectory
              info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

        processResult.OK
    with _ -> false

/// DotNet parameters
[<CLIMutable>]
type CommandParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
}

let private DefaultCommandParams : CommandParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    TimeOut = TimeSpan.FromMinutes 30.
}

/// Runs a dotnet command.
/// ## Parameters
///
///  - `setCommandParams` - Function used to overwrite the default parameters.
///  - `args` - command and additional arguments.
///
/// ## Sample
///
///     DotNetCli.RunCommand
///         (fun p -> 
///              { p with 
///                   TimeOut = TimeSpan.FromMinutes 10. })
///         "restore"
let RunCommand (setCommandParams: CommandParams -> CommandParams) args =
    use __ = traceStartTaskUsing "DotNet" ""

    let parameters = setCommandParams DefaultCommandParams

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "dotnet command failed on %s" args

/// DotNet restore parameters
[<CLIMutable>]
type RestoreParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string
    
    /// Project (optional).
    Project: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// Whether to use the NuGet cache.
    NoCache : bool

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultRestoreParams : RestoreParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    NoCache = false
    Project = ""
    TimeOut = TimeSpan.FromMinutes 30.
    AdditionalArgs = []
}

/// Runs the dotnet "restore" command.
/// ## Parameters
///
///  - `setRestoreParams` - Function used to overwrite the restore default parameters.
///
/// ## Sample
///
///     DotNetCli.Restore 
///         (fun p -> 
///              { p with 
///                   NoCache = true })
let Restore (setRestoreParams: RestoreParams -> RestoreParams) =
    use __ = traceStartTaskUsing "DotNet.Restore" ""

    let parameters = setRestoreParams DefaultRestoreParams
    let args =
        new StringBuilder()
        |> append "restore"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrue parameters.NoCache "--no-cache"
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Restore failed on %s" args

/// DotNet build parameters
[<CLIMutable>]
type BuildParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan

    /// Project (optional).
    Project: string
    
    /// The build configuration.
    Configuration : string

    /// Allows to build for a specific framework
    Framework : string

    /// Allows to build for a specific runtime
    Runtime : string

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultBuildParams : BuildParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Framework = ""
    Runtime = ""
    Project = ""
    AdditionalArgs = []
}

/// Runs the dotnet "build" command.
/// ## Parameters
///
///  - `setBuildParams` - Function used to overwrite the build default parameters.
///
/// ## Sample
///
///     DotNetCli.Build
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
let Build (setBuildParams: BuildParams -> BuildParams) =
    use __ = traceStartTaskUsing "DotNet.Build" ""

    let parameters = setBuildParams DefaultBuildParams
    let args =
        new StringBuilder()
        |> append "build"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Build failed on %s" args


/// DotNet test parameters
[<CLIMutable>]
type TestParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan

    /// Project (optional).
    Project: string    
    
    /// The build configuration.
    Configuration : string

    /// Allows to test a specific framework
    Framework : string

    /// Allows to test a specific runtime
    Runtime : string

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultTestParams : TestParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Framework = ""
    Project = ""
    Runtime = ""
    AdditionalArgs = []
}

/// Runs the dotnet "test" command.
/// ## Parameters
///
///  - `setTestParams` - Function used to overwrite the test default parameters.
///
/// ## Sample
///
///     DotNetCli.Test
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
let Test (setTestParams: TestParams -> TestParams) =
    use __ = traceStartTaskUsing "DotNet.Test" ""

    let parameters = setTestParams DefaultTestParams
    let args =
        new StringBuilder()
        |> append "test"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Test failed on %s" args


/// DotNet pack parameters
[<CLIMutable>]
type PackParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Optional output path.
    OutputPath: string

    /// Optional version suffix.
    VersionSuffix: string

    /// Project (optional).
    Project: string    

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// The build configuration.
    Configuration : string

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultPackParams : PackParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    OutputPath = ""
    VersionSuffix = ""
    Project = ""
    TimeOut = TimeSpan.FromMinutes 30.
    AdditionalArgs = []
}

/// Runs the dotnet "pack" command.
/// ## Parameters
///
///  - `setPackParams` - Function used to overwrite the pack default parameters.
///
/// ## Sample
///
///     DotNetCli.Pack
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
let Pack (setPackParams: PackParams -> PackParams) =
    use __ = traceStartTaskUsing "DotNet.Pack" ""

    let parameters = setPackParams DefaultPackParams
    let args =
        new StringBuilder()
        |> append "pack"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.OutputPath) (sprintf "--output %s"  parameters.OutputPath)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.VersionSuffix) (sprintf "--version-suffix %s"  parameters.VersionSuffix)
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Pack failed on %s" args

/// DotNet publish parameters
[<CLIMutable>]
type PublishParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan

    /// Project (optional).
    Project: string
    
    /// The build configuration.
    Configuration : string

    /// Allows to publish to a specific framework
    Framework : string

    /// Allows to test a specific runtime
    Runtime : string

    /// Optional version suffix.
    VersionSuffix: string

    /// Optional outputh path
    Output : string

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultPublishParams : PublishParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Project = ""
    Framework = ""
    Runtime = ""
    VersionSuffix = ""
    Output = ""
    AdditionalArgs = []
}

/// Runs the dotnet "publish" command.
/// ## Parameters
///
///  - `setPublishParams` - Function used to overwrite the publish default parameters.
///
/// ## Sample
///
///     DotNetCli.Publish
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
let Publish (setPublishParams: PublishParams -> PublishParams) =
    use __ = traceStartTaskUsing "DotNet.Publish" ""

    let parameters = setPublishParams DefaultPublishParams
    let args =
        new StringBuilder()
        |> append "publish"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Output) (sprintf "--output %s"  parameters.Output)     
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.VersionSuffix) (sprintf "--version-suffix %s"  parameters.VersionSuffix)           
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Test Publish on %s" args



/// Sets version in project.json
let SetVersionInProjectJson (version:string) fileName = 
    use __ = traceStartTaskUsing "DotNet.SetVersion" fileName
    let original = File.ReadAllText fileName
    let p = JObject.Parse(original)
    p.["version"] <- JValue version
    let newText = p.ToString()
    if newText <> original then
        File.WriteAllText(fileName,newText)