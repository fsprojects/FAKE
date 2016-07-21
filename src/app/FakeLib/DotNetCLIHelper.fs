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

/// DotNet Restore parameters
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
    traceStartTask "DotNetRestore" ""

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
        traceEndTask "DotNetRestore" ""