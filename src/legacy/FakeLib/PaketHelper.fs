/// Contains helper functions and task which allow to inspect, create and publish [NuGet](https://www.nuget.org/) packages with [Paket](http://fsprojects.github.io/Paket/index.html).
[<System.Obsolete "use Fake.DotNet.Paket instead">]
module Fake.Paket

#nowarn "44"
open System
open System.IO
open System.Xml.Linq
open System.Text.RegularExpressions

/// Paket pack parameter type
[<System.Obsolete "use Fake.DotNet.Paket instead">]
[<CLIMutable>]
type PaketPackParams =
    { ToolPath : string
      TimeOut : TimeSpan
      Version : string
      SpecificVersions : (string * string) list
      LockDependencies : bool
      ReleaseNotes : string
      BuildConfig : string
      BuildPlatform : string
      TemplateFile : string
      ExcludedTemplates : string list
      WorkingDir : string
      OutputPath : string 
      ProjectUrl : string
      Symbols : bool
      IncludeReferencedProjects : bool
      MinimumFromLockFile : bool
      PinProjectReferences : bool }

/// Paket pack default parameters
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let PaketPackDefaults() : PaketPackParams =
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
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

/// Paket push parameter type
[<System.Obsolete "use Fake.DotNet.Paket instead">]
[<CLIMutable>]
type PaketPushParams =
    { ToolPath : string
      TimeOut : TimeSpan
      PublishUrl : string
      EndPoint : string
      WorkingDir : string
      DegreeOfParallelism : int
      ApiKey : string }

/// Paket push default parameters
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let PaketPushDefaults() : PaketPushParams =
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
      TimeOut = System.TimeSpan.MaxValue
      PublishUrl = null
      EndPoint =  null
      WorkingDir = "./temp"
      DegreeOfParallelism = 8
      ApiKey = null }

/// Paket restore packages type
[<System.Obsolete "use Fake.DotNet.Paket instead">]
[<CLIMutable>]
type PaketRestoreParams =
    { ToolPath : string
      TimeOut : TimeSpan
      WorkingDir : string
      ForceDownloadOfPackages : bool
      OnlyReferencedFiles: bool
      Group: string
      ReferenceFiles: string list }

/// Paket restore default parameters
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let PaketRestoreDefaults() : PaketRestoreParams = 
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
      TimeOut = System.TimeSpan.MaxValue
      WorkingDir = "."
      ForceDownloadOfPackages = false
      OnlyReferencedFiles = false
      ReferenceFiles = []
      Group = "" }

/// Creates a new NuGet package by using Paket pack on all paket.template files in the working directory.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default parameters.
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let Pack setParams =
    let parameters : PaketPackParams = PaketPackDefaults() |> setParams
    use __ = traceStartTaskUsing "PaketPack" parameters.WorkingDir

    let xmlEncode (notEncodedText : string) =
        if String.IsNullOrWhiteSpace notEncodedText then ""
        else XText(notEncodedText).ToString().Replace("ß", "&szlig;")
    let version = if String.IsNullOrWhiteSpace parameters.Version then "" else " --version " + ProcessHelper.toParam parameters.Version
    let buildConfig = if String.IsNullOrWhiteSpace parameters.BuildConfig then "" else " --build-config " + ProcessHelper.toParam parameters.BuildConfig
    let buildPlatform = if String.IsNullOrWhiteSpace parameters.BuildPlatform then "" else " --build-platform " + ProcessHelper.toParam parameters.BuildPlatform
    let templateFile = if String.IsNullOrWhiteSpace parameters.TemplateFile then "" else " --template " + ProcessHelper.toParam parameters.TemplateFile
    let lockDependencies = if parameters.LockDependencies then " --lock-dependencies" else ""
    let excludedTemplates = parameters.ExcludedTemplates |> Seq.map (fun t -> " --exclude " + t) |> String.concat " "
    let specificVersions = parameters.SpecificVersions |> Seq.map (fun (id,v) -> sprintf " --specific-version %s %s" id v) |> String.concat " "
    let releaseNotes = if String.IsNullOrWhiteSpace parameters.ReleaseNotes then "" else " --release-notes " + ProcessHelper.toParam (xmlEncode parameters.ReleaseNotes)
    let minimumFromLockFile = if parameters.MinimumFromLockFile then " --minimum-from-lock-file" else ""
    let pinProjectReferences = if parameters.PinProjectReferences then " --pin-project-references" else ""
    let symbols = if parameters.Symbols then " --symbols" else ""
    let includeReferencedProjects = if parameters.IncludeReferencedProjects then " --include-referenced-projects" else ""
    let projectUrl = if String.IsNullOrWhiteSpace parameters.ProjectUrl then "" else " --project-url " + ProcessHelper.toParam parameters.ProjectUrl

    let packResult =
        let cmdArgs = 
            sprintf "%s%s%s%s%s%s%s%s%s%s%s%s%s" 
                version specificVersions releaseNotes buildConfig buildPlatform templateFile lockDependencies excludedTemplates 
                symbols includeReferencedProjects minimumFromLockFile pinProjectReferences projectUrl
        ExecProcess
            (fun info ->
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- sprintf "pack output \"%s\" %s" parameters.OutputPath cmdArgs) parameters.TimeOut

    if packResult <> 0 then failwithf "Error during packing %s." parameters.WorkingDir

