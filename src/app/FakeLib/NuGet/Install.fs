/// Contains tasks for installing NuGet packages using the [nuget.exe install command](http://docs.nuget.org/docs/reference/command-line-reference#Install_Command).
module Fake.NuGet.Install

open System
open Fake

/// Nuget install parameters. All optional.
type NugetInstallParams =
    {
      /// Path to the nuget.exe.
      ToolPath: string
      /// Timeout for the update.
      TimeOut: TimeSpan
      /// Number of retries if update fails.
      Retries: int
      /// Nuget feeds to search updates in. Use default if empty.
      Sources: string list
      /// Folder to store packages in. Default `./packages`.
      RepositoryPath: string
      /// Allows updating to prerelease versions. Default `false`.
      Prerelease: bool
      /// Do not prompt for user input or confirmations. Default `true`.
      NonInteractive: bool
      /// NuGet configuration file. Default `None`.
      ConfigFile: string option }

/// Parameter default values.
let NugetInstallDefaults =
    { ToolPath = findToolInSubPath "nuget.exe" (currentDirectory @@ "tools" @@ "NuGet")
      TimeOut = TimeSpan.FromMinutes 5.
      Retries = 5
      Sources = []
      RepositoryPath = "./packages"
      Prerelease = false
      NonInteractive = true
      ConfigFile = None }

/// [omit]
let argList name values =
    values
    |> Seq.collect (fun v -> ["-" + name; sprintf @"""%s""" v])
    |> String.concat " "

/// [omit]
let buildArgs (param: NugetInstallParams) =
    [   param.Sources |> argList "source"
        [param.RepositoryPath] |> argList "repositoryPath"
        (if param.Prerelease then "-prerelease" else "")
        (if param.NonInteractive then "-nonInteractive" else "")
        param.ConfigFile |> Option.toList |> argList "configFile"
    ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "

/// Installs the given package.
///
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default parameters.
///  - `packagesFile` - Path to the `*.sln`, `*.*proj` or `packages.config` file.
let NugetInstall setParams packageName =
    traceStartTask "NugetInstall" packageName
    let param = NugetInstallDefaults |> setParams
    let args = sprintf "install %s %s" packageName (buildArgs param)
    runNuGetTrial param.Retries param.ToolPath param.TimeOut args (fun () -> failwithf "Package install for %s failed." packageName)
    traceEndTask "NugetInstall" packageName