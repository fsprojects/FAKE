[<AutoOpen>]
module Fake.OctoTools

    open Fake
    open System
    open System.Reflection
    open Microsoft.FSharp.Reflection


    // ************************************************************************
    // TYPES

    /// Octo.exe server options
    type OctoServerOptions = { Server: string; ApiKey: string }
    
    /// Options for creating a new release
    type CreateReleaseOptions = {
        Project                 : string
        Version                 : string
        PackageVersion          : string
        PackageVersionOverride  : string option
        PackagesFolder          : string option
        ReleaseNotes            : string
        ReleaseNotesFile        : string
    }

    /// Options for deploying a release to an environment
    type DeployReleaseOptions = {
        Project                     : string
        DeployTo                    : string
        Version                     : string
        Force                       : bool
        WaitForDeployment           : bool
        DeploymentTimeout           : System.TimeSpan option
        DeploymentCheckSleepCycle   : System.TimeSpan option
    }

    /// Options for deleting a range of releases in a project
    type DeleteReleaseOptions = {
        Project     : string
        MinVersion  : string
        MaxVersion  : string
    }

    /// DU for selecting one command
    type OctoCommand = 
        | CreateRelease of CreateReleaseOptions * DeployReleaseOptions option
        | DeployRelease of DeployReleaseOptions
        | DeleteRelease of DeleteReleaseOptions
        | ListEnvironments

    /// Complete Octo.exe CLI params
    type OctoParams = {
        ToolName            : string
        ToolPath            : string
        WorkingDirectory    : string
        Command             : OctoCommand
        Server              : OctoServerOptions
        Timeout             : TimeSpan
    }

    // ************************************************************************
    // DEFAULT OPTIONS

    /// Default server options.
    let serverOptions = { Server = ""; ApiKey = ""; }

    /// Default options for 'CreateRelease'
    let releaseOptions = {
        Project = ""; Version = ""; PackageVersion = ""; PackageVersionOverride = None; 
        PackagesFolder = None; ReleaseNotes = ""; ReleaseNotesFile = ""; 
    }

    /// Default options for 'DeployRelease'
    let deployOptions = {
        Project = ""; DeployTo = ""; Version = ""; Force = false; WaitForDeployment = false; 
        DeploymentTimeout = None; DeploymentCheckSleepCycle = None;
    }

    /// Default options for 'DeleteReleases'
    let deleteOptions = {
        Project = ""; MinVersion = ""; MaxVersion = "";
    }

    /// Default parameters to call octo.exe.
    let octoParams = {
        ToolName = "Octo.exe"; ToolPath = ""; Command = ListEnvironments;
        Server = serverOptions; Timeout = TimeSpan.MaxValue; WorkingDirectory = "";
    }

    // ************************************************************************
    // HELPER FUNCTIONS

    let private commandLine opts =     
        let recordValueAsString opts field = 
            if Reflection.FSharpValue.GetRecordField(opts, field) = null then ""
            else Reflection.FSharpValue.GetRecordField(opts, field).ToString()     
           
        Reflection.FSharpType.GetRecordFields(opts.GetType())
        |> Array.map (fun field -> field.Name, recordValueAsString opts field)
        |> Array.fold (fun s (o, v) -> 
            if isNullOrEmpty v then s
            else s + (sprintf " --%s=\"%s\"" (o.ToLower()) v)
        ) ""

    let private commandString command =       
        match command with
        | CreateRelease (releaseOpts, None) ->        
            releaseOpts |> commandLine |> (sprintf " create-release%s")
        | CreateRelease (releaseOpts, Some (deployOpts)) ->
            ((releaseOpts |> commandLine) + (deployOpts |> commandLine)) |> (sprintf " create-release%s")
        | DeployRelease opts ->
            opts |> commandLine |> (sprintf " deploy-release%s")
        | DeleteRelease opts ->
            opts |> commandLine |> (sprintf " delete-releases%s")
        | ListEnvironments -> 
            " list-environments"

    // ************************************************************************
    // OCTO FUNCTION

    /// Calls the Octo.exe CLI.
    let Octo setParams =     
        
        let octoParams = setParams(octoParams)
        let command = (octoParams.Command.ToString())
        let tool = octoParams.ToolPath @@ octoParams.ToolName
        let args = (commandString octoParams.Command) + (commandLine octoParams.Server)    

        traceStartTask "Octo " command       
        trace (tool + args)
        
        let result = 
            execProcessAndReturnExitCode (fun info ->
                info.Arguments <- args
                info.WorkingDirectory <- octoParams.WorkingDirectory
                info.FileName <- tool
            ) octoParams.Timeout

        match result with
            | 0 -> traceEndTask "Octo " command
            | _ -> failwithf "Octo %s failed. Process finished with exit code %i" command result