/// Pushes the given NuGet packages to the server by using Paket push.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default parameters.
///  - `files` - The files to be pushed to the server.
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let PushFiles setParams files =
    let parameters : PaketPushParams = PaketPushDefaults() |> setParams

    let packages = Seq.toList files
    let url = if String.IsNullOrWhiteSpace parameters.PublishUrl then "" else " url " + toParam parameters.PublishUrl
    let endpoint = if String.IsNullOrWhiteSpace parameters.EndPoint then "" else " endpoint " + toParam parameters.EndPoint
    let key = if String.IsNullOrWhiteSpace parameters.ApiKey then "" else " apikey " + toParam parameters.ApiKey

    use __ = traceStartTaskUsing "PaketPush" (separated ", " packages)

    if parameters.DegreeOfParallelism > 0 then
        /// Returns a sequence that yields chunks of length n.
        /// Each chunk is returned as a list.
        let split length (xs: seq<'T>) =
            let rec loop xs =
                [
                    yield Seq.truncate length xs |> Seq.toList
                    match Seq.length xs <= length with
                    | false -> yield! loop (Seq.skip length xs)
                    | true -> ()
                ]
            loop xs

        for chunk in split parameters.DegreeOfParallelism packages do
            let tasks =
                chunk
                |> Seq.toArray
                |> Array.map (fun package -> async {
                        let pushResult =
                            ExecProcess (fun info ->
                                info.FileName <- parameters.ToolPath
                                info.WorkingDirectory <- parameters.WorkingDir
                                info.Arguments <- sprintf "push %s%s%s %s" url endpoint key (toParam package)) parameters.TimeOut
                        if pushResult <> 0 then failwithf "Error during pushing %s." package })

            Async.Parallel tasks
            |> Async.RunSynchronously
            |> ignore

    else
        for package in packages do
            let pushResult =
                ExecProcess (fun info ->
                    info.FileName <- parameters.ToolPath
                    info.WorkingDirectory <- parameters.WorkingDir
                    info.Arguments <- sprintf "push %s%s%s %s" url endpoint key (toParam package)) parameters.TimeOut
            if pushResult <> 0 then failwithf "Error during pushing %s." package

/// Pushes all NuGet packages in the working dir to the server by using Paket push.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default parameters.
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let Push setParams =
    let parameters : PaketPushParams = PaketPushDefaults() |> setParams

    !! (parameters.WorkingDir @@ "/**/*.nupkg")
    |> PushFiles (fun _ -> parameters)

/// Returns the dependencies from specified paket.references file
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let GetDependenciesForReferencesFile (referencesFile:string) =
    let getReferenceFilePackages =
        let isSingleFile (line: string) = line.StartsWith "File:"
        let isGroupLine (line: string) = line.StartsWith "group "
        let notEmpty (line: string) = not <| String.IsNullOrWhiteSpace line
        let parsePackageName (line: string) =
            let parts = line.Split(' ')
            parts.[0]
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
                if fi.Exists then fi.FullName else find fi.Directory.Parent.FullName
            find <| FileInfo(referencesFile).Directory.FullName
        
        let breakInParts (line : string) = match Regex.Match(line,"^[ ]{4}([^ ].+) \((.+)\)") with
                                           | m when m.Success && m.Groups.Count = 3 -> Some (m.Groups.[1].Value, m.Groups.[2].Value)
                                           | _ -> None

        getPaketLockFile
        >> File.ReadAllLines
        >> Array.choose breakInParts

    let refLines = getReferenceFilePackages referencesFile

    getLockFilePackages referencesFile
    |> Array.filter (fun (n, _) -> refLines |> Array.exists (fun pn -> pn.Equals(n, StringComparison.InvariantCultureIgnoreCase)))

/// Restores all packages referenced in either a paket.dependencies or a paket.references file using Paket
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default parameters.
[<System.Obsolete "use Fake.DotNet.Paket instead">]
let Restore setParams = 
    let parameters : PaketRestoreParams = PaketRestoreDefaults() |> setParams
    let forceRestore = if parameters.ForceDownloadOfPackages then " --force " else ""
    let onlyReferenced = if parameters.OnlyReferencedFiles then " --only-referenced " else ""
    let groupArg = if parameters.Group <> "" then (sprintf " group %s " parameters.Group) else ""
    let referencedFiles = 
        if parameters.ReferenceFiles |> List.isEmpty |> not
        then (sprintf " --references-files %s " (System.String.Join(" ", parameters.ReferenceFiles)))
        else ""
    
    use __ = traceStartTaskUsing "PaketRestore" parameters.WorkingDir 

    let restoreResult = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- sprintf "restore %s%s%s%s" forceRestore onlyReferenced groupArg referencedFiles) parameters.TimeOut

    if restoreResult <> 0 then failwithf "Error during restore %s." parameters.WorkingDir
