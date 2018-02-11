
/// Contains helper functions and task which allow to inspect, create and publish [NuGet](https://www.nuget.org/) packages.
/// There is also a tutorial about [nuget package creating](../create-nuget-package.html) available.
module Fake.DotNet.NuGet.NuGet

open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.Core.String
open Fake.Core.BuildServer
open Fake.Core.Process
open Fake.Core
open System
open System.IO
open System.Text
open System
open System.IO
open System.Text
open System.Xml.Linq
open Fake.DotNet.NuGet.Restore

type NugetDependencies = (string * string) list

type NugetFrameworkDependencies =
    { FrameworkVersion : string
      Dependencies : NugetDependencies }

type NugetReferences = string list

type NugetFrameworkReferences =
    { FrameworkVersion : string
      References : NugetReferences }

type NugetFrameworkAssemblyReferences =
    { FrameworkVersions : string list
      AssemblyName : string }

type NugetSymbolPackage =
    /// Do not build symbol packages
    | None = 0
    /// Build a symbol package using a project file, if provided
    | ProjectFile = 1
    /// Build a symbol package using the nuspec file
    | Nuspec = 2

/// Nuget parameter type
type NuGetParams =
    { ToolPath : string
      TimeOut : TimeSpan
      Version : string
      Authors : string list
      Project : string
      Title : string
      Summary : string
      Description : string
      Tags : string
      ReleaseNotes : string
      Copyright : string
      WorkingDir : string
      BasePath : string option
      OutputPath : string
      PublishUrl : string
      AccessKey : string
      SymbolPublishUrl: string
      SymbolAccessKey: string
      NoDefaultExcludes : bool
      NoPackageAnalysis : bool
      ProjectFile : string
      Dependencies : NugetDependencies
      DependenciesByFramework : NugetFrameworkDependencies list
      References : NugetReferences
      ReferencesByFramework : NugetFrameworkReferences list
      FrameworkAssemblies : NugetFrameworkAssemblyReferences list
      IncludeReferencedProjects : bool
      PublishTrials : int
      Publish : bool
      SymbolPackage : NugetSymbolPackage
      Properties : list<string * string>
      Files : list<string*string option*string option>
      Language : string}

/// NuGet default parameters
let NuGetDefaults() =
    { ToolPath = findNuget (Shell.pwd() @@ "tools" @@ "NuGet")
      TimeOut = TimeSpan.FromMinutes 5.
      Version =
          if not isLocalBuild then buildVersion
          else "0.1.0.0"
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
      Language = null }

/// Creates a string which tells NuGet that you require exactly this package version.
let RequireExactly version = sprintf "[%s]" version

type BreakingPoint =
  | SemVer
  | Minor
  | Patch

// See https://docs.nuget.org/create/versioning
let RequireRange breakingPoint version =
  let v = SemVer.parse version
  match breakingPoint with
  | SemVer ->
    sprintf "[%s,%d.0)" version (v.Major + 1u)
  | Minor -> // Like Semver but we assume that the increase of a minor version is already breaking
    sprintf "[%s,%d.%d)" version v.Major (v.Minor + 1u)
  | Patch -> // Every update breaks
    version |> RequireExactly

let private packageFileName parameters = sprintf "%s.%s.nupkg" parameters.Project parameters.Version

[<Obsolete("I was just lazy in porting")>]
let private FullName = Path.getFullName

/// Gets the version no. for a given package in the deployments folder
let GetPackageVersion deploymentsDir package =
    try
        if Directory.Exists deploymentsDir |> not then
            failwithf "Package %s was not found, because the deployment directory %s doesn't exist." package deploymentsDir
        let version =
            let dirs = Directory.GetDirectories(deploymentsDir, sprintf "%s.*" package)
            if Seq.isEmpty dirs then failwithf "Package %s was not found." package
            let folder = Seq.head dirs
            let index = folder.LastIndexOf package + package.Length + 1
            if index < folder.Length then
                folder.Substring index
            else
                let nuspec = Directory.GetFiles(folder, sprintf "%s.nuspec" package) |> Seq.head
                let doc = System.Xml.Linq.XDocument.Load(nuspec)
                let vers = doc.Descendants(XName.Get("version", doc.Root.Name.NamespaceName))
                (Seq.head vers).Value

        Trace.logfn "Version %s found for package %s" version package
        version
    with
    | exn -> new Exception("Could not detect package version for " + package, exn) |> raise

