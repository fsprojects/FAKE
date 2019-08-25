[<AutoOpen>]
/// Contains helper functions and task which allow to inspect, create and publish [NuGet](https://www.nuget.org/) packages.
/// There is also a tutorial about [nuget package creating](/create-nuget-package.html) available.
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
module Fake.NuGetHelper

#nowarn "44"
open System
open System.IO
open System.Xml.Linq

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
type NugetDependencies = (string * string) list

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
type NugetFrameworkDependencies = 
    { FrameworkVersion : string
      Dependencies : NugetDependencies }
      
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
type NugetReferences = string list

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
type NugetFrameworkReferences = 
    { FrameworkVersion : string
      References : NugetReferences }
      
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
type NugetFrameworkAssemblyReferences =
    { FrameworkVersions : string list
      AssemblyName : string }
      
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
type NugetSymbolPackage =
    /// Do not build symbol packages
    | None = 0
    /// Build a symbol package using a project file, if provided
    | ProjectFile = 1
    /// Build a symbol package using the nuspec file
    | Nuspec = 2
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Nuget parameter type
[<CLIMutable>]
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
      
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// NuGet default parameters  
let NuGetDefaults() = 
    { ToolPath = findNuget (currentDirectory @@ "tools" @@ "NuGet")
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
      
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Creates a string which tells NuGet that you require exactly this package version.
let RequireExactly version = sprintf "[%s]" version

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
let private packageFileName parameters = sprintf "%s.%s.nupkg" parameters.Project parameters.Version


[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
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
               
        logfn "Version %s found for package %s" version package
        version
    with
    | exn -> new Exception("Could not detect package version for " + package, exn) |> raise

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
let private replaceAccessKeys parameters (text:string) =
    let replaceKey key (str:string) =
        if isNullOrEmpty key then str
        else str.Replace(key, "PRIVATEKEY")

    text |> (replaceKey parameters.AccessKey >> replaceKey parameters.SymbolAccessKey)

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
let private createNuSpecFromTemplate parameters (templateNuSpec:FileInfo) =
    let specFile = parameters.WorkingDir @@ (templateNuSpec.Name.Replace("nuspec", "") + parameters.Version + ".nuspec")
                    |> FullName
    tracefn "Creating .nuspec file at %s" specFile

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
        else XText(notEncodedText).ToString().Replace("ß","&szlig;")

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
    
    processTemplates replacements [ specFile ]
    tracefn "Created nuspec file %s" specFile
    specFile
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
let private createNuSpecFromTemplateIfNotProjFile parameters nuSpecOrProjFile = 
    let nuSpecOrProjFileInfo = fileInfo nuSpecOrProjFile
    match nuSpecOrProjFileInfo.Extension.ToLower().EndsWith("proj") with
    | true -> None
    | false -> Some (createNuSpecFromTemplate parameters nuSpecOrProjFileInfo)
    
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
let private propertiesParam = function 
    | [] -> ""
    | lst -> 
        "-Properties " + (lst
                          |> List.map (fun p -> (fst p) + "=\"" + (snd p) + "\"")
                          |> String.concat ";")

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Creates a NuGet package without templating (including symbols package if enabled)
let private pack parameters nuspecFile =
    let nuspecFile = FullName nuspecFile
    let properties = propertiesParam parameters.Properties
    let basePath = parameters.BasePath |> Option.map (sprintf "-BasePath \"%s\"") |> Option.defaultValue ""
    let outputPath = (FullName(parameters.OutputPath.TrimEnd('\\').TrimEnd('/')))
    let packageAnalysis = if parameters.NoPackageAnalysis then "-NoPackageAnalysis" else ""
    let defaultExcludes = if parameters.NoDefaultExcludes then "-NoDefaultExcludes" else ""
    let includeReferencedProjects = if parameters.IncludeReferencedProjects then "-IncludeReferencedProjects" else ""
    
    if Directory.Exists parameters.OutputPath |> not then 
        failwithf "OutputDir %s does not exist." parameters.OutputPath

    let execute args =
        let result =
            ExecProcessAndReturnMessages (fun info ->
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- FullName parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
        if result.ExitCode <> 0 || result.Errors.Count > 0 then failwithf "Error during NuGet package creation. %s %s\r\n%s" parameters.ToolPath args (toLines result.Errors)

    match parameters.SymbolPackage with
    | NugetSymbolPackage.ProjectFile ->
        if not (isNullOrEmpty parameters.ProjectFile) then
            sprintf "pack -Symbols -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s %s"
                parameters.Version outputPath (FullName parameters.ProjectFile) packageAnalysis defaultExcludes includeReferencedProjects properties basePath
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
    
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// push package (and try again if something fails)
let rec private publish parameters = 
    let tracing = enableProcessTracing
    enableProcessTracing <- false

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
    tracefn "%s %s in WorkingDir: %s Trials left: %d" parameters.ToolPath (replaceAccessKeys parameters args)
                                                      (FullName parameters.WorkingDir) parameters.PublishTrials
    try 
        let result = 
            ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- FullName parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
        enableProcessTracing <- tracing
        if result <> 0 then failwithf "Error during NuGet push. %s %s" parameters.ToolPath args
    with exn -> 
        if parameters.PublishTrials > 0 then publish { parameters with PublishTrials = parameters.PublishTrials - 1 }
        else raise exn
        
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// push package to symbol server (and try again if something fails)
let rec private publishSymbols parameters = 
    let tracing = enableProcessTracing
    enableProcessTracing <- false
    let args = 
        sprintf "push -source %s \"%s\" %s" parameters.PublishUrl (packageFileName parameters) parameters.AccessKey

    tracefn "%s %s in WorkingDir: %s Trials left: %d" parameters.ToolPath (replaceAccessKeys parameters args)
        (FullName parameters.WorkingDir) parameters.PublishTrials
    try 
        let result = 
            ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- FullName parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
        enableProcessTracing <- tracing
        if result <> 0 then failwithf "Error during NuGet symbol push. %s %s" parameters.ToolPath args
    with exn -> 
        if parameters.PublishTrials > 0 then publish { parameters with PublishTrials = parameters.PublishTrials - 1 }
        else raise exn
        
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Creates a new NuGet package based on the given .nuspec or project file.
/// The .nuspec / projectfile is passed as-is (no templating is performed)
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec or project file name.
let NuGetPackDirectly setParams nuspecOrProjectFile =
    use __ = traceStartTaskUsing "NuGetPackDirectly" nuspecOrProjectFile
    let parameters = NuGetDefaults() |> setParams
    try
         pack parameters nuspecOrProjectFile
    with exn ->
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKeys parameters
        |> failwith

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Creates a new NuGet package based on the given .nuspec or project file.
/// Template parameter substitution is performed when passing a .nuspec
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec or project file name.
let NuGetPack setParams nuspecOrProjectFile =
    use __ = traceStartTaskUsing "NuGetPack" nuspecOrProjectFile
    let parameters = NuGetDefaults() |> setParams
    try
        match (createNuSpecFromTemplateIfNotProjFile parameters nuspecOrProjectFile) with
        | Some nuspecTemplateFile -> 
            pack parameters nuspecTemplateFile
            DeleteFile nuspecTemplateFile
        | None -> pack parameters nuspecOrProjectFile
    with exn ->
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKeys parameters
        |> failwith

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Publishes a NuGet package to the nuget server.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
let NuGetPublish setParams = 
    let parameters = NuGetDefaults() |> setParams
    use __ = traceStartTaskUsing "NuGet-Push" (packageFileName parameters)
    try
        publish parameters
    with exn ->
        if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message else exn.Message
        |> replaceAccessKeys parameters
        |> failwith
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Creates a new NuGet package, and optionally publishes it.
/// Template parameter substitution is performed when passing a .nuspec
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec file name.
let NuGet setParams nuspecOrProjectFile = 
    use __ = traceStartTaskUsing "NuGet" nuspecOrProjectFile
    let parameters = NuGetDefaults() |> setParams
    try 
        match (createNuSpecFromTemplateIfNotProjFile parameters nuspecOrProjectFile) with
        | Some nuspecTemplateFile -> 
            pack parameters nuspecTemplateFile
            DeleteFile nuspecTemplateFile
        | None -> pack parameters nuspecOrProjectFile

        if parameters.Publish then 
            publish parameters
            if parameters.ProjectFile <> null then publishSymbols parameters
    with exn -> 
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKeys parameters
        |> failwith

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
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
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Parses nuspec metadata from a nuspec file.
/// ## Parameters
/// 
///  - `nuspec` - The .nuspec file content.
let getNuspecProperties (nuspec : string) = 
    let doc = XMLDoc nuspec
    
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
                |> XPathValue (sprintf "%s:metadata/%s:%s" ns ns name) namespaces
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
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Returns the NuGet meta data from the given package file name.
/// ## Parameters
/// 
///  - `packageFileName` - The .nuspec package file name.
let GetMetaDataFromPackageFile packageFileName = 
    packageFileName
    |> ZipHelper.UnzipFirstMatchingFileInMemory(fun ze -> ze.Name.EndsWith ".nuspec")
    |> getNuspecProperties
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Default NuGet feed
let feedUrl = "http://go.microsoft.com/fwlink/?LinkID=206669"

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
let private webClient = new System.Net.WebClient()

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// [omit]
let discoverRepoUrl = 
    lazy (let resp = webClient.DownloadString(feedUrl)
          let doc = XMLDoc resp
          doc.["service"].GetAttribute("xml:base"))

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// [omit]
let getRepoUrl() = discoverRepoUrl.Force()

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
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

[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// [omit]
let getPackage (repoUrl:string) packageName version = 
    let url : string = repoUrl.TrimEnd('/') + "/Packages(Id='" + packageName + "',Version='" + version + "')"
    let resp = webClient.DownloadString(url)
    let doc = XMLDoc resp
    extractFeedPackageFromXml doc.["entry"]
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// [omit]
let getFeedPackagesFromUrl (url : string) = 
    let resp = webClient.DownloadString(url)
    let doc = XMLDoc resp
    [ for entry in doc.["feed"].GetElementsByTagName("entry") -> extractFeedPackageFromXml entry ]
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// [omit]
let getLatestPackage (repoUrl:string) packageName = 
    repoUrl.TrimEnd('/') + "/Packages()?$filter=(Id%20eq%20'" + packageName + "')%20and%20IsLatestVersion"
    |> getFeedPackagesFromUrl
    |> Seq.head
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// [omit]
let downloadPackage targetDir (package : NuSpecPackage) = 
    ensureDirectory targetDir
    let targetFileName = targetDir @@ package.FileName
    tracefn "Downloading package %s %s from %s and saving it to %s" package.Id package.Version package.Url 
        targetFileName
    webClient.DownloadFile(package.Url, targetFileName)
    targetFileName
    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// [omit]
let argList name values =
    values
    |> Seq.collect (fun v -> ["-" + name; sprintf @"""%s""" v])
    |> String.concat " "

    
[<System.Obsolete("Use Fake.DotNet.NuGet.NuGet instead")>]
/// Returns the dependencies from specified packages.config file
let getDependencies (packagesFile:string) =
    let xname = XName.op_Implicit
    let attribute name (e:XElement) =
        match e.Attribute (xname name) with
        | null -> ""
        | a -> a.Value

    let doc = 
        XDocument.Load packagesFile
    [for package in doc.Descendants (xname"package") ->
        attribute "id" package, attribute "version" package ]
