namespace Fake

open System
open System.Diagnostics
open System.Text;

/// Contains tasks which allow to call [Chocolatey](https://www.chocolatey.org)
module Choco =

    /// The choco install parameter type.
    type ChocoInstallParams = {
        /// Version of the package
        /// Equivalent to the `--version <version>` option.
        Version: string
        /// Include prerelease. Default `false`.
        /// Equivalent to the `--pre` option.
        Prerelease: bool
        /// Parameters to pass to the package.
        /// Equivalent to the `--params <params>` option.
        PackageParameters: string
        /// The source to find the package(s) to install.
        // Special sources  include: ruby, webpi, cygwin, windowsfeatures, and python.
        /// Equivalent to the `--source <source>` option.
        Source: string
        /// Force x86 (32bit) installation on 64 bit systems. Default `false`.
        /// Equivalent to the `--forcex86` option.
        ForceX86: bool
        /// Install Arguments to pass to the native installer in the package.
        /// Equivalent to the `--installargs <args>` option.
        InstallArgs: string
        /// Should install arguments be used exclusively without appending to current package passed arguments? Default `false`.
        /// Equivalent to the `--overrideargs` option.
        OverrideArgs: bool
        /// Skip Powershell - Do not run chocolateyInstall.ps1. Default `false`.
        /// Equivalent to the `--skippowershell` option.
        SkipPowershell: bool
        /// User - used with authenticated feeds.
        /// Equivalent to the `--user <user>` option.
        User: string
        /// Password - the user's password to the source.
        /// Equivalent to the `--password <password>` option.
        Password: string
        /// The choco execution timeout.
        Timeout:TimeSpan
        /// The location of the choco executable. Automatically found if null or empty.
        ToolPath: string
        /// A character string containing additional arguments to give to choco.
        AdditionalArgs: string
        /// Do not prompt for user input or confirmations. Default `true`.
        /// Equivalent to the `-y` option.
        NonInteractive: bool
    }

    /// The choco pack parameter type.
    type ChocoPackParams = {
        /// The version you would like to insert into the package.
        /// Equivalent to the `--version <version>` option.
        Version: string
        /// The choco execution timeout.
        Timeout:TimeSpan
        /// The location of the choco executable. Automatically found if null or empty.
        ToolPath: string
        /// A character string containing additional arguments to give to choco.
        AdditionalArgs: string
        /// Do not prompt for user input or confirmations. Default `true`.
        /// Equivalent to the `-y` option.
        NonInteractive: bool
    }

    /// The choco push parameter type.
    type ChocoPushParams = {
        /// The source we are pushing the package to. Default: "https://chocolatey.org/"
        /// Equivalent to the `--source <source>` option.
        Source: string
        /// The api key for the source. If not specified (and not local file source), does a lookup. 
        /// If not specified and one is not found for an https source, push will fail.
        /// Equivalent to the `--apikey <apikey>` option.
        ApiKey: string
        /// The choco execution timeout.
        Timeout:TimeSpan
        /// The location of the choco executable. Automatically found if null or empty.
        ToolPath: string
        /// A character string containing additional arguments to give to choco.
        AdditionalArgs: string
        /// Do not prompt for user input or confirmations. Default `true`.
        /// Equivalent to the `-y` option.
        NonInteractive: bool
    }

    /// The default option set given to choco install.
    let ChocoInstallDefaults = {
        Timeout = TimeSpan.FromMinutes 5.
        NonInteractive = true
        Prerelease = false
        ForceX86 = false
        OverrideArgs = false
        SkipPowershell = false
        Version = null
        PackageParameters = null
        Source = null
        InstallArgs = null
        User = null
        Password = null
        ToolPath = null
        AdditionalArgs = null
    }

    /// The default option set given to choco pack.
    let ChocoPackDefaults: ChocoPackParams = {
        Timeout = TimeSpan.FromMinutes 5.
        NonInteractive = true
        Version = null
        ToolPath = null
        AdditionalArgs = null
    }
    
    /// The default option set given to choco pack.
    let ChocoPushDefaults = {
        Timeout = TimeSpan.FromMinutes 5.
        NonInteractive = true
        Source = "https://chocolatey.org/"
        ApiKey = null
        ToolPath = null
        AdditionalArgs = null
    }
    
    let private getPaths =
        let programDataPath = environVar "ProgramData"
        if programDataPath |> isNotNullOrEmpty
        then 
            [
                Seq.singleton (programDataPath @@ "chocolatey" @@ "bin")
                pathDirectories
            ]
        else
            [
                pathDirectories
            ]

    /// [omit]
    /// Tries to find the specified choco executable:
    ///
    /// 1. In the `<ProgramData>\chocolatey\bin` directory
    /// 2. In the `PATH` environment variable.
    let FindExe =
        getPaths |> Seq.concat
            |> Seq.map (fun directory -> directory @@ "choco.exe")
            |> Seq.tryFind fileExists

    /// [omit]
    /// Invokes chocolatey with the specified arguments
    /// ## Parameters
    ///  - `exePath` - The location of choco executable. Automatically found if null or empty.
    ///  - `args` - The arguments given to the executable.
    ///  - `timeout` - The choco execution timeout
    let private callChoco exePath args timeout =
        // Try to find the choco executable if not specified by the user.
        let chocoExe =
            if not <| isNullOrEmpty exePath then exePath else
            let found = FindExe
            if found <> None then found.Value else failwith "Cannot find the choco executable."

        traceStartTask "choco" args
        let setInfo (info:ProcessStartInfo) =
            info.FileName <- chocoExe
            info.Arguments <- args
        let result = ExecProcess (setInfo) timeout
        if result <> 0 then failwithf "choco failed with exit code %i." result
        traceEndTask "choco" args

    /// True if choco is available (only on windows)
    /// ## Sample usage
    ///     "Build" =?> ("ChocoInstall", Choco.IsAvailable)
    let IsAvailable = not isUnix && FindExe <> None

    /// Call choco to [install](https://github.com/chocolatey/choco/wiki/CommandsInstall) a package
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default choco parameters. See `ChocoInstallParams`
    ///  - `packages` - Names of packages, path to packages.config, .nuspec or .nupkg to install
    /// ## Sample usage
    ///
    ///     Target "ChocoInstall" (fun _ ->
    ///         "pretzel" |> Choco.Install (fun p -> { p with Version = "0.4.0" })
    ///     )
    let Install (setParams: (ChocoInstallParams -> ChocoInstallParams)) (packages: string) =
        if packages |> isNullOrEmpty then failwith "'packages' must not be empty."

        let parameters = setParams ChocoInstallDefaults

        let args = new StringBuilder()
                |> appendWithoutQuotes "install"
                |> append packages
                |> appendWithoutQuotesIfNotNull parameters.Version "--version "
                |> appendIfTrueWithoutQuotes parameters.Prerelease "--pre"
                |> appendWithoutQuotesIfNotNull parameters.PackageParameters "--params "
                |> appendWithoutQuotesIfNotNull parameters.Source "--source "
                |> appendIfTrueWithoutQuotes parameters.ForceX86 "--forcex86"
                |> appendWithoutQuotesIfNotNull parameters.InstallArgs "--installargs "
                |> appendIfTrueWithoutQuotes parameters.OverrideArgs "--overrideargs"
                |> appendIfTrueWithoutQuotes parameters.SkipPowershell "--skippowershell"
                |> appendWithoutQuotesIfNotNull parameters.User "--user "
                |> appendWithoutQuotesIfNotNull parameters.Password "--password "
                |> appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
                |> appendIfNotNullOrEmpty parameters.AdditionalArgs ""
                |> toText

        callChoco parameters.ToolPath args parameters.Timeout

    /// Call choco to [pack](https://github.com/chocolatey/choco/wiki/CommandsPack) a package
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default choco parameters. See `ChocoPackParams`
    ///  - `nuspecPath` - path to the .nuspec to pack
    /// ## Sample usage
    ///
    ///     Target "ChocoPack" (fun _ ->
    ///         "pretzel.nuspec" |> Choco.Pack (fun p -> { p with Version = "0.5.0" })
    ///     )
    let Pack setParams nuspecPath =
        if nuspecPath |> isNullOrEmpty then failwith "'nuspecPath' must not be empty."

        let parameters = setParams ChocoPackDefaults

        let args = new StringBuilder()
                |> appendWithoutQuotes "pack"
                |> append nuspecPath
                |> appendWithoutQuotesIfNotNull parameters.Version "--version "
                |> appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
                |> appendWithoutQuotesIfNotNull parameters.AdditionalArgs parameters.AdditionalArgs
                |> toText

        callChoco parameters.ToolPath args parameters.Timeout
        
    /// Call choco to [push](https://github.com/chocolatey/choco/wiki/CommandsPush) a package
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default choco parameters. See `ChocoPushParams`
    ///  - `nupkgPath` - path to the .nupkg to push
    /// ## Sample usage
    ///
    ///     Target "ChocoPush" (fun _ ->
    ///         "pretzel.0.5.0.nupkg" |> Choco.Push (fun p -> { p with ApiKey = "123-123123-123" })
    ///     )
    let Push setParams nupkgPath =
        if nupkgPath |> isNullOrEmpty then failwith "'nupkgPath' must not be empty."

        let parameters = setParams ChocoPushDefaults

        let args = new StringBuilder()
                |> appendWithoutQuotes "push"
                |> append nupkgPath
                |> appendWithoutQuotesIfNotNull parameters.Source "--source "
                |> appendWithoutQuotesIfNotNull parameters.ApiKey "--apikey "
                |> appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
                |> appendWithoutQuotesIfNotNull parameters.AdditionalArgs parameters.AdditionalArgs
                |> toText

        callChoco parameters.ToolPath args parameters.Timeout
