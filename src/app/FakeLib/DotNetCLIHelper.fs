/// Contains a task which can be used to run dotnet CLI commands.
module Fake.DotNet

open Fake
open System
open System.Text

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
}

let private DefaultRestoreParams : RestoreParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    NoCache = false
    TimeOut = TimeSpan.FromMinutes 30.
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
            |> toText

        if 0 <> ExecProcess (fun info ->  
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
        then
            failwithf "Restore failed on %s" args
    finally
        traceEndTask "DotNet.Restore" ""

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
}

let private DefaultTestParams : TestParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
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
                |> appendIfNotNullOrEmpty parameters.Configuration "--configuration "
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
                |> appendIfNotNullOrEmpty parameters.Configuration "--configuration "
                |> toText

            if 0 <> ExecProcess (fun info ->  
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
            then
                failwithf "Pack failed on %s" args
    finally
        traceEndTask "DotNet.pack" ""