let private replaceAccessKeys parameters (text:string) =
    let replaceKey key (str:string) =
        if isNullOrEmpty key then str
        else str.Replace(key, "PRIVATEKEY")

    text |> (replaceKey parameters.AccessKey >> replaceKey parameters.SymbolAccessKey)

let private createNuSpecFromTemplate parameters (templateNuSpec:FileInfo) =
    let specFile = parameters.WorkingDir @@ (templateNuSpec.Name.Replace("nuspec", "") + parameters.Version + ".nuspec")
                    |> Path.getFullName
    Trace.tracefn "Creating .nuspec file at %s" specFile

    templateNuSpec.CopyTo(specFile, true) |> ignore

    let getFrameworkGroup (frameworkTags : (string * string) seq) =
        frameworkTags
        |> Seq.map (fun (frameworkVersion, tags) ->
                    if isNullOrEmpty frameworkVersion then sprintf "<group>%s</group>" tags
                    else sprintf "<group targetFramework=\"%s\">%s</group>" frameworkVersion tags)
        |> toLines

    let getGroup items toTags =
        if items = [] then ""
        else sprintf "<group>%s</group>" (items |> toTags)

    let getReferencesTags references =
        references
        |> Seq.map (fun assembly -> sprintf "<reference file=\"%s\" />" assembly)
        |> toLines

    let references = getGroup parameters.References getReferencesTags

    let referencesByFramework =
        parameters.ReferencesByFramework
        |> Seq.map (fun x -> (x.FrameworkVersion, getReferencesTags x.References))
        |> getFrameworkGroup

    let referencesXml = sprintf "<references>%s</references>" (references + referencesByFramework)

    let getFrameworkAssemblyTags references =
        references
        |> Seq.map (fun x ->
                    if x.FrameworkVersions = [] then sprintf "<frameworkAssembly assemblyName=\"%s\" />" x.AssemblyName
                    else sprintf "<frameworkAssembly assemblyName=\"%s\" targetFramework=\"%s\" />" x.AssemblyName (x.FrameworkVersions |> separated ", "))
        |> toLines

    let frameworkAssembliesXml =
        if parameters.FrameworkAssemblies = [] then ""
        else sprintf "<frameworkAssemblies>%s</frameworkAssemblies>" (parameters.FrameworkAssemblies |> getFrameworkAssemblyTags)

    let getDependenciesTags dependencies =
        dependencies
        |> Seq.map (fun (package, version) -> sprintf "<dependency id=\"%s\" version=\"%s\" />" package version)
        |> toLines

    let dependencies = getGroup parameters.Dependencies getDependenciesTags

    let dependenciesByFramework =
        parameters.DependenciesByFramework
        |> Seq.map (fun x -> (x.FrameworkVersion, getDependenciesTags x.Dependencies))
        |> getFrameworkGroup

    let dependenciesXml = sprintf "<dependencies>%s</dependencies>" (dependencies + dependenciesByFramework)

    let filesTags =
        parameters.Files
        |> Seq.map (fun (source, target, exclude) ->
            let excludeStr =
                if exclude.IsSome then sprintf " exclude=\"%s\"" exclude.Value
                else String.Empty
            let targetStr =
                if target.IsSome then sprintf " target=\"%s\"" target.Value
                else String.Empty

            sprintf "<file src=\"%s\"%s%s />" source targetStr excludeStr)
        |> toLines

    let filesXml = sprintf "<files>%s</files>" filesTags

    let xmlEncode (notEncodedText : string) =
        if System.String.IsNullOrWhiteSpace notEncodedText then ""
        else XText(notEncodedText).ToString().Replace("�","&szlig;")

    let toSingleLine (text:string) =
        if text = null then null
        else text.Replace("\r", "").Replace("\n", "").Replace("  ", " ")

    let replacements =
        [ "@build.number@", parameters.Version
          "@title@", parameters.Title
          "@authors@", parameters.Authors |> separated ", "
          "@project@", parameters.Project
          "@summary@", parameters.Summary |> toSingleLine
          "@description@", parameters.Description |> toSingleLine
          "@tags@", parameters.Tags
          "@releaseNotes@", parameters.ReleaseNotes
          "@copyright@", parameters.Copyright
          "@language@", parameters.Language
        ]
        |> List.map (fun (placeholder, replacement) -> placeholder, xmlEncode replacement)
        |> List.append [ "@dependencies@", dependenciesXml
                         "@references@", referencesXml
                         "@frameworkAssemblies@", frameworkAssembliesXml
                         "@files@", filesXml ]

    Templates.processTemplates replacements [ specFile ]
    Trace.tracefn "Created nuspec file %s" specFile
    specFile

