namespace Fake.DotNet

/// <summary>
/// Contains helper functions and task which allow to inspect, create and publish
/// <a href="https://www.nuget.org/">NuGet</a> packages with
/// <a href="http://fsprojects.github.io/Paket/index.html">Paket</a>.
/// </summary>
[<RequireQualifiedAccess>]
module Paket =

    open System
    open System.IO
    open System.Xml.Linq
    open System.Text.RegularExpressions
    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators
    open Fake.IO.Globbing.Operators

    /// <summary>
    /// Paket pack parameter type
    /// </summary>
    type PaketPackParams =
        {
            /// paket tool path
            ToolPath: string

            /// The kind of application ToolPath references
            ToolType: ToolType

            /// The timeout for the toll commands execution
            TimeOut: TimeSpan

            /// The packaging version
            Version: string

            /// version number to use for package ID; may be repeated
            SpecificVersions: (string * string) list

            /// use version constraints from paket.lock instead of paket.dependencies
            LockDependencies: bool

            /// the release notes to include in output NuGet package
            ReleaseNotes: string

            /// build configuration that should be packaged (default: Release)
            BuildConfig: string

            /// build platform that should be packaged (default: check all known platform targets)
            BuildPlatform: string

            /// pack a single paket.template file
            TemplateFile: string

            /// exclude paket.template file by package ID; may be repeated
            ExcludedTemplates: string list

            /// the directory to execute command in
            WorkingDir: string

            /// output directory for <c>.nupkg</c> files
            OutputPath: string

            /// homepage URL for the package
            ProjectUrl: string

            /// create symbol and source packages in addition to library and content packages
            Symbols: bool

            /// include symbols and source from referenced projects
            IncludeReferencedProjects: bool

            /// use version constraints from paket.lock instead of paket.dependencies and add them as a minimum
            /// version; --lock-dependencies overrides this option
            MinimumFromLockFile: bool

            /// pin dependencies generated from project references to exact versions (<c>=</c>)
            /// instead of using minimum versions (<c>&gt;=</c>); with <c>--lock-dependencies</c> project
            /// references will be pinned even if this option is not specified
            PinProjectReferences: bool
        }

    let internal findPaketExecutable baseDir =
        ProcessUtils.findLocalTool "PAKET" "paket" [ Path.Combine(baseDir, ".paket") ]

    /// Paket pack default parameters
    let PaketPackDefaults () : PaketPackParams =
        { ToolPath = findPaketExecutable ""
          ToolType = ToolType.Create()
          TimeOut = TimeSpan.FromMinutes 5.
          Version = null
          SpecificVersions = []
          LockDependencies = false
          ReleaseNotes = null
          BuildConfig = null
          BuildPlatform = null
          TemplateFile = null
          ProjectUrl = null
          ExcludedTemplates = []
          WorkingDir = "."
          OutputPath = "./temp"
          Symbols = false
          IncludeReferencedProjects = false
          MinimumFromLockFile = false
          PinProjectReferences = false }

    /// <summary>
    /// Paket push parameter type
    /// </summary>
    type PaketPushParams =
        {
            /// paket tool path
            ToolPath: string

            /// The kind of application ToolPath references
            ToolType: ToolType

            /// The timeout for the toll commands execution
            TimeOut: TimeSpan

            /// URL of the NuGet feed
            PublishUrl: string

            /// API endpoint to push to (default: /api/v2/package)
            EndPoint: string

            /// the directory to execute command in
            WorkingDir: string

            /// the number of parallel processes to use
            DegreeOfParallelism: int

            /// API key for the URL (default: value of the NUGET_KEY environment variable)
            ApiKey: string
        }

    /// Paket push default parameters
    let PaketPushDefaults () : PaketPushParams =
        { ToolPath = findPaketExecutable ""
          ToolType = ToolType.Create()
          TimeOut = TimeSpan.MaxValue
          PublishUrl = null
          EndPoint = null
          WorkingDir = "./temp"
          DegreeOfParallelism = 8
          ApiKey = null }

    /// <summary>
    /// Paket restore packages type
    /// </summary>
    type PaketRestoreParams =
        {
            /// paket tool path
            ToolPath: string

            /// The kind of application ToolPath references
            ToolType: ToolType

            /// The timeout for the toll commands execution
            TimeOut: TimeSpan

            /// the directory to execute command in
            WorkingDir: string

            /// force download and re-installation of all dependencies
            ForceDownloadOfPackages: bool

            /// only restore packages that are referenced by paket.references files
            OnlyReferencedFiles: bool

            /// restore dependencies of a single group
            Group: string

            /// restore packages from a paket.references file; may be repeated
            ReferenceFiles: string list
        }

    /// Paket restore default parameters
    let PaketRestoreDefaults () : PaketRestoreParams =
        { ToolPath = findPaketExecutable ""
          ToolType = ToolType.Create()
          TimeOut = TimeSpan.MaxValue
          WorkingDir = "."
          ForceDownloadOfPackages = false
          OnlyReferencedFiles = false
          ReferenceFiles = []
          Group = "" }

    let private startPaket (toolType: ToolType) toolPath workDir timeout args =
        CreateProcess.fromCommand (RawCommand(toolPath, args))
        |> CreateProcess.withToolType toolType
        |> CreateProcess.withWorkingDirectory workDir
        |> CreateProcess.withTimeout timeout

    let private start (c: CreateProcess<ProcessResult<_>>) = c |> Proc.run |> (fun r -> r.ExitCode)

    type internal StartType =
        | PushFile of parameters: PaketPushParams * files: string
        | Pack of parameters: PaketPackParams
        | Restore of parameters: PaketRestoreParams

    let internal createProcess (runType: StartType) =
        match runType with
        | PushFile (parameters, file) ->
            Arguments.OfArgs [ "push" ]
            |> Arguments.appendNotEmpty "--url" parameters.PublishUrl
            |> Arguments.appendNotEmpty "--endpoint" parameters.EndPoint
            |> Arguments.appendNotEmpty "--api-key" parameters.ApiKey
            |> Arguments.append [ file ]
            |> startPaket parameters.ToolType parameters.ToolPath parameters.WorkingDir parameters.TimeOut
        | Pack parameters ->
            let xmlEncode (notEncodedText: string) =
                if String.IsNullOrWhiteSpace notEncodedText then
                    ""
                else
                    XText(notEncodedText).ToString().Replace("ß", "&szlig;")

            Arguments.OfArgs [ "pack" ]
            |> Arguments.appendNotEmpty "--version" parameters.Version
            |> Arguments.appendNotEmpty "--build-config" parameters.BuildConfig
            |> Arguments.appendNotEmpty "--build-platform" parameters.BuildPlatform
            |> Arguments.appendNotEmpty "--template" parameters.TemplateFile
            |> Arguments.appendNotEmpty "--release-notes" (xmlEncode parameters.ReleaseNotes)
            |> Arguments.appendNotEmpty "--project-url" parameters.ProjectUrl
            |> Arguments.appendIf parameters.LockDependencies "--lock-dependencies"
            |> Arguments.appendIf parameters.MinimumFromLockFile "--minimum-from-lock-file"
            |> Arguments.appendIf parameters.PinProjectReferences "--pin-project-references"
            |> Arguments.appendIf parameters.Symbols "--symbols"
            |> Arguments.appendIf parameters.IncludeReferencedProjects "--include-referenced-projects"
            |> List.foldBack (fun t -> Arguments.append [ "--exclude"; t ]) parameters.ExcludedTemplates
            |> List.foldBack
                (fun (id, v) -> Arguments.append [ "--specific-version"; id; v ])
                parameters.SpecificVersions
            |> Arguments.append [ parameters.OutputPath ]
            |> startPaket parameters.ToolType parameters.ToolPath parameters.WorkingDir parameters.TimeOut
        | Restore parameters ->
            Arguments.OfArgs [ "restore" ]
            |> Arguments.appendNotEmpty "--group" parameters.Group
            |> Arguments.appendIf parameters.ForceDownloadOfPackages "--force"
            |> Arguments.appendIf parameters.OnlyReferencedFiles "--only-referenced"
            |> List.foldBack (fun ref -> Arguments.append [ "--references-file"; ref ]) parameters.ReferenceFiles
            |> startPaket parameters.ToolType parameters.ToolPath parameters.WorkingDir parameters.TimeOut

    /// <summary>
    /// Creates a new NuGet package by using Paket pack on all paket.template files in the working directory.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default parameters.</param>
    let pack setParams =
        let parameters: PaketPackParams = PaketPackDefaults() |> setParams
        use __ = Trace.traceTask "PaketPack" parameters.WorkingDir

        let packResult = createProcess (Pack parameters) |> start

        if packResult <> 0 then
            failwithf "Error during packing %s." parameters.WorkingDir

        __.MarkSuccess()


    /// <summary>
    /// Pushes the given NuGet packages to the server by using Paket push.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default parameters.</param>
    /// <param name="files">The files to be pushed to the server.</param>
    let pushFiles setParams files =
        let parameters: PaketPushParams = PaketPushDefaults() |> setParams

        TraceSecrets.register "<PaketApiKey>" parameters.ApiKey

        match Environment.environVarOrNone "nugetkey" with
        | Some k -> TraceSecrets.register "<PaketApiKey>" k
        | None -> ()

        match Environment.environVarOrNone "nuget-key" with
        | Some k -> TraceSecrets.register "<PaketApiKey>" k
        | None -> ()

        let packages = Seq.toList files
        use __ = Trace.traceTask "PaketPush" (String.separated ", " packages)

        if parameters.DegreeOfParallelism > 0 then
            /// Returns a sequence that yields chunks of length n.
            /// Each chunk is returned as a list.
            let split length (xs: seq<'T>) =
                let rec loop xs =
                    [ yield Seq.truncate length xs |> Seq.toList
                      match Seq.length xs <= length with
                      | false -> yield! loop (Seq.skip length xs)
                      | true -> () ]

                loop xs

            for chunk in split parameters.DegreeOfParallelism packages do
                let tasks =
                    chunk
                    |> Seq.toArray
                    |> Array.map (fun package ->
                        async {
                            let pushResult = createProcess (PushFile(parameters, package)) |> start

                            if pushResult <> 0 then
                                failwithf "Error during pushing %s." package
                        })

                Async.Parallel tasks |> Async.RunSynchronously |> ignore

        else
            for package in packages do
                let pushResult = createProcess (PushFile(parameters, package)) |> start

                if pushResult <> 0 then
                    failwithf "Error during pushing %s." package

        __.MarkSuccess()

    /// <summary>
    /// Pushes all NuGet packages in the working dir to the server by using Paket push.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default parameters.</param>
    let push setParams =
        let parameters: PaketPushParams = PaketPushDefaults() |> setParams

        !!(parameters.WorkingDir @@ "/**/*.nupkg") |> pushFiles (fun _ -> parameters)

    /// <summary>
    /// Returns the dependencies from specified paket.references file
    /// </summary>
    ///
    /// <param name="referencesFile">Paket reference file to use</param>
    /// - `referencesFile` -
    let getDependenciesForReferencesFile (referencesFile: string) =
        let getReferenceFilePackages =
            let isSingleFile (line: string) = line.StartsWith "File:"
            let isGroupLine (line: string) = line.StartsWith "group "
            let notEmpty (line: string) = not <| String.IsNullOrWhiteSpace line

            let parsePackageName (line: string) =
                let parts = line.Split(' ')
                parts[0]

            File.ReadAllLines
            >> Array.filter notEmpty
            >> Array.map (fun s -> s.Trim())
            >> Array.filter (isSingleFile >> not)
            >> Array.filter (isGroupLine >> not)
            >> Array.map parsePackageName

        let getLockFilePackages =
            let getPaketLockFile referencesFile =
                let rec find dir =
                    let fi = FileInfo(dir </> "paket.lock")

                    if fi.Exists then
                        fi.FullName
                    else
                        find fi.Directory.Parent.FullName

                find <| FileInfo(referencesFile).Directory.FullName

            let breakInParts (line: string) =
                match Regex.Match(line, "^[ ]{4}([^ ].+) \((.+)\)") with
                | m when m.Success && m.Groups.Count = 3 -> Some(m.Groups[1].Value, m.Groups[2].Value)
                | _ -> None

            getPaketLockFile >> File.ReadAllLines >> Array.choose breakInParts

        let refLines = getReferenceFilePackages referencesFile

        getLockFilePackages referencesFile
        |> Array.filter (fun (n, _) ->
            refLines
            |> Array.exists (fun pn -> pn.Equals(n, StringComparison.OrdinalIgnoreCase)))

    /// <summary>
    /// Restores all packages referenced in either a paket.dependencies or a paket.references file using Paket
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default parameters.</param>
    let restore setParams =
        let parameters: PaketRestoreParams = PaketRestoreDefaults() |> setParams
        use __ = Trace.traceTask "PaketRestore" parameters.WorkingDir

        let restoreResult = createProcess (Restore parameters) |> start

        if restoreResult <> 0 then
            failwithf "Error during restore %s." parameters.WorkingDir

        __.MarkSuccess()
