/// Contains tasks for installing NuGet packages using the [nuget.exe install command](http://docs.nuget.org/docs/reference/command-line-reference#Install_Command).
module Fake.NuGet.Install

open System
open Fake

type NugetInstallVerbosity =
| Normal
| Quiet
| Detailed

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
      /// The version of the package to install.
      Version: string option
      /// If set, the destination directory will contain only the package name, not the version number. Default `false`.
      ExcludeVersion: bool
      /// Allows updating to prerelease versions. Default `false`.
      Prerelease: bool
      /// Specifies the directory in which packages will be installed. If none specified, uses the current directory.
      OutputDirectory: string
      /// Display this amount of details in the output: normal, quiet, detailed. Default `normal`.
      Verbosity: NugetInstallVerbosity
      /// Do not prompt for user input or confirmations. Default `true`.
      NonInteractive: bool
      /// Disable looking up packages from local machine cache. Default `false`.
      NoCache: bool
      /// NuGet configuration file. Default `None`.
      ConfigFile: string option }

/// Parameter default values.
let NugetInstallDefaults =
    { ToolPath = findToolInSubPath "nuget.exe" (currentDirectory @@ "tools" @@ "NuGet")
      TimeOut = TimeSpan.FromMinutes 5.
      Retries = 5
      Sources = []
      Version = None
      ExcludeVersion = false
      OutputDirectory = ""
      Prerelease = false
      NonInteractive = true
      Verbosity = Normal
      NoCache = false
      ConfigFile = None }

/// [omit]
let argList name values =
    values
    |> Seq.collect (fun v -> ["-" + name; sprintf @"""%s""" v])
    |> String.concat " "

/// [omit]
let buildArgs (param: NugetInstallParams) =
    [   param.Sources |> argList "source"
        (match param.Version with | Some version -> sprintf "-version \"%s\"" version | _ -> "")
        (if param.OutputDirectory <> "" then sprintf "-outputdirectory \"%s\"" param.OutputDirectory else "")
        (match param.Verbosity with | Quiet -> "quiet" | Detailed -> "detailed" | Normal -> "")
        (if param.ExcludeVersion then "-excludeversion" else "")
        (if param.Prerelease then "-prerelease" else "")
        (if param.NoCache then "-nocache" else "")
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