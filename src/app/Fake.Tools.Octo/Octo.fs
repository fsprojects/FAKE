namespace Fake.Tools

/// <summary>
/// Octo module contains tasks to interact with <c>Octo.exe</c> tool
/// </summary>
[<RequireQualifiedAccess>]
module Octo =

    open Fake.Core
    open Fake.DotNet
    open Fake.IO
    open Fake.IO.FileSystemOperators
    open System
    open System.IO

    /// <summary>
    /// <c>Octo.exe</c> server options
    /// </summary>
    type ServerOptions =
        {
            /// The base URL for your Octopus server
            ServerUrl: string
            /// Your API key; retrieved from the user profile page.
            ApiKey: string
        }

    /// <summary>
    /// Common <c>Octo.exe</c> CLI params
    /// </summary>
    type Options =
        { ToolType: ToolType
          ToolName: string
          ToolPath: string
          WorkingDirectory: string
          Server: ServerOptions
          Timeout: TimeSpan }

    /// <summary>
    /// Options for creating a new release
    /// </summary>
    type CreateReleaseOptions =
        {
            /// Name of the project
            Project: string
            /// Release number to use for the new release
            Version: string
            /// Default version of all packages to use for this release
            PackageVersion: string
            /// Version number to use for a package in the release
            Packages: string list
            /// A folder containing NuGet packages from which we should get versions
            PackagesFolder: string option
            /// Release Notes for the new release
            ReleaseNotes: string
            /// Path to a file that contains Release Notes for the new release
            ReleaseNotesFile: string
            /// If a release with the version number already exists, ignore it
            IgnoreExisting: bool
            /// Channel to use for the new release
            Channel: string option
            /// Ignore package version matching rules
            IgnoreChannelRules: bool
            ///common parameters
            Common: Options
        }

    /// <summary>
    /// Options for deploying a release to an environment
    /// </summary>
    type DeployReleaseOptions =
        {
            /// Name of the project
            Project: string
            /// Environment to deploy to
            DeployTo: string
            /// Version number of the release to deploy; Specify "latest" for
            /// the latest release
            Version: string
            /// If a project is configured to skip packages with already-installed
            /// versions, override this setting to force re-deployment
            Force: bool
            /// Whether to wait synchronously for deployment to finish
            WaitForDeployment: bool
            /// Don't print the raw log of failed tasks
            NoRawLog: bool
            /// Show progress of the deployment.
            /// (Sets --waitfordeployment and --norawlog to true.)
            Progress: bool
            /// Specifies maximum time that deployment can take
            /// (default: 10 minutes)
            DeploymentTimeout: TimeSpan option
            /// Specifies how much time should elapse between deployment status
            /// checks (default: 10 seconds)
            DeploymentCheckSleepCycle: TimeSpan option
            /// A comma-separated list of machine names to target in the
            /// deployed environment. If not specified, all machines in
            /// the environment will be considered.
            SpecificMachines: string option
            /// Channel to use for the new release
            Channel: string option
            /// Common parameters
            Common: Options
        }

    /// <summary>
    /// Options for deleting a range of releases in a project
    /// </summary>
    type DeleteReleasesOptions =
        {
            /// Name of the project
            Project: string
            /// Minimum (inclusive) version number for the range of versions to delete
            MinVersion: string
            /// Maximum (inclusive) version number for the range of versions to delete
            MaxVersion: string
            /// If specified, only releases
            /// associated with the channel will be deleted;
            /// specify this argument multiple times to target
            /// multiple channels
            Channel: string option
            /// Common parameters
            Common: Options
        }

    /// <summary>
    /// Option type for pushing packages
    /// </summary>
    type PushOptions =
        {
            // paths to one or more packages to push to the server
            Packages: string list
            /// if the package already exists, should this package overwrite it?
            ReplaceExisting: bool
            /// Common parameters
            Common: Options
        }

    /// Option type for selecting one command
    type internal Command =
        | CreateRelease of CreateReleaseOptions * DeployReleaseOptions option
        | DeployRelease of DeployReleaseOptions
        | DeleteReleases of DeleteReleasesOptions
        | ListEnvironments
        | Push of PushOptions

    /// Default server options.
    let private serverOptions = { ServerUrl = ""; ApiKey = "" }

    let internal toolPath toolName =
        let currentDirectory = Directory.GetCurrentDirectory()

        let toolPath =
            ProcessUtils.tryFindLocalTool
                "TOOL"
                toolName
                [ currentDirectory @@ "tools" @@ "OctopusTools"
                  Environment.environVarOrDefault "ChocolateyInstall" currentDirectory ]

        match toolPath with
        | Some path -> path
        | None -> toolName

    /// Default parameters to call <c>Octo.exe</c>.
    let internal commonOptions =
        let toolName = "Octo.exe"

        { ToolPath = toolPath toolName
          ToolName = toolName
          Server = serverOptions
          Timeout = TimeSpan.MaxValue
          WorkingDirectory = ""
          ToolType = ToolType.Create() }

    /// Default options for 'CreateRelease'
    let internal releaseOptions =
        { Project = ""
          Version = ""
          PackageVersion = ""
          Packages = []
          PackagesFolder = None
          ReleaseNotes = ""
          ReleaseNotesFile = ""
          IgnoreExisting = false
          Channel = None
          IgnoreChannelRules = false
          Common = commonOptions }

    /// Default options for 'DeployRelease'
    let internal deployOptions =
        { Project = ""
          DeployTo = ""
          Version = ""
          Force = false
          WaitForDeployment = false
          DeploymentTimeout = None
          DeploymentCheckSleepCycle = None
          SpecificMachines = None
          NoRawLog = false
          Progress = false
          Channel = None
          Common = commonOptions }

    /// Default options for 'DeleteReleases'
    let internal deleteOptions =
        { Project = ""
          MinVersion = ""
          MaxVersion = ""
          Channel = None
          Common = commonOptions }

    /// Default options for 'Push'
    let internal pushOptions =
        { Packages = []
          ReplaceExisting = false
          Common = commonOptions }

    let private optionalStringParam p o =
        match o with
        | Some s -> sprintf "--%s=%s" p s
        | None -> ""

    let private optionalObjParam p o =
        match o with
        | Some x -> sprintf "--%s=%s" p (x.ToString())
        | None -> ""

    let private stringListParam p os =
        let sb = Text.StringBuilder()

        for o in os do
            sb.Append(sprintf "--%s=%s " p (o.ToString())) |> ignore

        sb.ToString().Trim()

    let private flag p b = if b then sprintf "--%s" p else ""

    let private releaseCommandLine (opts: CreateReleaseOptions) =
        [ (optionalStringParam "project" (String.liftString opts.Project))
          (optionalStringParam "version" (String.liftString opts.Version))
          (optionalStringParam "packageversion" (String.liftString opts.PackageVersion))
          (stringListParam "package" opts.Packages)
          (optionalStringParam "packagesfolder" opts.PackagesFolder)
          (optionalStringParam "releasenotes" (String.liftString opts.ReleaseNotes))
          (optionalStringParam "releasenotesfile" (String.liftString opts.ReleaseNotesFile))
          (flag "ignoreExisting" opts.IgnoreExisting)
          (optionalStringParam "channel" opts.Channel)
          (flag "ignorechannelrules" opts.IgnoreChannelRules) ]
        |> List.filter String.isNotNullOrEmpty

    let private deployCommandLine (opts: DeployReleaseOptions) =
        [ (optionalStringParam "project" (String.liftString opts.Project))
          (optionalStringParam "deployto" (String.liftString opts.DeployTo))
          (optionalStringParam "version" (String.liftString opts.Version))
          (flag "force" opts.Force)
          (flag "waitfordeployment" opts.WaitForDeployment)
          (flag "norawlog" opts.NoRawLog)
          (flag "progress" opts.Progress)
          (optionalObjParam "deploymenttimeout" opts.DeploymentTimeout)
          (optionalObjParam "deploymentchecksleepcycle" opts.DeploymentCheckSleepCycle)
          (optionalStringParam "specificmachines" opts.SpecificMachines)
          (optionalStringParam "channel" opts.Channel) ]
        |> List.filter String.isNotNullOrEmpty

    let private deleteCommandLine (opts: DeleteReleasesOptions) =
        [ (optionalStringParam "project" (String.liftString opts.Project))
          (optionalStringParam "minversion" (String.liftString opts.MinVersion))
          (optionalStringParam "maxversion" (String.liftString opts.MaxVersion))
          (optionalStringParam "channel" opts.Channel) ]
        |> List.filter String.isNotNullOrEmpty

    let internal serverCommandLine (opts: ServerOptions) =
        [ (optionalStringParam "server" (String.liftString opts.ServerUrl))
          (optionalStringParam "apikey" (String.liftString opts.ApiKey)) ]
        |> List.filter String.isNotNullOrEmpty

    let private pushCommandLine (opts: PushOptions) =
        [ stringListParam "package" opts.Packages
          flag "replace-existing" opts.ReplaceExisting ]
        |> List.filter String.isNotNullOrEmpty

    /// Maps a command to string input for the octopus tools cli.
    let internal commandLine command =
        match command with
        | CreateRelease(opts, None) -> "create-release" :: (releaseCommandLine opts)
        | CreateRelease(opts, Some dopts) ->
            "create-release"
            :: (List.append (releaseCommandLine opts) (deployCommandLine dopts))
        | DeployRelease opts -> "deploy-release" :: (deployCommandLine opts)
        | DeleteReleases opts -> "delete-releases" :: (deleteCommandLine opts)
        | ListEnvironments -> [ "list-environments" ]
        | Push opts -> "push" :: (pushCommandLine opts)

    let private exec command options =
        let serverCommandLineForTracing (opts: ServerOptions) =
            serverCommandLine { opts with ApiKey = "(Removed for security purposes)" }

        let tool = options.ToolPath

        let args =
            List.append (commandLine command) (serverCommandLine options.Server)
            |> Arguments.OfArgs

        let traceArgs =
            (List.append (commandLine command) (serverCommandLineForTracing options.Server))
            |> List.fold (+) ""

        let commandString = command.ToString()

        use __ = Trace.traceTask "Octo " commandString
        Trace.trace (tool + traceArgs)

        let result =
            RawCommand(tool, args)
            |> CreateProcess.fromCommand
            |> CreateProcess.withToolType (options.ToolType.WithDefaultToolCommandName "dotnet-octo")
            |> CreateProcess.withWorkingDirectory options.WorkingDirectory
            |> CreateProcess.withTimeout options.Timeout
            |> Proc.run
            |> (fun finishedProcess -> finishedProcess.ExitCode)

        match result with
        | 0 ->
            __.MarkSuccess()
            result
        | _ ->
            __.MarkFailed()
            result

    /// <summary>
    /// Creates a release and returns the exit code.
    /// </summary>
    ///
    /// <param name="setParams">The create release parameters</param>
    let createReleaseWithExitCode setParams =
        let options = setParams releaseOptions
        exec (CreateRelease(options, None)) options.Common

    /// <summary>
    /// Creates a release, and optionally deploys it to one or more environments and returns the exit code.
    /// </summary>
    ///
    /// <param name="setReleaseParams">The release parameters</param>
    /// <param name="setDeployParams">The deploy parameters</param>
    let createReleaseAndDeployWithExitCode setReleaseParams setDeployParams =
        let releaseOptions = setReleaseParams releaseOptions
        let deployOptions = setDeployParams deployOptions
        exec (CreateRelease(releaseOptions, deployOptions)) releaseOptions.Common

    /// <summary>
    /// Deploys releases that have already been created and returns the exit code.
    /// </summary>
    ///
    /// <param name="setParams">The release parameters</param>
    let deployReleaseWithExitCode setParams =
        let options = setParams deployOptions
        exec (DeployRelease options) options.Common

    /// <summary>
    /// Deletes a range of releases and returns the exit code.
    /// </summary>
    ///
    /// <param name="setParams">The delete release parameters</param>
    let deleteReleasesWithExitCode setParams =
        let options = setParams deleteOptions
        exec (DeleteReleases options) options.Common

    /// <summary>
    /// Lists all environments and returns the exit code.
    /// </summary>
    ///
    /// <param name="setParams">The Octo tool parameters</param>
    let listEnvironmentsWithExitCode setParams =
        let options = setParams commonOptions
        exec ListEnvironments options

    /// <summary>
    /// Pushes one or more packages to the Octopus built-in repository and returns the exit code.
    /// </summary>
    ///
    /// <param name="setParams">The push parameters</param>
    let pushWithExitCode setParams =
        let options = setParams pushOptions
        exec (Push options) options.Common


    let private handleIgnoreExitCode commandString result =
        match result with
        | 0 -> ()
        | _ -> failwithf "Octo %s failed. Process finished with exit code %i" commandString result

    /// <summary>
    /// Creates a release.
    /// </summary>
    ///
    /// <param name="setParams">The create release parameters</param>
    let createRelease setParams =
        let commandLine = (CreateRelease((setParams releaseOptions), None)).ToString()

        createReleaseWithExitCode setParams |> (handleIgnoreExitCode <| commandLine)

    /// <summary>
    /// Creates a release, and optionally deploys it to one or more environments.
    /// </summary>
    ///
    /// <param name="setReleaseParams">The release parameters</param>
    /// <param name="setDeployParams">The deploy parameters</param>
    let createReleaseAndDeploy setReleaseParams setDeployParams =
        let commandLine =
            (CreateRelease((setReleaseParams releaseOptions), (setDeployParams deployOptions)))
                .ToString()

        createReleaseAndDeployWithExitCode setReleaseParams setDeployParams
        |> (handleIgnoreExitCode <| commandLine)

    /// <summary>
    /// Deploys releases that have already been created.
    /// </summary>
    ///
    /// <param name="setParams">The release deployment parameters</param>
    let deployRelease setParams =
        let commandLine = (DeployRelease(setParams deployOptions)).ToString()

        deployReleaseWithExitCode setParams |> (handleIgnoreExitCode <| commandLine)

    /// <summary>
    /// Deletes a range of releases.
    /// </summary>
    ///
    /// <param name="setParams">The releases to delete parameters</param>
    let deleteReleases setParams =
        let commandLine = (DeleteReleases(setParams deleteOptions)).ToString()

        deleteReleasesWithExitCode setParams |> (handleIgnoreExitCode <| commandLine)

    /// <summary>
    /// Lists all environments.
    /// </summary>
    ///
    /// <param name="setParams">The environments to list parameters</param>
    let listEnvironments setParams =
        let commandLine = ListEnvironments.ToString()

        listEnvironmentsWithExitCode setParams |> (handleIgnoreExitCode <| commandLine)

    /// <summary>
    /// Pushes one or more packages to the Octopus built-in repository.
    /// </summary>
    ///
    /// <param name="setParams">The push package parameters</param>
    let push setParams =
        let commandLine = (Push(setParams pushOptions)).ToString()

        pushWithExitCode setParams |> (handleIgnoreExitCode <| commandLine)