let private createNuSpecFromTemplateIfNotProjFile parameters nuSpecOrProjFile =
    let nuSpecOrProjFileInfo = FileInfo.ofPath nuSpecOrProjFile
    match nuSpecOrProjFileInfo.Extension.ToLower().EndsWith("proj") with
    | true -> None
    | false -> Some (createNuSpecFromTemplate parameters nuSpecOrProjFileInfo)


let private propertiesParam = function
    | [] -> ""
    | lst ->
        "-Properties " + (lst
                          |> List.map (fun p -> (fst p) + "=\"" + (snd p) + "\"")
                          |> String.concat ";")

/// Creates a NuGet package without templating (including symbols package if enabled)
let private pack parameters nuspecFile =
    let nuspecFile = Path.getFullName nuspecFile
    let properties = propertiesParam parameters.Properties
    let basePath = parameters.BasePath |> Option.map (sprintf "-BasePath \"%s\"") |> Option.defaultValue ""
    let outputPath = (Path.getFullName(parameters.OutputPath.TrimEnd('\\').TrimEnd('/')))
    let packageAnalysis = if parameters.NoPackageAnalysis then "-NoPackageAnalysis" else ""
    let defaultExcludes = if parameters.NoDefaultExcludes then "-NoDefaultExcludes" else ""
    let includeReferencedProjects = if parameters.IncludeReferencedProjects then "-IncludeReferencedProjects" else ""

    if Directory.Exists parameters.OutputPath |> not then
        failwithf "OutputDir %s does not exist." parameters.OutputPath

    let execute args =
        let result =
            ExecProcessAndReturnMessages ((fun info ->
            { info with
                FileName = parameters.ToolPath
                WorkingDirectory = Path.getFullName parameters.WorkingDir
                Arguments = args }) >> Process.withFramework) parameters.TimeOut
        if result.ExitCode <> 0 || result.Errors.Count > 0 then failwithf "Error during NuGet package creation. %s %s\r\n%s" parameters.ToolPath args (toLines result.Errors)

    match parameters.SymbolPackage with
    | NugetSymbolPackage.ProjectFile ->
        if not (isNullOrEmpty parameters.ProjectFile) then
            sprintf "pack -Symbols -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
                parameters.Version outputPath (Path.getFullName parameters.ProjectFile) packageAnalysis defaultExcludes includeReferencedProjects properties basePath
            |> execute
        sprintf "pack -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
            parameters.Version outputPath nuspecFile packageAnalysis defaultExcludes includeReferencedProjects properties basePath
        |> execute
    | NugetSymbolPackage.Nuspec ->
        sprintf "pack -Symbols -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
            parameters.Version outputPath nuspecFile packageAnalysis defaultExcludes includeReferencedProjects properties basePath
        |> execute
    | _ ->
        sprintf "pack -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
            parameters.Version outputPath nuspecFile packageAnalysis defaultExcludes includeReferencedProjects properties basePath
        |> execute

