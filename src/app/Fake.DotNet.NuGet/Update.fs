namespace Fake.DotNet.NuGet

/// <summary>
/// Contains tasks for updating NuGet packages including assembly hint paths in the project files using
/// the <a href="https://docs.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-update">nuget.exe update command</a>.
/// </summary>
module Update =

    open Fake.IO
    open Fake.IO.FileSystemOperators
    open Fake.DotNet.NuGet.NuGet
    open Fake.Core
    open System
    open Fake.DotNet.NuGet.Restore

    /// <summary>
    /// Nuget update parameters.
    /// </summary>
    [<CLIMutable>]
    type NugetUpdateParams =
        {
            /// Path to the nuget.exe.
            ToolPath: string
            
            /// Timeout for the update.
            TimeOut: TimeSpan
            
            /// Number of retries if update fails.
            Retries: int
            
            /// Nuget feeds to search updates in. Use default if empty.
            Sources: string list
            
            /// Packages to update. Update all if empty.
            Ids: string list
            
            /// Version to update to. Default <c>None</c>. Used to upgrade/downgrade to a explicit version of a package.
            Version: string option
            
            /// Folder to store packages in. Default <c>./packages</c>.
            RepositoryPath: string
            
            /// Looks for updates with the highest version available within the same major and minor version as
            /// the installed package. Default <c>false</c>.
            Safe: bool
            
            /// Show verbose output while updating. Default <c>false</c>.
            Verbose: bool
            
            /// Allows updating to prerelease versions. Default <c>false</c>.
            Prerelease: bool
            
            /// Do not prompt for user input or confirmations. Default <c>true</c>.
            NonInteractive: bool
            
            /// NuGet configuration file. Default <c>None</c>.
            ConfigFile: string option
        }

    /// Parameter default values.
    let NugetUpdateDefaults =
        { ToolPath = findNuget (Shell.pwd () @@ "tools" @@ "NuGet")
          TimeOut = TimeSpan.FromMinutes 5.
          Retries = 5
          Sources = []
          Ids = []
          Version = None
          RepositoryPath = "./packages"
          Safe = false
          Verbose = false
          Prerelease = false
          NonInteractive = true
          ConfigFile = None }

    /// [omit]
    let buildArgs (param: NugetUpdateParams) =
        [ param.Sources |> argList "source"
          param.Ids |> argList "id"
          [ param.RepositoryPath ] |> argList "repositoryPath"
          param.Version |> Option.toList |> argList "Version"
          (if param.Safe then "-safe" else "")
          (if param.Prerelease then "-prerelease" else "")
          (if param.NonInteractive then "-nonInteractive" else "")
          (if param.Verbose then "-verbose" else "")
          param.ConfigFile |> Option.toList |> argList "configFile" ]
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> String.concat " "

    /// <summary>
    /// Update packages specified in the package file.
    /// </summary>
    /// <remarks>
    /// Fails if packages are not installed; see <a href="https://nuget.codeplex.com/workitem/3874">nuget bug</a>.
    /// Fails if packages file has no corresponding VS project; see
    /// <a href="https://nuget.codeplex.com/workitem/3875">nuget bug</a>.
    /// </remarks>
    /// 
    /// <param name="setParams">Function used to manipulate the default parameters.</param>
    /// <param name="packagesFile">Path to the <c>*.sln</c>, <c>*.*proj</c> or <c>packages.config</c> file.</param>
    let NugetUpdate setParams packagesFile =
        use __ = Trace.traceTask "NugetUpdate" packagesFile
        let param = NugetUpdateDefaults |> setParams
        let args = sprintf "update %s %s" packagesFile (buildArgs param)

        runNuGetTrial param.Retries param.ToolPath param.TimeOut args (fun () ->
            failwithf "Package update for %s failed." packagesFile)

        __.MarkSuccess()
