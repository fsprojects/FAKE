namespace Fake.Tools

[<RequireQualifiedAccess>]
module GitVersion =

    open Newtonsoft.Json
    open System
    open System.IO
    open Fake.Core
    open Fake.DotNet

    /// The parameters for GitVersion tool
    type GitVersionParams =
        {
          /// Tool type
          ToolType: ToolType
          /// Path to the GitVersion exe file.
          ToolPath: string
          /// The timeout for the GitVersion process.
          TimeOut: TimeSpan }

    let internal toolPath toolName =
        let toolPath =
            ProcessUtils.tryFindLocalTool
                "TOOL"
                toolName
                [ Environment.environVarOrDefault "ChocolateyInstall" (Directory.GetCurrentDirectory()) ]

        match toolPath with
        | Some path -> path
        | None -> toolName

    let private GitVersionDefaults =
        { ToolType = ToolType.Create()
          ToolPath = toolPath "GitVersion.exe"
          TimeOut = TimeSpan.FromMinutes 1. }

    /// The arguments to pass to GitVersion tool
    type GitVersionProperties =
        { Major: int
          Minor: int
          Patch: int
          PreReleaseTag: string
          PreReleaseTagWithDash: string
          PreReleaseLabel: string
          PreReleaseNumber: Nullable<int>
          BuildMetaData: string
          BuildMetaDataPadded: string
          FullBuildMetaData: string
          MajorMinorPatch: string
          SemVer: string
          LegacySemVer: string
          LegacySemVerPadded: string
          AssemblySemVer: string
          FullSemVer: string
          InformationalVersion: string
          BranchName: string
          Sha: string
          NuGetVersionV2: string
          NuGetVersion: string
          CommitsSinceVersionSource: int
          CommitsSinceVersionSourcePadded: string
          CommitDate: string }

    let internal createProcess setParams =
        let parameters = GitVersionDefaults |> setParams

        CreateProcess.fromCommand (RawCommand(parameters.ToolPath, Arguments.Empty))
        |> CreateProcess.withToolType (parameters.ToolType.WithDefaultToolCommandName "gitversion")
        |> CreateProcess.redirectOutput
        |> CreateProcess.withTimeout parameters.TimeOut
        |> CreateProcess.ensureExitCode
        |> fun command ->
            Trace.trace command.CommandLine
            command

    /// Runs [GitVersion](https://gitversion.net/docs/) on a .NET project file.
    ///
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the GitVersionDefaults value.
    ///
    /// ## Sample
    ///      generateProperties id // Use Defaults
    ///      generateProperties (fun p -> { p with ToolPath = "/path/to/directory" }
    let generateProperties setParams =
        let result = createProcess setParams |> Proc.run

        result.Result.Output
        |> JsonConvert.DeserializeObject<GitVersionProperties>
