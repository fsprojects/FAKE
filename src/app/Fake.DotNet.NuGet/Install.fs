namespace Fake.DotNet.NuGet

/// <namespacedoc>
/// <summary>
/// DotNet.NuGet namespace contains tasks to interact with NuGet registry and packages
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains tasks for installing NuGet packages using the
/// <a href="https://docs.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-install">
/// nuget.exe install command</a>.
/// </summary>
module Install =

    open Fake.IO
    open Fake.IO.FileSystemOperators
    open Fake.Core
    open Fake.DotNet.NuGet.Restore
    open System

    /// Nuget install verbosity mode.
    /// RestorePackages Verbosity settings
    type NugetInstallVerbosity =
        /// Normal verbosity level
        | Normal
        /// Quiet verbosity level, the default value
        | Quiet
        /// Verbose/detailed verbosity level
        | Detailed

    /// <summary>
    /// Nuget install parameters.
    /// </summary>
    [<CLIMutable>]
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
            Version: string

            /// If set, the destination directory will contain only the package name, not the version number.
            /// Default `false`.
            ExcludeVersion: bool

            /// Allows updating to prerelease versions. Default <c>false</c>.
            Prerelease: bool

            /// Specifies the directory in which packages will be installed. Default  <c>./packages/</c>.
            OutputDirectory: string

            /// Display this amount of details in the output: normal, quiet, detailed. Default <c>normal</c>.
            Verbosity: NugetInstallVerbosity

            /// Do not prompt for user input or confirmations. Default <c>true</c>.
            NonInteractive: bool

            /// Disable looking up packages from local machine cache. Default <c>false</c>.
            NoCache: bool

            /// NuGet configuration file. Default <c>None</c>.
            ConfigFile: string option
        }

    /// Parameter default values.
    let NugetInstallDefaults =
        { ToolPath = findNuget (Shell.pwd () @@ "tools" @@ "NuGet")
          TimeOut = TimeSpan.FromMinutes 5.
          Retries = 5
          Sources = []
          Version = ""
          ExcludeVersion = false
          OutputDirectory = "./packages/"
          Prerelease = false
          NonInteractive = true
          Verbosity = Normal
          NoCache = false
          ConfigFile = None }

    /// [omit]
    let argList name values =
        values
        |> Seq.collect (fun v -> [ "-" + name; sprintf @"""%s""" v ])
        |> String.concat " "

    /// [omit]
    let buildArgs (param: NugetInstallParams) =
        [ param.Sources |> argList "source"
          (if param.Version <> "" then
               sprintf "-version \"%s\"" param.Version
           else
               "")
          (if param.OutputDirectory <> "" then
               sprintf "-outputdirectory \"%s\"" param.OutputDirectory
           else
               "")
          (match param.Verbosity with
           | Quiet -> "quiet"
           | Detailed -> "detailed"
           | Normal -> "normal")
          |> sprintf "-verbosity %s"
          (if param.ExcludeVersion then "-excludeversion" else "")
          (if param.Prerelease then "-prerelease" else "")
          (if param.NoCache then "-nocache" else "")
          (if param.NonInteractive then "-nonInteractive" else "")
          param.ConfigFile |> Option.toList |> argList "configFile" ]
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> String.concat " "

    /// <summary>
    /// Installs the given package.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default parameters.</param>
    /// <param name="packagesFile">Path to the <c>*.sln</c>, <c>*.*proj</c> or <c>packages.config</c> file,
    /// or simply a NuGet package name</param>
    let NugetInstall setParams packageName =
        use __ = Trace.traceTask "NugetInstall" packageName
        let param = NugetInstallDefaults |> setParams
        let args = sprintf "install %s %s" packageName (buildArgs param)

        runNuGetTrial param.Retries param.ToolPath param.TimeOut args (fun () ->
            failwithf "Package install for %s failed." packageName)

        __.MarkSuccess()
