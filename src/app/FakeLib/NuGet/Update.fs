/// Contains tasks for updating NuGet packages including assembly hint paths in the project files using the [nuget.exe update command](http://docs.nuget.org/docs/reference/command-line-reference#Update_Command).
module Fake.NuGet.Update

open System
open Fake

/// Nuget update parameters.
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
      /// Folder to store packages in. Default `./packages`.
      RepositoryPath: string
      /// Looks for updates with the highest version available within the same major and minor version as the installed package. Default `false`.
      Safe: bool
      /// Show verbose output while updating. Default `false`.
      Verbose: bool
      /// Allows updating to prerelease versions. Default `false`.
      Prerelease: bool
      /// Do not prompt for user input or confirmations. Default `true`.
      NonInteractive: bool
      /// NuGet configuration file. Default `None`.
      ConfigFile: string option }

/// Parameter default values.
let NugetUpdateDefaults =
    { ToolPath = findNuget (currentDirectory @@ "tools" @@ "NuGet")
      TimeOut = TimeSpan.FromMinutes 5.
      Retries = 5
      Sources = []
      Ids = []
      RepositoryPath = "./packages"
      Safe = false
      Verbose = false
      Prerelease = false
      NonInteractive = true
      ConfigFile = None }

/// [omit]
let buildArgs (param: NugetUpdateParams) =
    [   param.Sources |> argList "source"
        param.Ids |> argList "id"
        [param.RepositoryPath] |> argList "repositoryPath"
        (if param.Safe then "-safe" else "")
        (if param.Prerelease then "-prerelease" else "")
        (if param.NonInteractive then "-nonInteractive" else "")
        (if param.Verbose then "-verbose" else "")
        param.ConfigFile |> Option.toList |> argList "configFile"
    ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "

/// Update packages specified in the package file.
///
/// Fails if packages are not installed; see [nuget bug](https://nuget.codeplex.com/workitem/3874).
/// Fails if packages file has no corresponding VS project; see [nuget bug](https://nuget.codeplex.com/workitem/3875).
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default parameters.
///  - `packagesFile` - Path to the `*.sln`, `*.*proj` or `packages.config` file.
let NugetUpdate setParams packagesFile =
    traceStartTask "NugetUpdate" packagesFile
    let param = NugetUpdateDefaults |> setParams
    let args = sprintf "update %s %s" packagesFile (buildArgs param)
    runNuGetTrial param.Retries param.ToolPath param.TimeOut args (fun () -> failwithf "Package update for %s failed." packagesFile)
    traceEndTask "NugetUpdate" packagesFile