/// push package (and try again if something fails)
let rec private publish parameters =
    // Newer NuGet requires source to be always specified, so if PublishUrl is empty,
    // ignore symbol source - the produced source is broken anyway.
    let normalize str = if isNullOrEmpty str then None else Some str
    let source = match parameters.PublishUrl |> normalize, parameters.SymbolPublishUrl |> normalize with
                 | None, _                     -> ""
                 | Some source, None           -> sprintf "-source %s" source
                 | Some source, Some symSource -> sprintf "-source %s -SymbolSource %s -SymbolApiKey %s"
                                                          source symSource parameters.SymbolAccessKey

    let args = sprintf "push \"%s\" %s %s" (parameters.OutputPath @@ packageFileName parameters |> FullName)
                                           parameters.AccessKey source
    Trace.tracefn "%s %s in WorkingDir: %s Trials left: %d" parameters.ToolPath (replaceAccessKeys parameters args)
                                                            (FullName parameters.WorkingDir) parameters.PublishTrials
    try
        let result =
            let tracing = shouldEnableProcessTracing()
            try
                setEnableProcessTracing false
                ExecProcess ((fun info ->
                { info with
                    FileName = parameters.ToolPath
                    WorkingDirectory = FullName parameters.WorkingDir
                    Arguments = args }) >> Process.withFramework) parameters.TimeOut
            finally setEnableProcessTracing tracing
        if result <> 0 then failwithf "Error during NuGet push. %s %s" parameters.ToolPath args
    with exn when parameters.PublishTrials > 0 ->
        publish { parameters with PublishTrials = parameters.PublishTrials - 1 }

/// push package to symbol server (and try again if something fails)
let rec private publishSymbols parameters =

    let args =
        sprintf "push -source %s \"%s\" %s" parameters.PublishUrl (packageFileName parameters) parameters.AccessKey

    Trace.tracefn "%s %s in WorkingDir: %s Trials left: %d" parameters.ToolPath (replaceAccessKeys parameters args)
        (FullName parameters.WorkingDir) parameters.PublishTrials
    try
        let result =
            let tracing = shouldEnableProcessTracing()
            try
                setEnableProcessTracing false
                ExecProcess ((fun info ->
                    { info with
                        FileName = parameters.ToolPath
                        WorkingDirectory = FullName parameters.WorkingDir
                        Arguments = args }) >> Process.withFramework) parameters.TimeOut
            finally setEnableProcessTracing tracing
        if result <> 0 then failwithf "Error during NuGet symbol push. %s %s" parameters.ToolPath args
    with exn when parameters.PublishTrials > 0->
        publish { parameters with PublishTrials = parameters.PublishTrials - 1 }

/// Creates a new NuGet package based on the given .nuspec or project file.
/// The .nuspec / projectfile is passed as-is (no templating is performed)
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec or project file name.
let NuGetPackDirectly setParams nuspecOrProjectFile =
    use __ = Trace.traceTask "NuGetPackDirectly" nuspecOrProjectFile
    let parameters = NuGetDefaults() |> setParams
    try
         pack parameters nuspecOrProjectFile
    with exn ->
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKeys parameters
        |> failwith

/// Creates a new NuGet package based on the given .nuspec or project file.
/// Template parameter substitution is performed when passing a .nuspec
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec or project file name.
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
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKeys parameters
        |> failwith

/// Publishes a NuGet package to the nuget server.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default NuGet parameters.
let NuGetPublish setParams =
    let parameters = NuGetDefaults() |> setParams
    use __ = Trace.traceTask "NuGet-Push" (packageFileName parameters)
    try
        publish parameters
    with exn ->
        if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message else exn.Message
        |> replaceAccessKeys parameters
        |> failwith

/// Creates a new NuGet package, and optionally publishes it.
/// Template parameter substitution is performed when passing a .nuspec
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec file name.
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
            if parameters.ProjectFile <> null then publishSymbols parameters
    with exn ->
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKeys parameters
        |> failwith

