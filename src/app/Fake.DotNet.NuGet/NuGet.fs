namespace Fake.DotNet.NuGet

/// <summary>
/// Contains helper functions and task which allow to inspect, create and publish
/// <a href="https://www.nuget.org/">NuGet</a> packages.
/// There is also a tutorial about <a href="/dotnet-nuget.html">nuget package creating</a> available.
/// </summary>
module NuGet =

    open Fake.IO
    open Fake.IO.FileSystemOperators
    open Fake.Core
    open System
    open System.IO
    open System.Text
    open System.Xml.Linq
    open Fake.DotNet.NuGet.Restore

    type NugetDependencies = (string * string) list

    type NugetFrameworkDependencies =
        { FrameworkVersion: string
          Dependencies: NugetDependencies }

    type NugetReferences = string list

    type NugetFrameworkReferences = { FrameworkVersion: string; References: NugetReferences }

    type NugetFrameworkAssemblyReferences = { FrameworkVersions: string list; AssemblyName: string }

    /// Specifies that the package contains sources and symbols.
    type NugetSymbolPackage =
        /// Do not build symbol packages
        | None = 0
        /// Build a symbol package using a project file, if provided
        | ProjectFile = 1
        /// Build a symbol package using the nuspec file
        | Nuspec = 2

    type internal ToolOptions =
        {
            /// The NuGet executable path
            ToolPath: string

            /// The NuGet command to execute
            Command: string

            /// The working directory to execute the command in
            WorkingDir: string

            /// Mark if to use full framework or not
            IsFullFramework: bool
        }

        static member Create toolPath command workingDir isFullFramework =
            { ToolPath = toolPath
              Command = command
              WorkingDir = workingDir
              IsFullFramework = isFullFramework }

    /// <summary>
    /// Nuget base parameter type
    /// </summary>
    type NuGetParams =
        {
            /// The path to the NuGet executable
            ToolPath: string

            /// The timeout to use to restrict NuGet command run time
            TimeOut: TimeSpan

            /// The package version
            Version: string

            /// The list of authors of the package
            Authors: string list

            /// The project name of the package
            Project: string

            /// The package title
            Title: string

            /// The summary description of the package
            Summary: string

            /// The descriptive text of the package
            Description: string

            /// Tags referring to the package
            Tags: string

            /// The release notes file path of the package
            ReleaseNotes: string

            /// The copyright text of the package
            Copyright: string

            /// The working directory to execute command in
            WorkingDir: string

            /// Sets the base path of the files defined in the .nuspec file.
            BasePath: string option

            /// Specifies the folder in which the created package is stored. If no folder is specified,
            /// the current folder is used.
            OutputPath: string

            /// Specifies the server URL. NuGet identifies a UNC or local folder source and simply
            /// copies the file there instead of pushing it using HTTP
            PublishUrl: string

            /// NuGet API access key
            AccessKey: string

            /// Specifies the symbol server URL.
            SymbolPublishUrl: string

            /// NuGet symbol API access key
            SymbolAccessKey: string

            /// Prevents default exclusion of NuGet package files and files and folders starting with a dot,
            /// such as <c>.svn</c> and <c>.gitignore</c>.
            NoDefaultExcludes: bool

            /// Specifies that pack should not run package analysis after building the package.
            NoPackageAnalysis: bool

            /// The project file to use
            ProjectFile: string

            /// The list of dependencies of the package. <c>dependencies</c>
            Dependencies: NugetDependencies

            /// The list of dependencies of the package grouped by Framework. <c>dependencies</c>
            DependenciesByFramework: NugetFrameworkDependencies list

            /// The list of packages that reference the package. <c>references</c>
            References: NugetReferences

            /// The list of packages that reference the package grouped by Framework. <c>references</c>
            ReferencesByFramework: NugetFrameworkReferences list

            /// The list of <c>frameworkAssemblies</c>
            FrameworkAssemblies: NugetFrameworkAssemblyReferences list

            /// Mark if to include list of projects that reference the package
            IncludeReferencedProjects: bool

            /// mark if to publish a trial version of the package
            PublishTrials: int

            /// mark if to publish the package or not
            Publish: bool

            /// `NugetSymbolPackage` parameters
            SymbolPackage: NugetSymbolPackage

            /// Should appear last on the command line after other options. Specifies a list of properties
            /// that override values in the project file
            Properties: list<string * string>

            /// The list of files to include or exclude. <c>files</c>
            Files: list<string * string option * string option>

            /// The list of content files to include or exclude. <c>contentFiles</c>
            ContentFiles: list<string * string option * string option * bool option * bool option>

            /// The package language. <c>language</c>
            Language: string
        }

    /// NuGet default parameters
    let NuGetDefaults () =
        { ToolPath = findNuget (Shell.pwd () @@ "tools" @@ "NuGet")
          TimeOut = TimeSpan.FromMinutes 5.
          Version =
            if not BuildServer.isLocalBuild then
                BuildServer.buildVersion
            else
                "0.1.0.0"
          Authors = []
          Project = ""
          Title = ""
          Summary = null
          ProjectFile = null
          Description = null
          Tags = null
          ReleaseNotes = null
          Copyright = null
          Dependencies = []
          DependenciesByFramework = []
          References = []
          ReferencesByFramework = []
          FrameworkAssemblies = []
          IncludeReferencedProjects = false
          BasePath = None
          OutputPath = "./NuGet"
          WorkingDir = "./NuGet"
          PublishUrl = "https://www.nuget.org/api/v2/package"
          AccessKey = null
          SymbolPublishUrl = null
          SymbolAccessKey = null
          NoDefaultExcludes = false
          NoPackageAnalysis = false
          PublishTrials = 5
          Publish = false
          SymbolPackage = NugetSymbolPackage.ProjectFile
          Properties = []
          Files = []
          ContentFiles = []
          Language = null }

    /// <summary>
    /// Creates a string which tells NuGet that you require exactly this package version.
    /// </summary>
    ///
    /// <param name="version">The exact version to require</param>
    let RequireExactly version = sprintf "[%s]" version

    /// NuGet package versioning breaking changes point
    type BreakingPoint =
        /// Breaking on major component of SemVer
        | SemVer
        /// Breaking on minor component of SemVer
        | Minor
        /// Breaking on patch component of SemVer
        | Patch

    /// <summary>
    /// Require a version by given breaking point and version
    /// See <a href="https://docs.nuget.org/create/versioning">NuGet Versioning</a>
    /// </summary>
    ///
    /// <param name="breakingPoint">The breaking point for version range. See <c>BreakingPoint</c> type</param>
    /// <param name="version">The version to use to find the range</param>
    let RequireRange breakingPoint version =
        let v = SemVer.parse version

        match breakingPoint with
        | SemVer -> sprintf "[%s,%d.0)" version (v.Major + 1u)
        | Minor -> // Like Semver but we assume that the increase of a minor version is already breaking
            sprintf "[%s,%d.%d)" version v.Major (v.Minor + 1u)
        | Patch -> // Every update breaks
            version |> RequireExactly

    let private packageFileName parameters =
        sprintf "%s.%s.nupkg" parameters.Project parameters.Version

    /// <summary>
    /// Gets the version no. for a given package in the deployments folder
    /// </summary>
    ///
    /// <param name="deploymentsDir">The deployment directory to look into</param>
    /// <param name="package">The package id to look for</param>
    let GetPackageVersion deploymentsDir package =
        try
            if Directory.Exists deploymentsDir |> not then
                failwithf
                    "Package %s was not found, because the deployment directory %s doesn't exist."
                    package
                    deploymentsDir

            let version =
                let dirs = Directory.GetDirectories(deploymentsDir, sprintf "%s*" package)

                if Seq.isEmpty dirs then
                    failwithf "Package %s was not found." package

                let folder = Seq.head dirs
                let index = folder.LastIndexOf package + package.Length + 1

                if index < folder.Length then
                    folder.Substring index
                else
                    let nuspec = Directory.GetFiles(folder, sprintf "%s.nuspec" package) |> Seq.head
                    let doc = XDocument.Load(nuspec)
                    let vers = doc.Descendants(XName.Get("version", doc.Root.Name.NamespaceName))
                    (Seq.head vers).Value

            Trace.logfn "Version %s found for package %s" version package
            version
        with exn ->
            Exception("Could not detect package version for " + package, exn) |> raise

    let private createNuSpecFromTemplate parameters (templateNuSpec: FileInfo) =
        let specFile =
            parameters.WorkingDir
            @@ (templateNuSpec.Name.Replace("nuspec", "") + parameters.Version + ".nuspec")
            |> Path.getFullName

        Trace.tracefn "Creating .nuspec file at %s" specFile

        templateNuSpec.CopyTo(specFile, true) |> ignore

        let getFrameworkGroup (frameworkTags: (string * string) seq) =
            frameworkTags
            |> Seq.map (fun (frameworkVersion, tags) ->
                if String.isNullOrEmpty frameworkVersion then
                    sprintf "<group>%s</group>" tags
                else
                    sprintf "<group targetFramework=\"%s\">%s</group>" frameworkVersion tags)
            |> String.toLines

        let getGroup items toTags =
            if items = [] then
                ""
            else
                sprintf "<group>%s</group>" (items |> toTags)

        let getReferencesTags references =
            references
            |> Seq.map (fun assembly -> sprintf "<reference file=\"%s\" />" assembly)
            |> String.toLines

        let references = getGroup parameters.References getReferencesTags

        let referencesByFramework =
            parameters.ReferencesByFramework
            |> Seq.map (fun x -> (x.FrameworkVersion, getReferencesTags x.References))
            |> getFrameworkGroup

        let referencesXml =
            sprintf "<references>%s</references>" (references + referencesByFramework)

        let getFrameworkAssemblyTags references =
            references
            |> Seq.map (fun x ->
                if x.FrameworkVersions = [] then
                    sprintf "<frameworkAssembly assemblyName=\"%s\" />" x.AssemblyName
                else
                    sprintf
                        "<frameworkAssembly assemblyName=\"%s\" targetFramework=\"%s\" />"
                        x.AssemblyName
                        (x.FrameworkVersions |> String.separated ", "))
            |> String.toLines

        let frameworkAssembliesXml =
            if parameters.FrameworkAssemblies = [] then
                ""
            else
                sprintf
                    "<frameworkAssemblies>%s</frameworkAssemblies>"
                    (parameters.FrameworkAssemblies |> getFrameworkAssemblyTags)

        let getDependenciesTags dependencies =
            dependencies
            |> Seq.map (fun (package, version) -> sprintf "<dependency id=\"%s\" version=\"%s\" />" package version)
            |> String.toLines

        let dependencies = getGroup parameters.Dependencies getDependenciesTags

        let dependenciesByFramework =
            parameters.DependenciesByFramework
            |> Seq.map (fun x -> (x.FrameworkVersion, getDependenciesTags x.Dependencies))
            |> getFrameworkGroup

        let dependenciesXml =
            sprintf "<dependencies>%s</dependencies>" (dependencies + dependenciesByFramework)

        let contentFilesTags =
            parameters.ContentFiles
            |> Seq.map (fun (incl, exclArg, buildActionArg, copyToOutputArg, flattenArg) ->
                let excl =
                    match exclArg with
                    | Some x -> sprintf " exclude=\"%s\"" x
                    | _ -> String.Empty

                let buildAction =
                    match buildActionArg with
                    | Some x -> sprintf " buildAction=\"%s\"" x
                    | _ -> String.Empty

                let copyToOutput =
                    match copyToOutputArg with
                    | Some x -> sprintf " copyToOutput=\"%b\"" x
                    | _ -> String.Empty

                let flatten =
                    match flattenArg with
                    | Some x -> sprintf " flatten=\"%b\"" x
                    | _ -> String.Empty

                sprintf "<files include=\"%s\"%s%s%s%s />" incl excl buildAction copyToOutput flatten)
            |> String.toLines

        let contentFilesXml = sprintf "<contentFiles>%s</contentFiles>" contentFilesTags

        let filesTags =
            parameters.Files
            |> Seq.map (fun (source, target, exclude) ->
                let excludeStr =
                    if exclude.IsSome then
                        sprintf " exclude=\"%s\"" exclude.Value
                    else
                        String.Empty

                let targetStr =
                    if target.IsSome then
                        sprintf " target=\"%s\"" target.Value
                    else
                        String.Empty

                sprintf "<file src=\"%s\"%s%s />" source targetStr excludeStr)
            |> String.toLines

        let filesXml = sprintf "<files>%s</files>" filesTags

        let xmlEncode (notEncodedText: string) =
            if String.IsNullOrWhiteSpace notEncodedText then
                ""
            else
                XText(notEncodedText).ToString().Replace("�", "&szlig;")

        let toSingleLine (text: string) =
            if text = null then
                null
            else
                text.Replace("\r", "").Replace("\n", "").Replace("  ", " ")

        let replacements =
            [ "@build.number@", parameters.Version
              "@title@", parameters.Title
              "@authors@", parameters.Authors |> String.separated ", "
              "@project@", parameters.Project
              "@summary@", parameters.Summary |> toSingleLine
              "@description@", parameters.Description |> toSingleLine
              "@tags@", parameters.Tags
              "@releaseNotes@", parameters.ReleaseNotes
              "@copyright@", parameters.Copyright
              "@language@", parameters.Language ]
            |> List.map (fun (placeholder, replacement) -> placeholder, xmlEncode replacement)
            |> List.append
                [ "@dependencies@", dependenciesXml
                  "@references@", referencesXml
                  "@frameworkAssemblies@", frameworkAssembliesXml
                  "@contentFiles@", contentFilesXml
                  "@files@", filesXml ]

        Templates.replaceInFiles replacements [ specFile ]
        Trace.tracefn "Created nuspec file %s" specFile
        specFile

    let private createNuSpecFromTemplateIfNotProjFile parameters nuSpecOrProjFile =
        let nuSpecOrProjFileInfo = FileInfo.ofPath nuSpecOrProjFile

        match nuSpecOrProjFileInfo.Extension.ToLower().EndsWith("proj") with
        | true -> None
        | false -> Some(createNuSpecFromTemplate parameters nuSpecOrProjFileInfo)


    let private propertiesParam =
        function
        | [] -> ""
        | lst ->
            "-Properties "
            + (lst |> List.map (fun p -> (fst p) + "=\"" + (snd p) + "\"") |> String.concat ";")

    /// Creates a NuGet package without templating (including symbols package if enabled)
    let private pack parameters nuspecFile =
        if String.isNotNullOrEmpty parameters.AccessKey then
            TraceSecrets.register "<NuGetKey>" parameters.AccessKey

        if String.isNotNullOrEmpty parameters.SymbolAccessKey then
            TraceSecrets.register "<NuGetSymbolKey>" parameters.SymbolAccessKey

        let nuspecFile = Path.getFullName nuspecFile
        let properties = propertiesParam parameters.Properties

        let basePath =
            parameters.BasePath
            |> Option.map (sprintf "-BasePath \"%s\"")
            |> Option.defaultValue ""

        let outputPath =
            (Path.getFullName (parameters.OutputPath.TrimEnd('\\').TrimEnd('/')))

        let packageAnalysis =
            if parameters.NoPackageAnalysis then
                "-NoPackageAnalysis"
            else
                ""

        let defaultExcludes =
            if parameters.NoDefaultExcludes then
                "-NoDefaultExcludes"
            else
                ""

        let includeReferencedProjects =
            if parameters.IncludeReferencedProjects then
                "-IncludeReferencedProjects"
            else
                ""

        if Directory.Exists parameters.OutputPath |> not then
            failwithf "OutputDir %s does not exist." parameters.OutputPath

        let execute args =
            let errorResults = System.Collections.Generic.List<string>()
            let outputResults = System.Collections.Generic.List<string>()

            let errorF msg = errorResults.Add msg

            let messageF msg = outputResults.Add msg

            let processResult =
                CreateProcess.fromRawCommandLine parameters.ToolPath args
                |> CreateProcess.withTimeout parameters.TimeOut
                |> CreateProcess.withFramework
                |> CreateProcess.withWorkingDirectory (Path.getFullName parameters.WorkingDir)
                |> CreateProcess.redirectOutput
                |> CreateProcess.withOutputEventsNotNull messageF errorF
                |> Proc.run

            if processResult.ExitCode <> 0 || errorResults.Count > 0 then
                failwithf
                    "Error during NuGet package creation. %s %s\r\n%s"
                    parameters.ToolPath
                    args
                    (String.toLines errorResults)

        match parameters.SymbolPackage with
        | NugetSymbolPackage.ProjectFile ->
            if not (String.isNullOrEmpty parameters.ProjectFile) then
                sprintf
                    "pack -Symbols -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
                    parameters.Version
                    outputPath
                    (Path.getFullName parameters.ProjectFile)
                    packageAnalysis
                    defaultExcludes
                    includeReferencedProjects
                    properties
                    basePath
                |> execute

            sprintf
                "pack -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
                parameters.Version
                outputPath
                nuspecFile
                packageAnalysis
                defaultExcludes
                includeReferencedProjects
                properties
                basePath
            |> execute
        | NugetSymbolPackage.Nuspec ->
            sprintf
                "pack -Symbols -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
                parameters.Version
                outputPath
                nuspecFile
                packageAnalysis
                defaultExcludes
                includeReferencedProjects
                properties
                basePath
            |> execute
        | _ ->
            sprintf
                "pack -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
                parameters.Version
                outputPath
                nuspecFile
                packageAnalysis
                defaultExcludes
                includeReferencedProjects
                properties
                basePath
            |> execute

    /// <summary>
    /// dotnet nuget push command options
    /// </summary>
    type NuGetPushParams =
        {
            /// Disables buffering when pushing to an HTTP(S) server to reduce memory usage.
            DisableBuffering: bool

            /// The API key for the server
            ApiKey: string option

            /// Doesn't push symbols (even if present).
            NoSymbols: bool

            /// Doesn't append "api/v2/package" to the source URL.
            NoServiceEndpoint: bool

            /// Specifies the server URL. This option is required unless DefaultPushSource config value is set in
            /// the NuGet config file.
            Source: string option

            /// The API key for the symbol server.
            SymbolApiKey: string option

            /// Specifies the symbol server URL.
            SymbolSource: string option

            /// Specifies the timeout for pushing to a server.
            Timeout: TimeSpan option

            /// Number of times to retry pushing the package
            PushTrials: int
        }

        static member Create() =
            { DisableBuffering = false
              ApiKey = None
              NoSymbols = false
              NoServiceEndpoint = false
              Source = None
              SymbolApiKey = None
              SymbolSource = None
              Timeout = None
              PushTrials = 5 }

    type NuGetParams with

        member internal x.NuGetPushOptions =
            let normalize str =
                if String.isNullOrEmpty str then None else Some str

            { DisableBuffering = false
              ApiKey = normalize x.AccessKey
              NoSymbols = false
              NoServiceEndpoint = false
              Source = normalize x.PublishUrl
              SymbolApiKey = normalize x.SymbolAccessKey
              SymbolSource = normalize x.SymbolPublishUrl
              Timeout = Some x.TimeOut
              PushTrials = x.PublishTrials }

        member internal x.ToolOptions = ToolOptions.Create x.ToolPath "push" x.WorkingDir true
        member internal x.Nupkg = (x.OutputPath @@ packageFileName x |> Path.getFullName)

    let internal toPushCliArgs param =
        let toSeconds (t: TimeSpan) = t.TotalSeconds |> int |> string

        let stringToArg name values =
            values |> List.collect (fun v -> [ "-" + name; v ])

        let boolToArg name value =
            match value with
            | true -> [ sprintf "-%s" name ]
            | false -> []

        [ param.ApiKey |> Option.toList |> stringToArg "ApiKey"
          param.DisableBuffering |> boolToArg "DisableBuffering"
          param.NoSymbols |> boolToArg "NoSymbols"
          param.NoServiceEndpoint |> boolToArg "NoServiceEndpoint"
          param.Source |> Option.toList |> stringToArg "Source"
          param.SymbolApiKey |> Option.toList |> stringToArg "SymbolApiKey"
          param.SymbolSource |> Option.toList |> stringToArg "SymbolSource"
          param.Timeout |> Option.map toSeconds |> Option.toList |> stringToArg "Timeout" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)

    let rec private push (options: ToolOptions) (parameters: NuGetPushParams) nupkg =
        parameters.ApiKey
        |> Option.iter (fun key -> TraceSecrets.register "<NuGetKey>" key)

        parameters.SymbolApiKey
        |> Option.iter (fun key -> TraceSecrets.register "<NuGetSymbolKey>" key)

        let pushArgs = parameters |> toPushCliArgs |> Args.toWindowsCommandLine
        let args = sprintf "%s \"%s\" %s" options.Command nupkg pushArgs

        sprintf
            "%s %s in WorkingDir: %s Trials left: %d"
            options.ToolPath
            args
            (Path.getFullName options.WorkingDir)
            parameters.PushTrials
        |> TraceSecrets.guardMessage
        |> Trace.trace

        try
            let result =
                CreateProcess.fromRawCommandLine options.ToolPath args
                |> CreateProcess.withWorkingDirectory options.WorkingDir
                |> CreateProcess.withTimeout (parameters.Timeout |> Option.defaultValue (TimeSpan.FromMinutes 5.0))
                |> (fun p ->
                    if options.IsFullFramework then
                        p |> CreateProcess.withFramework
                    else
                        p)
                |> Proc.run

            if result.ExitCode <> 0 then
                sprintf "Error during NuGet push. %s %s" options.ToolPath args
                |> TraceSecrets.guardMessage
                |> failwith

        with _ when parameters.PushTrials > 0 ->
            push options { parameters with PushTrials = parameters.PushTrials - 1 } nupkg

    let private publish (parameters: NuGetParams) =
        push parameters.ToolOptions parameters.NuGetPushOptions parameters.Nupkg

    /// push package to symbol server (and try again if something fails)
    let rec private publishSymbols parameters =
        let args =
            sprintf "push -source %s \"%s\" %s" parameters.PublishUrl (packageFileName parameters) parameters.AccessKey

        Trace.tracefn
            "%s %s in WorkingDir: %s Trials left: %d"
            parameters.ToolPath
            args
            (Path.getFullName parameters.WorkingDir)
            parameters.PublishTrials

        try
            let result =
                let tracing = Process.shouldEnableProcessTracing ()

                try
                    Process.setEnableProcessTracing false

                    let processResult =
                        CreateProcess.fromRawCommandLine parameters.ToolPath args
                        |> CreateProcess.withTimeout parameters.TimeOut
                        |> CreateProcess.withFramework
                        |> CreateProcess.withWorkingDirectory (Path.getFullName parameters.WorkingDir)
                        |> Proc.run

                    processResult.ExitCode
                finally
                    Process.setEnableProcessTracing tracing

            if result <> 0 then
                sprintf "Error during NuGet symbol push. %s %s" parameters.ToolPath args
                |> TraceSecrets.guardMessage
                |> failwith
        with _ when parameters.PublishTrials > 0 ->
            publish
                { parameters with
                    PublishTrials = parameters.PublishTrials - 1 }

    /// <summary>
    /// Creates a new NuGet package based on the given .nuspec or project file.
    /// The .nuspec / projectfile is passed as-is (no templating is performed)
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default NuGet parameters.</param>
    /// <param name="nuspecOrProjectFile">The .nuspec or project file name.</param>
    let NuGetPackDirectly setParams nuspecOrProjectFile =
        use __ = Trace.traceTask "NuGetPackDirectly" nuspecOrProjectFile
        let parameters = NuGetDefaults() |> setParams

        try
            pack parameters nuspecOrProjectFile
        with exn ->
            (if not (isNull exn.InnerException) then
                 exn.Message + "\r\n" + exn.InnerException.Message
             else
                 exn.Message)
            |> TraceSecrets.guardMessage
            |> failwith

        __.MarkSuccess()

    /// <summary>
    /// Creates a new NuGet package based on the given .nuspec or project file.
    /// Template parameter substitution is performed when passing a .nuspec
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default NuGet parameters.</param>
    /// <param name="nuspecOrProjectFile">The .nuspec or project file name.</param>
    let NuGetPack setParams nuspecOrProjectFile =
        use __ = Trace.traceTask "NuGetPack" nuspecOrProjectFile
        let parameters = NuGetDefaults() |> setParams

        try
            match (createNuSpecFromTemplateIfNotProjFile parameters nuspecOrProjectFile) with
            | Some nuspecTemplateFile ->
                pack parameters nuspecTemplateFile
                File.delete nuspecTemplateFile
            | None -> pack parameters nuspecOrProjectFile
        with exn ->
            (if not (isNull exn.InnerException) then
                 exn.Message + "\r\n" + exn.InnerException.Message
             else
                 exn.Message)
            |> TraceSecrets.guardMessage
            |> failwith

        __.MarkSuccess()

    /// <summary>
    /// Publishes a NuGet package to the nuget server.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default NuGet parameters.</param>
    let NuGetPublish setParams =
        let parameters = NuGetDefaults() |> setParams
        use __ = Trace.traceTask "NuGet-Push" (packageFileName parameters)

        try
            publish parameters
        with exn ->
            if not (isNull exn.InnerException) then
                exn.Message + "\r\n" + exn.InnerException.Message
            else
                exn.Message
            |> TraceSecrets.guardMessage
            |> failwith

        __.MarkSuccess()

    /// <summary>
    /// Creates a new NuGet package, and optionally publishes it.
    /// Template parameter substitution is performed when passing a .nuspec
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default NuGet parameters.</param>
    /// <param name="nuspecOrProjectFile">The .nuspec file name.</param>
    let NuGet setParams nuspecOrProjectFile =
        use __ = Trace.traceTask "NuGet" nuspecOrProjectFile
        let parameters = NuGetDefaults() |> setParams

        try
            match (createNuSpecFromTemplateIfNotProjFile parameters nuspecOrProjectFile) with
            | Some nuspecTemplateFile ->
                pack parameters nuspecTemplateFile
                File.delete nuspecTemplateFile
            | None -> pack parameters nuspecOrProjectFile

            if parameters.Publish then
                publish parameters

                if not (isNull parameters.ProjectFile) then
                    publishSymbols parameters
        with exn ->
            (if not (isNull exn.InnerException) then
                 exn.Message + "\r\n" + exn.InnerException.Message
             else
                 exn.Message)
            |> TraceSecrets.guardMessage
            |> failwith

        __.MarkSuccess()

    /// <summary>
    /// NuSpec metadata type Please see
    /// <a href="https://docs.microsoft.com/en-us/nuget/reference/nuspec">NuSpec reference</a>
    /// </summary>
    type NuSpecPackage =
        {
            /// The case-insensitive package identifier
            Id: string

            /// The version of the package, following the major.minor.patch pattern. Version numbers may
            /// include a pre-release suffix
            Version: string

            /// A comma-separated list of packages authors, matching the profile names on nuget.org.
            Authors: string

            /// A comma-separated list of the package creators using profile names on nuget.org.
            Owners: string

            /// A URL for the package's home page, often shown in UI displays as well as nuget.org.
            Url: string

            /// Holds if the package is the latest version published or not
            IsLatestVersion: bool

            /// The creation date of the package
            Created: DateTime

            /// The published date of the package
            Published: DateTime

            /// The unique hash of the package
            PackageHash: string

            /// The package hash algorithm used
            PackageHashAlgorithm: string

            /// package license URL
            LicenseUrl: string

            /// The package project URL
            ProjectUrl: string

            /// Mark if the package need usage acceptance before using it by license
            RequireLicenseAcceptance: bool

            /// The package description
            Description: string

            /// The package language
            Language: string

            /// The release notes file of the package
            ReleaseNotes: string

            /// tags referencing the package
            Tags: string
        }

        member x.Name = sprintf "%s %s" x.Id x.Version
        override x.ToString() = x.Name
        member x.DirectoryName = sprintf "%s.%s" x.Id x.Version
        member x.FileName = sprintf "%s.%s.nupkg" x.Id x.Version

    /// <summary>
    /// Parses nuspec metadata from a nuspec file.
    /// </summary>
    ///
    /// <param name="nuspec">The .nuspec file content.</param>
    let getNuspecProperties (nuspec: string) =
        let doc = Xml.createDoc nuspec

        let namespaces =
            [ "x", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"
              "y", "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"
              "default", ""
              "inDoc", doc.DocumentElement.NamespaceURI ]

        let getValue name =
            let getWith ns =
                try
                    doc
                    |> Xml.selectXPathValue (sprintf "%s:metadata/%s:%s" ns ns name) namespaces
                    |> Some
                with exn ->
                    None

            namespaces
            |> Seq.map fst
            |> Seq.tryPick (fun ns -> getWith ns)
            |> (fun x -> if x.IsSome then x.Value else "")

        { Id = getValue "id"
          Version = getValue "version"
          Authors = getValue "authors"
          Owners = getValue "owners"
          LicenseUrl = getValue "licenseUrl"
          ProjectUrl = getValue "projectUrl"
          RequireLicenseAcceptance = (getValue "requireLicenseAcceptance").ToLower() = "true"
          Description = getValue "description"
          Language = getValue "language"
          Tags = getValue "tags"
          ReleaseNotes = getValue "releaseNotes"
          Url = String.Empty
          IsLatestVersion = false
          Created = DateTime.MinValue
          Published = DateTime.MinValue
          PackageHash = String.Empty
          PackageHashAlgorithm = String.Empty }

    /// <summary>
    /// NuGet package information
    /// </summary>
    type NugetPackageInfo =
        {
            /// The case-insensitive package identifier
            Id: string

            /// The version of the package, following the major.minor.patch pattern. Version numbers may
            /// include a pre-release suffix
            Version: string

            /// The package description
            Description: string

            /// The package summary notes
            Summary: string

            /// Holds if the package is the latest version published or not
            IsLatestVersion: bool

            /// A comma-separated list of packages authors, matching the profile names on nuget.org.
            Authors: string

            /// A comma-separated list of the package creators using profile names on nuget.org.
            Owners: string

            /// tags referencing the package
            Tags: string

            /// The package project URL
            ProjectUrl: string

            /// package license URL
            LicenseUrl: string

            /// The package title
            Title: string
        }

    /// Default NuGet feed. Using V3 feed: <c>https://api.nuget.org/v3/index.json</c>
    let galleryV3 = "https://api.nuget.org/v3/index.json"

