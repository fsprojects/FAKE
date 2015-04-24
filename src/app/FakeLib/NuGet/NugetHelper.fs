[<AutoOpen>]
/// Contains helper functions and task which allow to inspect, create and publish [NuGet](https://www.nuget.org/) packages.
/// There is also a tutorial about [nuget package creating](../create-nuget-package.html) available.
module Fake.NuGetHelper

open System
open System.IO
open System.Xml.Linq

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
      OutputPath : string
      PublishUrl : string
      AccessKey : string
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
      Files : list<string*string option*string option>}

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
      OutputPath = "./NuGet"
      WorkingDir = "./NuGet"
      PublishUrl = null
      AccessKey = null
      NoDefaultExcludes = false
      NoPackageAnalysis = false
      PublishTrials = 5
      Publish = false
      SymbolPackage = NugetSymbolPackage.ProjectFile
      Properties = []
      Files = [] }

/// Creates a string which tells NuGet that you require exactly this package version.
let RequireExactly version = sprintf "[%s]" version

let private packageFileName parameters = sprintf "%s.%s.nupkg" parameters.Project parameters.Version


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
                let files = Directory.GetFiles(folder, sprintf "%s.*.nupkg" package)
                let file = (Seq.head files).Replace(".nupkg","")
                let index = file.LastIndexOf package + package.Length + 1
                file.Substring index
               
        logfn "Version %s found for package %s" version package
        version
    with
    | exn -> new Exception("Could not detect package version for " + package, exn) |> raise

let private replaceAccessKey key (text : string) = 
    if isNullOrEmpty key then text
    else text.Replace(key, "PRIVATEKEY")

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
        ]
        |> List.map (fun (placeholder, replacement) -> placeholder, xmlEncode replacement)
        |> List.append [ "@dependencies@", dependenciesXml
                         "@references@", referencesXml
                         "@frameworkAssemblies@", frameworkAssembliesXml
                         "@files@", filesXml ]
    
    processTemplates replacements [ specFile ]
    tracefn "Created nuspec file %s" specFile
    specFile

let private createNuSpecFromTemplateIfNotCsprojFile parameters nuSpecOrProjFile = 
    let nuSpecOrProjFileInfo = fileInfo nuSpecOrProjFile
    match nuSpecOrProjFileInfo.Extension.ToLower() = ".csproj" with
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
    let properties = propertiesParam parameters.Properties
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
        if result.ExitCode <> 0 then failwithf "Error during NuGet package creation. %s %s\r\n%s" parameters.ToolPath args (toLines result.Errors)

    let nuspecFile = 
        let fi = fileInfo nuspecFile
        if fi.Directory.FullName = FullName parameters.WorkingDir then
            fi.Name
        else
            FullName nuspecFile

    match parameters.SymbolPackage with
    | NugetSymbolPackage.ProjectFile ->
        if not (isNullOrEmpty parameters.ProjectFile) then
            sprintf "pack -Symbols -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s"
                parameters.Version outputPath (FullName parameters.ProjectFile) packageAnalysis defaultExcludes includeReferencedProjects properties
            |> execute
        sprintf "pack -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s"
            parameters.Version outputPath nuspecFile packageAnalysis defaultExcludes includeReferencedProjects properties
        |> execute
    | NugetSymbolPackage.Nuspec ->
        sprintf "pack -Symbols -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s"
            parameters.Version outputPath nuspecFile packageAnalysis defaultExcludes includeReferencedProjects properties
        |> execute
    | _ ->
        sprintf "pack -Version %s -OutputDirectory \"%s\" \"%s\" %s %s %s %s"
            parameters.Version outputPath nuspecFile packageAnalysis defaultExcludes includeReferencedProjects properties
        |> execute
    

/// push package (and try again if something fails)
let rec private publish parameters = 
    let tracing = enableProcessTracing
    enableProcessTracing <- false
    let source = 
        if isNullOrEmpty parameters.PublishUrl then ""
        else sprintf "-s %s" parameters.PublishUrl
    
    let args = sprintf "push \"%s\" %s %s" (parameters.OutputPath @@ packageFileName parameters
                                            |> FullName) parameters.AccessKey source
    tracefn "%s %s in WorkingDir: %s Trials left: %d" parameters.ToolPath (replaceAccessKey parameters.AccessKey args) 
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

/// push package to symbol server (and try again if something fails)
let rec private publishSymbols parameters = 
    let tracing = enableProcessTracing
    enableProcessTracing <- false
    let args = 
        sprintf "push -source %s \"%s\" %s" parameters.PublishUrl (packageFileName parameters) parameters.AccessKey

    tracefn "%s %s in WorkingDir: %s Trials left: %d" parameters.ToolPath (replaceAccessKey parameters.AccessKey args) 
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