/// NuSpec metadata type
type NuSpecPackage =
    { Id : string
      Version : string
      Authors : string
      Owners : string
      Url : string
      IsLatestVersion : bool
      Created : DateTime
      Published : DateTime
      PackageHash : string
      PackageHashAlgorithm : string
      LicenseUrl : string
      ProjectUrl : string
      RequireLicenseAcceptance : bool
      Description : string
      Language : string
      ReleaseNotes : string
      Tags : string }
    member x.Name = sprintf "%s %s" x.Id x.Version
    override x.ToString() = x.Name
    member x.DirectoryName = sprintf "%s.%s" x.Id x.Version
    member x.FileName = sprintf "%s.%s.nupkg" x.Id x.Version

/// Parses nuspec metadata from a nuspec file.
/// ## Parameters
///
///  - `nuspec` - The .nuspec file content.
let getNuspecProperties (nuspec : string) =
    let doc = Xml.Doc nuspec

    let namespaces =
        [ "x", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"
          "y", "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"
          "default", ""
          "inDoc", doc.DocumentElement.NamespaceURI
          ]

    let getValue name =
        let getWith ns =
            try
                doc
                |> Xml.XPathValue (sprintf "%s:metadata/%s:%s" ns ns name) namespaces
                |> Some
            with exn -> None
        namespaces
        |> Seq.map fst
        |> Seq.tryPick (fun ns -> getWith ns)
        |> (fun x ->
        if x.IsSome then x.Value
        else "")

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
      PackageHashAlgorithm = String.Empty
    }

/// Returns the NuGet meta data from the given package file name.
/// ## Parameters
///
///  - `packageFileName` - The .nuspec package file name.
// TODO: Need ICSharpCode for netcore...
//let GetMetaDataFromPackageFile packageFileName =
//    packageFileName
//    |> ZipHelper.UnzipFirstMatchingFileInMemory(fun ze -> ze.Name.EndsWith ".nuspec")
//    |> getNuspecProperties

/// Default NuGet feed
let feedUrl = "http://go.microsoft.com/fwlink/?LinkID=206669"