#if NETSTANDARD
    open System.Net.Http
    open Newtonsoft.Json.Linq

    type WebClient = HttpClient

    type HttpClient with

        member x.DownloadFileTaskAsync(uri: Uri, filePath: string) =
            async {
                let! response = x.GetAsync(uri) |> Async.AwaitTask

                use fileStream =
                    new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)

                do! response.Content.CopyToAsync(fileStream) |> Async.AwaitTask
                fileStream.Flush()
            }
            |> Async.StartAsTask

        member x.DownloadFileTaskAsync(uri: string, filePath: string) =
            x.DownloadFileTaskAsync(Uri uri, filePath)

        member x.DownloadFile(uri: string, filePath: string) =
            x.DownloadFileTaskAsync(uri, filePath).GetAwaiter().GetResult()

        member x.DownloadFile(uri: Uri, filePath: string) =
            x.DownloadFileTaskAsync(uri, filePath).GetAwaiter().GetResult()

        member x.DownloadStringTaskAsync(uri: Uri) =
            async {
                let! response = x.GetAsync(uri) |> Async.AwaitTask
                let! result = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return result
            }
            |> Async.StartAsTask

        member x.DownloadStringTaskAsync(uri: string) = x.DownloadStringTaskAsync(Uri uri)

        member x.DownloadString(uri: string) =
            x.DownloadStringTaskAsync(uri).GetAwaiter().GetResult()

        member x.DownloadString(uri: Uri) =
            x.DownloadStringTaskAsync(uri).GetAwaiter().GetResult()

        member x.DownloadDataTaskAsync(uri: Uri) =
            async {
                let! response = x.GetAsync(uri) |> Async.AwaitTask
                let! result = response.Content.ReadAsByteArrayAsync() |> Async.AwaitTask
                return result
            }
            |> Async.StartAsTask

        member x.DownloadDataTaskAsync(uri: string) = x.DownloadDataTaskAsync(Uri uri)

        member x.DownloadData(uri: string) =
            x.DownloadDataTaskAsync(uri).GetAwaiter().GetResult()

        member x.DownloadData(uri: Uri) =
            x.DownloadDataTaskAsync(uri).GetAwaiter().GetResult()

        member x.UploadFileAsMultipart (url: Uri) filename =
            let fileTemplate =
                "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n"

            let boundary =
                "---------------------------"
                + DateTime.Now.Ticks.ToString("x", System.Globalization.CultureInfo.InvariantCulture)

            let fileInfo = FileInfo(Path.GetFullPath(filename))

            let fileHeaderBytes =
                String.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    fileTemplate,
                    boundary,
                    "package",
                    "package",
                    "application/octet-stream"
                )
                |> Encoding.UTF8.GetBytes

            let newlineBytes = Environment.NewLine |> Encoding.UTF8.GetBytes

            let trailerBytes =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "--{0}--", boundary)
                |> Encoding.UTF8.GetBytes

            x.DefaultRequestHeaders.Add("ContentType", "multipart/form-data; boundary=" + boundary)
            use stream = new MemoryStream() // x.OpenWrite(url, "PUT")
            stream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length)
            use fileStream = File.OpenRead fileInfo.FullName
            fileStream.CopyTo(stream, (4 * 1024))
            stream.Write(newlineBytes, 0, newlineBytes.Length)
            stream.Write(trailerBytes, 0, trailerBytes.Length)
            stream.Write(newlineBytes, 0, newlineBytes.Length)
            stream.Position <- 0L
            x.PutAsync(url, new StreamContent(stream)).GetAwaiter().GetResult()

    let internal addAcceptHeader (client: HttpClient) (contentType: string) =
        for headerVal in contentType.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries) do
            client.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(headerVal))

    let internal addHeader (client: HttpClient) (headerKey: string) (headerVal: string) =
        client.DefaultRequestHeaders.Add(headerKey, headerVal)
