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
      NoPackageAnalysis : bool
      ProjectFile : string
      Dependencies : NugetDependencies
      DependenciesByFramework : NugetFrameworkDependencies list
      References : NugetReferences
      ReferencesByFramework : NugetFrameworkReferences list
      PublishTrials : int
      Publish : bool
      Properties : list<string * string> }

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
      OutputPath = "./NuGet"
      WorkingDir = "./NuGet"
      PublishUrl = null
      AccessKey = null
      NoPackageAnalysis = false
      PublishTrials = 5
      Publish = false
      Properties = [] }

/// Creates a string which tells NuGet that you require exactly this package version.
let RequireExactly version = sprintf "[%s]" version

let private packageFileName parameters = sprintf "%s.%s.nupkg" parameters.Project parameters.Version
let private symbolsPackageFileName parameters = sprintf "%s.%s.symbols.nupkg" parameters.Project parameters.Version

/// Gets the version no. for a given package in the deployments folder
let GetPackageVersion deploymentsDir package = 
    let version = 
        let files = Directory.GetDirectories(deploymentsDir, sprintf "%s.*" package)
        if Seq.isEmpty files then failwithf "Package %s was not found." package
        Seq.head files |> fun full -> full.Substring(full.LastIndexOf package + package.Length + 1)
    logfn "Version %s found for package %s" version package
    version

let private replaceAccessKey key (text : string) = 
    if isNullOrEmpty key then text
    else text.Replace(key, "PRIVATEKEY")

let private createNuspecFile parameters nuSpec = 
    let fi = fileInfo nuSpec
    let specFile = parameters.WorkingDir @@ (fi.Name.Replace("nuspec", "") + parameters.Version + ".nuspec")
                   |> FullName
    tracefn "Creating .nuspec file at %s" specFile
    fi.CopyTo(specFile, true) |> ignore
    let getFrameworkGroup (frameworkTags : (string * string) seq) = 
        frameworkTags
        |> Seq.map 
               (fun (frameworkVersion, tags) -> sprintf "<group targetFramework=\"%s\">%s</group>" frameworkVersion tags)
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
    
    let xmlEncode (notEncodedText : string) = 
        if isNullOrEmpty notEncodedText then ""
        else XText(notEncodedText).ToString()
    
    let replacements = 
        [ "@build.number@", parameters.Version
          "@title@", parameters.Title
          "@authors@", parameters.Authors |> separated ", "
          "@project@", parameters.Project
          "@summary@", parameters.Summary
          "@description@", parameters.Description
          "@tags@", parameters.Tags
          "@releaseNotes@", parameters.ReleaseNotes
          "@copyright@", parameters.Copyright ]
        |> List.map (fun (placeholder, replacement) -> placeholder, xmlEncode replacement)
        |> List.append [ "@dependencies@", dependenciesXml
                         "@references@", referencesXml ]
    
    processTemplates replacements [ specFile ]
    tracefn "Created nuspec file %s" specFile
    specFile

/// create symbols package
let private packSymbols parameters = 
    if isNullOrEmpty parameters.ProjectFile then ()
    else 
        let args = 
            sprintf "pack -sym -Version %s -OutputDirectory \"%s\" \"%s\"" parameters.Version 
                (FullName parameters.OutputPath) (FullName parameters.ProjectFile)
        
        let result = 
            ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- FullName parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
        if result <> 0 then failwithf "Error during NuGet symbols creation. %s %s" parameters.ToolPath args
        parameters.OutputPath @@ (symbolsPackageFileName parameters)
        |> DeleteFile

/// create package
let private pack parameters nuspecFile = 
    let properties = 
        match parameters.Properties with
        | [] -> ""
        | lst -> 
            "-Properties " + (lst
                              |> List.map (fun p -> (fst p) + "=\"" + (snd p) + "\"")
                              |> List.fold (fun state p -> p + ";" + state) ""
                              |> (fun s -> s.TrimEnd(';')))
    
    let args = 
        sprintf "pack \"%s\" -Version %s -OutputDirectory \"%s\" %s %s" (FullName nuspecFile) parameters.Version 
            (FullName(parameters.OutputPath.TrimEnd('\\').TrimEnd('/'))) (if parameters.NoPackageAnalysis then 
                                                                              "-NoPackageAnalysis"
                                                                          else "") properties
    
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- FullName parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
    
    if result <> 0 then failwithf "Error during NuGet package creation. %s %s" parameters.ToolPath args

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
///  - `nuspecFile` - The .nuspec file name.
let NuGetPack setParams nuspecFile = 
    traceStartTask "NuGet-Pack" nuspecFile
    let parameters = NuGetDefaults() |> setParams
    pack parameters nuspecFile
    traceEndTask "NuGet-Pack" nuspecFile

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
let NuGet setParams nuspecFile = 
    traceStartTask "NuGet" nuspecFile
    let parameters = NuGetDefaults() |> setParams
    try 
        let nuspecFile = createNuspecFile parameters nuspecFile
        packSymbols parameters
        pack parameters nuspecFile
        if parameters.Publish then 
            publish parameters
            if parameters.ProjectFile <> null then publishSymbols parameters
        DeleteFile nuspecFile
    with exn -> 
        (if exn.InnerException <> null then exn.Message + "\r\n" + exn.InnerException.Message
         else exn.Message)
        |> replaceAccessKey parameters.AccessKey
        |> failwith
    traceEndTask "NuGet" nuspecFile

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
          "y", "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd" ]
    
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
      PackageHashAlgorithm = String.Empty }

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