// TODO: Note that this is stolen from paket code. We might want to move that into a shared FAKE library..., see https://github.com/fsprojects/Paket/blob/06ef22ba79896cd9f2a2e2eefccde08b09ab7656/src/Paket.Core/Utils.fs
#if NETSTANDARD
open System.Net.Http
open System.Collections.Generic
type WebClient = HttpClient
type HttpClient with
    member x.DownloadFileTaskAsync (uri : Uri, filePath : string) =
      async {
        let! response = x.GetAsync(uri) |> Async.AwaitTask
        use fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)
        do! response.Content.CopyToAsync(fileStream) |> Async.AwaitTask
        fileStream.Flush()
      } |> Async.StartAsTask
    member x.DownloadFileTaskAsync (uri : string, filePath : string) = x.DownloadFileTaskAsync(Uri uri, filePath)
    member x.DownloadFile (uri : string, filePath : string) =
        x.DownloadFileTaskAsync(uri, filePath).GetAwaiter().GetResult()
    member x.DownloadFile (uri : Uri, filePath : string) =
        x.DownloadFileTaskAsync(uri, filePath).GetAwaiter().GetResult()
    member x.DownloadStringTaskAsync (uri : Uri) =
      async {
        let! response = x.GetAsync(uri) |> Async.AwaitTask
        let! result = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return result
      } |> Async.StartAsTask
    member x.DownloadStringTaskAsync (uri : string) = x.DownloadStringTaskAsync(Uri uri)
    member x.DownloadString (uri : string) =
        x.DownloadStringTaskAsync(uri).GetAwaiter().GetResult()
    member x.DownloadString (uri : Uri) =
        x.DownloadStringTaskAsync(uri).GetAwaiter().GetResult()

    member x.DownloadDataTaskAsync(uri : Uri) =
      async {
        let! response = x.GetAsync(uri) |> Async.AwaitTask
        let! result = response.Content.ReadAsByteArrayAsync() |> Async.AwaitTask
        return result
      } |> Async.StartAsTask
    member x.DownloadDataTaskAsync (uri : string) = x.DownloadDataTaskAsync(Uri uri)
    member x.DownloadData(uri : string) =
        x.DownloadDataTaskAsync(uri).GetAwaiter().GetResult()
    member x.DownloadData(uri : Uri) =
        x.DownloadDataTaskAsync(uri).GetAwaiter().GetResult()

    member x.UploadFileAsMultipart (url : Uri) filename =
        let fileTemplate =
            "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n"
        let boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", System.Globalization.CultureInfo.InvariantCulture)
        let fileInfo = (new FileInfo(Path.GetFullPath(filename)))
        let fileHeaderBytes =
            System.String.Format
                (System.Globalization.CultureInfo.InvariantCulture, fileTemplate, boundary, "package", "package", "application/octet-stream")
            |> Encoding.UTF8.GetBytes
        let newlineBytes = Environment.NewLine |> Encoding.UTF8.GetBytes
        let trailerbytes = String.Format(System.Globalization.CultureInfo.InvariantCulture, "--{0}--", boundary) |> Encoding.UTF8.GetBytes
        x.DefaultRequestHeaders.Add("ContentType", "multipart/form-data; boundary=" + boundary)
        use stream = new MemoryStream() // x.OpenWrite(url, "PUT")
        stream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length)
        use fileStream = File.OpenRead fileInfo.FullName
        fileStream.CopyTo(stream, (4 * 1024))
        stream.Write(newlineBytes, 0, newlineBytes.Length)
        stream.Write(trailerbytes, 0, trailerbytes.Length)
        stream.Write(newlineBytes, 0, newlineBytes.Length)
        stream.Position <- 0L
        x.PutAsync(url, new StreamContent(stream)).GetAwaiter().GetResult()

    member x.DownloadStringAndHeaders (url :string) =
      async {
        let! response = x.GetAsync(url) |> Async.AwaitTask
        let! result = response.Content.ReadAsStringAsync() |> Async.AwaitTask

        return
            result,
            response.Headers :> seq<KeyValuePair<string, seq<string>>>
            |> Seq.map (fun kv -> kv.Key, kv.Value |> Seq.toList)
            |> Map.ofSeq
      } |> Async.RunSynchronously
let internal addAcceptHeader (client:HttpClient) (contentType:string) =
    for headerVal in contentType.Split([|','|], System.StringSplitOptions.RemoveEmptyEntries) do
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(headerVal))
let internal addHeader (client:HttpClient) (headerKey:string) (headerVal:string) =
    client.DefaultRequestHeaders.Add(headerKey, headerVal)
#else

open System.Net
type WebClient with
    member x.UploadFileAsMultipart (url : Uri) filename =
        let fileTemplate =
            "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n"
        let boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", System.Globalization.CultureInfo.InvariantCulture)
        let fileInfo = (new FileInfo(Path.GetFullPath(filename)))
        let fileHeaderBytes =
            System.String.Format
                (System.Globalization.CultureInfo.InvariantCulture, fileTemplate, boundary, "package", "package", "application/octet-stream")
            |> Encoding.UTF8.GetBytes
        // we use a windows-style newline rather than Environment.NewLine for compatibility
        let newlineBytes = "\r\n" |> Encoding.UTF8.GetBytes
        let trailerbytes = String.Format(System.Globalization.CultureInfo.InvariantCulture, "--{0}--", boundary) |> Encoding.UTF8.GetBytes
        x.Headers.Add(HttpRequestHeader.ContentType, "multipart/form-data; boundary=" + boundary)
        use stream = x.OpenWrite(url, "PUT")
        stream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length)
        use fileStream = File.OpenRead fileInfo.FullName
        fileStream.CopyTo(stream, (4 * 1024))
        stream.Write(newlineBytes, 0, newlineBytes.Length)
        stream.Write(trailerbytes, 0, trailerbytes.Length)
        stream.Write(newlineBytes, 0, newlineBytes.Length)
        ()
    member x.DownloadStringAndHeaders (url :string) =
        let result = x.DownloadString(url)
        result,
        x.ResponseHeaders.AllKeys
        |> Seq.map (fun kv -> kv, [ x.ResponseHeaders.Get kv ])
        |> Map.ofSeq