#else

    open Newtonsoft.Json.Linq
    open System.Net

    type WebClient with

        member x.UploadFileAsMultipart (url: Uri) filename =
            let fileTemplate =
                "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n"

            let boundary =
                "---------------------------"
                + DateTime.Now.Ticks.ToString("x", System.Globalization.CultureInfo.InvariantCulture)

            let fileInfo = (new FileInfo(Path.GetFullPath(filename)))

            let fileHeaderBytes =
                System.String.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    fileTemplate,
                    boundary,
                    "package",
                    "package",
                    "application/octet-stream"
                )
                |> Encoding.UTF8.GetBytes
            // we use a windows-style newline rather than Environment.NewLine for compatibility
            let newlineBytes = "\r\n" |> Encoding.UTF8.GetBytes

            let trailerbytes =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "--{0}--", boundary)
                |> Encoding.UTF8.GetBytes

            x.Headers.Add(HttpRequestHeader.ContentType, "multipart/form-data; boundary=" + boundary)
            use stream = x.OpenWrite(url, "PUT")
            stream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length)
            use fileStream = File.OpenRead fileInfo.FullName
            fileStream.CopyTo(stream, (4 * 1024))
            stream.Write(newlineBytes, 0, newlineBytes.Length)
            stream.Write(trailerbytes, 0, trailerbytes.Length)
            stream.Write(newlineBytes, 0, newlineBytes.Length)
            ()

    type WebClient = System.Net.WebClient

    let internal addAcceptHeader (client: WebClient) contentType =
        client.Headers.Add(HttpRequestHeader.Accept, contentType)

    let internal addHeader (client: WebClient) (headerKey: string) (headerVal: string) =
        client.Headers.Add(headerKey, headerVal)

