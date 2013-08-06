[<AutoOpen>]
module Fake.OctoTools

    open Fake
    open System

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

    let optionalStringParam p o = 
        match o with
        | Some s -> sprintf " --%s=\"%s\"" p s
        | None -> ""

    let optionalObjParam p o = 
        match o with
        | Some x -> sprintf " --%s=\"%s\"" p (x.ToString())
        | None -> ""     

    let flag p b = if b then sprintf " --%s" p else ""
    
    let releaseCommandLine (opts:CreateReleaseOptions) =
        [ (optionalStringParam "project" (liftString opts.Project))
          (optionalStringParam "version" (liftString opts.Version))
          (optionalStringParam "packageversion" (liftString opts.PackageVersion))
          (optionalStringParam "packageversionoverride" opts.PackageVersionOverride)
          (optionalStringParam "packagesfolder" opts.PackagesFolder)
          (optionalStringParam "releasenotes" (liftString opts.ReleaseNotes))
          (optionalStringParam "releasenotesfile" (liftString opts.ReleaseNotesFile)) ] 
        |> List.fold (+) ""

    let deployCommandLine (opts:DeployReleaseOptions) = 
        [ (optionalStringParam "project" (liftString opts.Project))
          (optionalStringParam "deployto" (liftString opts.DeployTo))
          (optionalStringParam "version" (liftString opts.Version))
          (flag "force" opts.Force)
          (flag "waitfordeployment" opts.WaitForDeployment)
          (optionalObjParam "deploymenttimeout" opts.DeploymentTimeout)
          (optionalObjParam "deploymentchecksleepcycle" opts.DeploymentCheckSleepCycle) ] 
        |> List.fold (+) ""

    let deleteCommandLine (opts:DeleteReleaseOptions) =
        [ (optionalStringParam "project" (liftString opts.Project))
          (optionalStringParam "minversion" (liftString opts.MinVersion))
          (optionalStringParam "maxversion" (liftString opts.MaxVersion)) ] 
        |> List.fold (+) ""

    let serverCommandLine (opts:OctoServerOptions) = 
        [ (optionalStringParam "server" (liftString opts.Server))
          (optionalStringParam "apikey" (liftString opts.ApiKey)) ] 
        |> List.fold (+) ""

    /// Maps an command to string input for the octopus tools cli.
    let commandLine command =       
        match command with
        | CreateRelease (opts, None) ->        
            sprintf " create-release%s" (releaseCommandLine opts)
        | CreateRelease (opts, Some (dopts)) ->
            sprintf " create-release%s%s" (releaseCommandLine opts) (deployCommandLine dopts)
        | DeployRelease opts ->
            sprintf " deploy-release%s" (deployCommandLine opts)
        | DeleteRelease opts ->
            sprintf " delete-releases%s" (deleteCommandLine opts)
        | ListEnvironments -> 
            " list-environments"

    // ************************************************************************
    // OCTO FUNCTION

    /// Calls the Octo.exe CLI.
    let Octo setParams =     
        
        let octoParams = setParams(octoParams)
        let command = (octoParams.Command.ToString())
        let tool = octoParams.ToolPath @@ octoParams.ToolName
        let args = commandLine octoParams.Command |>(+)<| serverCommandLine octoParams.Server

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