type WebClient = System.Net.WebClient

let internal addAcceptHeader (client:WebClient) contentType =
    client.Headers.Add (HttpRequestHeader.Accept, contentType)
let internal addHeader (client:WebClient) (headerKey:string) (headerVal:string) =
    client.Headers.Add (headerKey, headerVal)

#endif

let private webClient = new WebClient()

/// [omit]
let discoverRepoUrl =
    lazy (let resp = webClient.DownloadString(feedUrl)
          let doc = Xml.Doc resp
          doc.["service"].GetAttribute("xml:base"))

/// [omit]
let getRepoUrl() = discoverRepoUrl.Force()

/// [omit]
let extractFeedPackageFromXml (entry : Xml.XmlNode) =
    let properties = entry.["m:properties"]
    let property name =
        let p = properties.["d:" + name]
        if p = null || p.IsEmpty then "" else p.InnerText
    let boolProperty name = (property name).ToLower() = "true"
    let author = entry.["author"].InnerText
    let dateTimeProperty name = DateTime.Parse(property name)
    { Id = entry.["title"].InnerText
      Version = property "Version"
      Description = property "Description"
      IsLatestVersion = boolProperty "IsLatestVersion"
      Authors = author
      Owners = author
      Language = property "Language"
      Tags = property "Tags"
      ReleaseNotes = property "ReleaseNotes"
      ProjectUrl = property "ProjectUrl"
      LicenseUrl = property "LicenseUrl"
      RequireLicenseAcceptance = boolProperty "RequireLicenseAcceptance"
      PackageHash = property "PackageHash"
      PackageHashAlgorithm = property "PackageHashAlgorithm"
      Created = dateTimeProperty "Created"
      Published = dateTimeProperty "Published"
      Url = entry.["content"].GetAttribute("src") }

/// [omit]
let getPackage (repoUrl:string) packageName version =
    let url : string = repoUrl.TrimEnd('/') + "/Packages(Id='" + packageName + "',Version='" + version + "')"
    let resp = webClient.DownloadString(url)
    let doc = Xml.Doc resp
    extractFeedPackageFromXml doc.["entry"]

/// [omit]
let getFeedPackagesFromUrl (url : string) =
    let resp = webClient.DownloadString(url)
    let doc = Xml.Doc resp
    [ for entry in doc.["feed"].GetElementsByTagName("entry") -> extractFeedPackageFromXml entry ]

/// [omit]
let getLatestPackage (repoUrl:string) packageName =
    repoUrl.TrimEnd('/') + "/Packages()?$filter=(Id%20eq%20'" + packageName + "')%20and%20IsLatestVersion"
    |> getFeedPackagesFromUrl
    |> Seq.head

/// [omit]
let downloadPackage targetDir (package : NuSpecPackage) =
    Directory.ensure targetDir
    let targetFileName = targetDir @@ package.FileName
    Trace.tracefn "Downloading package %s %s from %s and saving it to %s" package.Id package.Version package.Url 
        targetFileName
    webClient.DownloadFile(package.Url, targetFileName)
    targetFileName

/// [omit]
let argList name values =
    values
    |> Seq.collect (fun v -> ["-" + name; sprintf @"""%s""" v])
    |> String.concat " "

type NuGetDependency =
    {  Id : string; Version : SemVerInfo; IsDevelopmentDependency : bool }

/// Returns the dependencies from specified packages.config file
let getDependencies (packagesFile:string) =
    let xname = XName.op_Implicit
    let attribute name (e:XElement) =
        match e.Attribute (xname name) with
        | null -> ""
        | a -> a.Value

    let isDevDependency package = 
       let value = attribute "developmentDependency" package
       String.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase)

    let doc =
        XDocument.Load packagesFile
    [for package in doc.Descendants (xname"package") ->
        {  Id = attribute "id" package
           Version = SemVer.parse (attribute "version" package)
           IsDevelopmentDependency = isDevDependency package } ]