#endif

    let private webClient = new WebClient()

    /// [omit]
    let discoverRepoUrl =
        lazy
            (let resp = webClient.DownloadString(galleryV3)
             let json = JObject.Parse resp

             let nugetSearchResource =
                 (json.Item("resources") :?> JArray)
                 |> Seq.find (fun resource -> resource.Item("@type").ToString() = "SearchQueryService")
                 :?> JObject

             nugetSearchResource.Item("@id").ToString())

    /// [omit]
    let getRepoUrl () = discoverRepoUrl.Force()

    /// [omit]
    let extractFeedPackageFromJson (data: JObject) isLatestVersion =
        { Id = data["id"].ToString()
          Version = data["version"].ToString()
          Description = data["description"].ToString()
          Summary = data["summary"].ToString()
          IsLatestVersion = isLatestVersion
          Authors = String.Join(",", data["authors"] :?> JArray)
          Owners = String.Join(",", data["authors"] :?> JArray)
          Tags = String.Join(",", data["tags"] :?> JArray)
          ProjectUrl = data["projectUrl"].ToString()
          LicenseUrl = data["licenseUrl"].ToString()
          Title = data["title"].ToString() }

    /// <summary>
    /// Gets a Package information from NuGet feed by package id.
    /// </summary>
    ///
    /// <param name="repoUrl">Query endpoint of NuGet search service</param>
    /// <param name="packageName">The package to get</param>
    /// <param name="version">The specific version to get</param>
    let getPackage (repoUrl: string) (packageName: string) (version: string) =
        let url: string = repoUrl.TrimEnd('/') + "?q=packageid:" + packageName + "&take=1"
        let resp = webClient.DownloadString(url)
        let json = JObject.Parse resp
        let data = (json["data"] :?> JArray)[0] :?> JObject
        let packageVersions = (data["versions"] :?> JArray).ToObject<List<JObject>>()

        let versionExists =
            packageVersions
            |> List.exists (fun listedVersion -> listedVersion["version"].ToString() = version)

        if not versionExists then
            failwithf "Requested %s for package %s is not registered on NuGet" version packageName

        let isLatest = (data["version"].ToString() = version)
        // set the requested version instead of latest.
        data["version"] <- JValue version
        extractFeedPackageFromJson data isLatest

    /// <summary>
    /// Gets the latest published package from NuGet feed by package id.
    /// </summary>
    ///
    /// <param name="repoUrl">Query endpoint of NuGet search service</param>
    /// <param name="packageName">The package to get</param>
    let getLatestPackage (repoUrl: string) packageName =
        let url: string = repoUrl.TrimEnd('/') + "?q=packageid:" + packageName + "&take=1"
        let resp = webClient.DownloadString(url)
        let json = JObject.Parse resp
        let data = (json["data"] :?> JArray)[0] :?> JObject
        extractFeedPackageFromJson data true

    /// <summary>
    /// Search NuGet query endpoint for packages matching given name by title
    /// </summary>
    ///
    /// <param name="repoUrl">Query endpoint of NuGet search service</param>
    /// <param name="packageName">The package to search for</param>
    let searchByTitle (repoUrl: string) (packageName: string) =
        let url: string = repoUrl.TrimEnd('/') + "?q=title:" + packageName
        let resp = webClient.DownloadString(url)
        let json = JObject.Parse resp
        let data = (json["data"] :?> JArray).ToObject<List<JObject>>()
        data |> List.map (fun datum -> extractFeedPackageFromJson datum false)

    /// [omit]
    let downloadPackage targetDir (package: NuSpecPackage) =
        Directory.ensure targetDir
        let targetFileName = targetDir @@ package.FileName

        Trace.tracefn
            "Downloading package %s %s from %s and saving it to %s"
            package.Id
            package.Version
            package.Url
            targetFileName

        webClient.DownloadFile(package.Url, targetFileName)
        targetFileName

    /// [omit]
    let argList name values =
        values
        |> Seq.collect (fun v -> [ "-" + name; sprintf @"""%s""" v ])
        |> String.concat " "

    /// <summary>
    /// Holds data for NuGet dependencies of a package
    /// </summary>
    type NuGetDependency =
        {
            /// The package Id
            Id: string

            /// The package version
            Version: SemVerInfo

            /// Mark if the dependency is a development (dev) dependency or not
            IsDevelopmentDependency: bool
        }

    /// <summary>
    /// Returns the dependencies from specified packages.config file
    /// </summary>
    ///
    /// <param name="packagesFile">The packages file to use</param>
    let getDependencies (packagesFile: string) =
        let xName = XName.op_Implicit

        let attribute name (e: XElement) =
            match e.Attribute(xName name) with
            | null -> ""
            | a -> a.Value

        let isDevDependency package =
            let value = attribute "developmentDependency" package
            String.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase)

        let doc = XDocument.Load packagesFile

        [ for package in doc.Descendants(xName "package") ->
              { Id = attribute "id" package
                Version = SemVer.parse (attribute "version" package)
                IsDevelopmentDependency = isDevDependency package } ]
