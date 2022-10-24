namespace Fake.DotNet.NuGet

/// <summary>
/// Contains tasks which allow to restore NuGet packages from a NuGet package feed like
/// <a href="http://www.nuget.org">nuget.org</a> using the
/// <a href="https://docs.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-restore">
/// nuget.exe restore command</a>. There is also a tutorial about
/// <a href="/dotnet-nuget.html">nuget package restore</a> available.
/// </summary>
module Restore =

    open Fake.IO
    open Fake.IO.FileSystemOperators
    open Fake.IO.Globbing.Operators
    open Fake.Core
    open System

    /// <summary>
    /// Looks for NuGet.exe in
    /// <list type="number">
    /// <item>
    /// the specified defaultPath
    /// </item>
    /// <item>
    /// a list of standard tool folders
    /// </item>
    /// <item>
    /// any subfolder in the current directory
    /// </item>
    /// <item>
    /// the PATH - returns the first path where NuGet.exe was found.
    /// </item>
    /// </list>
    /// </summary>
    ///
    /// <param name="defaultPath">The default path to return when NuGet cannot be found by path specified above</param>
    let findNuget defaultPath =
        try
            let priorityList =
                [ defaultPath
                  Shell.pwd () @@ "tools" @@ "NuGet"
                  Shell.pwd () @@ ".nuget"
                  Shell.pwd () @@ "packages" @@ "NuGet.Commandline" @@ "tools"
                  Shell.pwd () @@ "packages" @@ "Nuget.Commandline" @@ "tools" ]

            let exeNames = [ "nuget.exe"; "NuGet.exe"; "Nuget.exe" ]

            let findInFolders folders =
                seq {
                    for path in folders do
                        for name in exeNames do
                            let fi = FileInfo.ofPath (path @@ name)

                            if fi.Exists then
                                yield fi.FullName
                }

            // Find in defaultPath or priorityFolders
            let priorityPaths = findInFolders priorityList

            if not <| Seq.isEmpty priorityPaths then
                Seq.head priorityPaths
            else

                // Find in ANY subfolder
                let tools =
                    !!("./**/" @@ "nuget.exe")
                    ++ ("./**/" @@ "NuGet.exe")
                    ++ ("./**/" @@ "Nuget.exe")

                if not <| Seq.isEmpty tools then
                    Seq.head tools
                else

                    // Find in PATH
                    let nugetInPATH = findInFolders Environment.pathDirectories

                    if not <| Seq.isEmpty nugetInPATH then
                        Seq.head nugetInPATH
                    else

                        defaultPath @@ "NuGet.exe"
        with _ ->
            defaultPath @@ "NuGet.exe"


    /// RestorePackages Verbosity settings
    type NugetRestoreVerbosity =
        /// Normal verbosity level
        | Normal
        /// Quiet verbosity level, the default value
        | Quiet
        /// Verbose/detailed verbosity level
        | Detailed

    /// <summary>
    /// RestorePackages parameter path
    /// </summary>
    type RestorePackageParams =
        {
            /// The path to the NuGet program
            ToolPath: string

            /// Specifies the list of package sources (as URLs) to use for the restore
            Sources: string list

            /// The timeout to use to restrict command execution time
            TimeOut: TimeSpan

            /// Specifies how often nuget should try to restore the packages - default is 5
            Retries: int

            /// Specifies the folder in which packages are installed. Output Directory
            OutputPath: string

            /// The verbosity level
            Verbosity: NugetRestoreVerbosity
        }

    /// RestorePackage defaults parameters
    let RestorePackageDefaults =
        { ToolPath = findNuget (Shell.pwd () @@ "tools" @@ "NuGet")
          Sources = []
          TimeOut = TimeSpan.FromMinutes 5.
          Retries = 5
          OutputPath = "./packages"
          Verbosity = Normal }

    /// <summary>
    /// RestorePackages parameter path for single packages
    /// </summary>
    type RestoreSinglePackageParams =
        {
            /// The path to the NuGet program
            ToolPath: string

            /// Specifies the list of package sources (as URLs) to use for the restore
            Sources: string list

            /// The timeout to use to restrict command execution time
            TimeOut: TimeSpan

            /// Specifies the folder in which packages are installed. Output Directory
            OutputPath: string

            /// The version to use in restoring the package
            Version: Version option

            /// Mark a version to be excluded, the version is specified in <c>Version</c> property
            ExcludeVersion: bool

            /// Specifies how often nuget should try to restore the packages - default is 5
            Retries: int

            /// Mark if pre-releases are included in restore process
            IncludePreRelease: bool

            /// The verbosity level
            Verbosity: NugetRestoreVerbosity
        }

    /// RestoreSinglePackageParams defaults parameters
    let RestoreSinglePackageDefaults =
        { ToolPath = RestorePackageDefaults.ToolPath
          Sources = []
          TimeOut = TimeSpan.FromMinutes 2.
          OutputPath = RestorePackageDefaults.OutputPath
          Version = None
          ExcludeVersion = false
          Retries = 5
          IncludePreRelease = false
          Verbosity = Normal }

    /// [omit]
    let runNuGet toolPath timeOut args failWith =
        let processResult =
            CreateProcess.fromRawCommandLine (toolPath |> Path.getFullName) args
            |> CreateProcess.withFramework
            |> CreateProcess.withTimeout timeOut
            |> Proc.run

        if processResult.ExitCode <> 0 then
            failWith ()

    /// [omit]
    let rec runNuGetTrial retries toolPath timeOut args failWith =
        let f () = runNuGet toolPath timeOut args failWith
        TaskRunner.runWithRetries f retries

    /// [omit]
    let buildSources sources : string list =
        sources |> List.collect (fun source -> [ "-Source"; source ])


    //Args Helper Functions
    let private verbosityToString (v: NugetRestoreVerbosity) =
        (match v with
         | Quiet -> "quiet"
         | Detailed -> "detailed"
         | Normal -> "normal")

    /// [omit]
    let buildNuGetArgs setParams packageId =
        let parameters = RestoreSinglePackageDefaults |> setParams
        let sources = parameters.Sources |> buildSources

        let args =
            [ yield! [ "install"; packageId ]
              yield! [ "-OutputDirectory"; parameters.OutputPath |> Path.getFullName ]
              yield! sources
              yield! [ "-verbosity"; (verbosityToString parameters.Verbosity) ] ]
            |> Args.toWindowsCommandLine

        match parameters.ExcludeVersion, parameters.IncludePreRelease, parameters.Version with
        | true, false, Some v -> args + " \"-ExcludeVersion\" \"-Version\" \"" + v.ToString() + "\""
        | true, false, None -> args + " \"-ExcludeVersion\""
        | false, _, Some v -> args + " \"-Version\" \"" + v.ToString() + "\""
        | false, false, None -> args
        | false, true, _ -> args + " \"-PreRelease\""
        | true, true, _ -> args + " \"-ExcludeVersion\" \"-PreRelease\""

    /// <summary>
    /// Restores the given package from NuGet
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default NuGet parameters.</param>
    /// <param name="packageFile">The package Id to restore</param>
    let RestorePackageId setParams packageId =
        use __ = Trace.traceTask "RestorePackageId" packageId
        let parameters = RestoreSinglePackageDefaults |> setParams

        let args = buildNuGetArgs setParams packageId

        runNuGetTrial parameters.Retries parameters.ToolPath parameters.TimeOut args (fun () ->
            failwithf "Package installation of package %s failed." packageId)

        __.MarkSuccess()

    /// <summary>
    /// Restores the packages in the given packages.config file from NuGet.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default NuGet parameters.</param>
    /// <param name="packageFile">The packages.config file name.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "RestorePackages" (fun _ ->
    ///          "./src/ProjectA/packages.config"
    ///          |> RestorePackage (fun p ->
    ///              { p with
    ///                  Sources = "http://myNugetSources.com" :: p.Sources
    ///                  OutputPath = outputDir
    ///                  Retries = 4 })
    ///      )
    /// </code>
    /// </example>
    let RestorePackage setParams packageFile =
        use __ = Trace.traceTask "RestorePackage" packageFile
        let (parameters: RestorePackageParams) = RestorePackageDefaults |> setParams

        let sources = parameters.Sources |> buildSources

        let args =
            [ yield! [ "install"; (packageFile |> Path.getFullName) ]
              yield! [ "-OutputDirectory"; parameters.OutputPath |> Path.getFullName ]
              yield! sources
              yield! [ "-verbosity"; (verbosityToString parameters.Verbosity) ] ]
            |> Args.toWindowsCommandLine

        runNuGetTrial parameters.Retries parameters.ToolPath parameters.TimeOut args (fun () ->
            failwithf "Package installation of %s generation failed." packageFile)

        __.MarkSuccess()

    /// <summary>
    /// Restores all packages from NuGet to the default directories by scanning for packages.config files
    /// in any subdirectory.
    /// </summary>
    let RestorePackages () =
        !! "./**/packages.config" |> Seq.iter (RestorePackage id)

    /// <summary>
    /// Restores the packages in the given solution file file from NuGet.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default NuGet parameters.</param>
    /// <param name="solutionFile">The microsoft sln file name.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "RestorePackages" (fun _ ->
    ///          "./src/Everything.sln"
    ///          |> RestoreMSSolutionPackages (fun p ->
    ///              { p with
    ///                  Sources = "http://myNugetSources.com" :: p.Sources
    ///                  OutputPath = outputDir
    ///                  Retries = 4 })
    ///      )
    /// </code>
    /// </example>
    let RestoreMSSolutionPackages setParams solutionFile =
        use __ = Trace.traceTask "RestoreSolutionPackages" solutionFile
        let (parameters: RestorePackageParams) = RestorePackageDefaults |> setParams

        let sources = parameters.Sources |> buildSources

        let args =
            [ yield! [ "restore"; (solutionFile |> Path.getFullName) ]
              yield! [ "-OutputDirectory"; parameters.OutputPath |> Path.getFullName ]
              yield! sources
              yield! [ "-verbosity"; (verbosityToString parameters.Verbosity) ] ]
            |> Args.toWindowsCommandLine

        runNuGetTrial parameters.Retries parameters.ToolPath parameters.TimeOut args (fun () ->
            failwithf "Package restore of %s failed" solutionFile)

        __.MarkSuccess()