/// Creates a new NuGet package based on the given .nuspec file.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec or project file name.
let NuGetPackDirectly setParams nuspecOrProjectFile =
    traceStartTask "NuGetPackDirectly" nuspecOrProjectFile
    let parameters = NuGetDefaults() |> setParams
    try
         pack parameters nuspecOrProjectFile
    with exn ->
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKey parameters.AccessKey
        |> failwith
    traceEndTask "NuGetPackDirectly" nuspecOrProjectFile

/// Creates a new NuGet package based on the given .nuspec file.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecOrProjectFile` - The .nuspec or project file name.
let NuGetPack setParams nuspecOrProjectFile =
    traceStartTask "NuGetPack" nuspecOrProjectFile
    let parameters = NuGetDefaults() |> setParams
    try
        match (createNuSpecFromTemplateIfNotCsprojFile parameters nuspecOrProjectFile) with
        | Some nuspecTemplateFile -> 
            pack parameters nuspecTemplateFile
            DeleteFile nuspecTemplateFile
        | None -> pack parameters nuspecOrProjectFile
    with exn ->
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKey parameters.AccessKey
        |> failwith
    traceEndTask "NuGetPack" nuspecOrProjectFile

/// Publishes a NuGet package to the nuget server.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
let NuGetPublish setParams = 
    let parameters = NuGetDefaults() |> setParams
    traceStartTask "NuGet-Push" (packageFileName parameters)
    publish parameters
    traceEndTask "NuGet-Push" (packageFileName parameters)

/// Creates a new NuGet package.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `nuspecFile` - The .nuspec file name.
let NuGet setParams nuspecOrProjectFile = 
    traceStartTask "NuGet" nuspecOrProjectFile
    let parameters = NuGetDefaults() |> setParams
    try 
        match (createNuSpecFromTemplateIfNotCsprojFile parameters nuspecOrProjectFile) with
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
        |> replaceAccessKey parameters.AccessKey
        |> failwith
    traceEndTask "NuGet" nuspecOrProjectFile

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
let GetMetaDataFromPackageFile packageFileName = 
    packageFileName
    |> ZipHelper.UnzipFirstMatchingFileInMemory(fun ze -> ze.Name.EndsWith ".nuspec")
    |> getNuspecProperties

/// Default NuGet feed
let feedUrl = "http://go.microsoft.com/fwlink/?LinkID=206669"

let private webClient = new System.Net.WebClient()

/// [omit]
let discoverRepoUrl = 
    lazy (let resp = webClient.DownloadString(feedUrl)
          let doc = XMLDoc resp
          doc.["service"].GetAttribute("xml:base"))

/// [omit]
let getRepoUrl() = discoverRepoUrl.Force()

/// [omit]
let extractFeedPackageFromXml (entry : Xml.XmlNode) = 
    let properties = entry.["m:properties"]
    let property name = properties.["d:" + name].InnerText
    let boolProperty name = (property name).ToLower() = "true"
    let dateTimeProperty name = DateTime.Parse(property name)
    { Id = property "Id"
      Version = property "Version"
      Description = property "Description"
      IsLatestVersion = boolProperty "IsLatestVersion"
      Authors = property "Authors"
      Owners = property "Authors"
      Language = property "Language"
      Tags = property "Tags"
      ProjectUrl = property "ProjectUrl"
      LicenseUrl = property "LicenseUrl"
      RequireLicenseAcceptance = boolProperty "RequireLicenseAcceptance"
      PackageHash = property "PackageHash"
      PackageHashAlgorithm = property "PackageHashAlgorithm"
      Created = dateTimeProperty "Created"
      Published = dateTimeProperty "Published"
      Url = entry.["content"].GetAttribute("src") }

/// [omit]
let getPackage repoUrl packageName version = 
    let url : string = repoUrl + "Packages(Id='" + packageName + "',Version='" + version + "')"
    let resp = webClient.DownloadString(url)
    let doc = XMLDoc resp
    extractFeedPackageFromXml doc.["entry"]

/// [omit]
let getFeedPackagesFromUrl (url : string) = 
    let resp = webClient.DownloadString(url)
    let doc = XMLDoc resp
    [ for entry in doc.["feed"].GetElementsByTagName("entry") -> extractFeedPackageFromXml entry ]

/// [omit]
let getLatestPackage repoUrl packageName = 
    repoUrl + "Packages()?$filter=(Id%20eq%20'" + packageName + "')%20and%20IsLatestVersion"
    |> getFeedPackagesFromUrl
    |> Seq.head

/// [omit]
let downloadPackage targetDir (package : NuSpecPackage) = 
    ensureDirectory targetDir
    let targetFileName = targetDir @@ package.FileName
    tracefn "Downloading package %s %s from %s and saving it to %s" package.Id package.Version package.Url 
        targetFileName
    webClient.DownloadFile(package.Url, targetFileName)
    targetFileName

/// [omit]
let argList name values =
    values
    |> Seq.collect (fun v -> ["-" + name; sprintf @"""%s""" v])
    |> String.concat " "


/// loads the dependences from specified packages.config file
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
