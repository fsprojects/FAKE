[<AutoOpen>]
module Fake.NuGetHelper

open System
open System.IO

type NuGetParams =
    { ToolPath: string;
      TimeOut: TimeSpan;
      Version: string;
      Authors: string list;
      Project: string;
      Summary: string;
      Description: string;                               
      WorkingDir: string;
      OutputPath: string;
      PublishUrl: string;
      AccessKey:string;
      NoPackageAnalysis: bool;
      ProjectFile:string;
      Dependencies: (string*string) list;
      PublishTrials: int;
      Publish:bool }

/// NuGet default params  
let NuGetDefaults() =
    { ToolPath = "./tools/NuGet/NuGet.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      Version = if not isLocalBuild then buildVersion else "0.1.0.0"
      Authors = []
      Project = ""
      Summary = null
      ProjectFile = null
      Description = null
      Dependencies = []
      OutputPath = "./NuGet"
      WorkingDir = "./NuGet"
      PublishUrl = null
      AccessKey = null
      NoPackageAnalysis = false
      PublishTrials = 5
      Publish = false}

let RequireExactly version = sprintf "[%s]" version

let private packageFileName parameters = sprintf "%s.%s.nupkg" parameters.Project parameters.Version

let private symbolsPackageFileName parameters = sprintf "%s.%s.symbols.nupkg" parameters.Project parameters.Version

/// Gets the version no. for a given package in the deployments folder
let GetPackageVersion deploymentsDir package = 
    let version = 
        Directory.GetDirectories(deploymentsDir, sprintf "%s.*" package) 
        |> Seq.head
        |> fun full -> full.Substring (full.LastIndexOf package + package.Length + 1)

    logfn "Version %s found for package %s" version package
    version

let private replaceAccessKey key (s:string) = s.Replace(key,"PRIVATEKEY")

let private createNuspecFile parameters nuSpec =
    // create .nuspec file
    CopyFile parameters.OutputPath nuSpec
    let specFile = parameters.OutputPath @@ (Path.GetFileName nuSpec) |> FullName

    let dependencies =
        if parameters.Dependencies = [] then "" else
        parameters.Dependencies
          |> Seq.map (fun (package,version) -> sprintf "<dependency id=\"%s\" version=\"%s\" />" package version)
          |> separated "\r\n"
          |> fun s -> sprintf "<dependencies>\r\n%s\r\n</dependencies>" s

    let replacements =
        ["@build.number@",parameters.Version
         "@authors@",parameters.Authors |> separated ", "
         "@project@",parameters.Project
         "@summary@",if isNullOrEmpty parameters.Summary then "" else parameters.Summary
         "@dependencies@",dependencies
         "@description@",parameters.Description]

    processTemplates replacements [specFile]
    tracefn "Created nuspec file %s" specFile

    specFile

// create symbols package
let private packSymbols parameters =
    if isNullOrEmpty parameters.ProjectFile then () else
    let args = 
        sprintf "pack -sym -Version %s -OutputDirectory \"%s\" \"%s\""
            parameters.Version
            (FullName parameters.OutputPath)
            (FullName parameters.ProjectFile)

    let result = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- FullName parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
               
    if result <> 0 then failwithf "Error during NuGet symbols creation. %s %s" parameters.ToolPath args
    parameters.OutputPath @@ (symbolsPackageFileName parameters) |> DeleteFile

// create package
let private pack parameters nuspecFile =    
    let args = 
        sprintf "pack \"%s\" -Version %s -OutputDirectory \"%s\" %s" 
            (FullName nuspecFile)
            parameters.Version
            (FullName parameters.OutputPath)
            (if parameters.NoPackageAnalysis then "-NoPackageAnalysis" else "")

    let result = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- FullName parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
               
    if result <> 0 then failwithf "Error during NuGet creation. %s %s" parameters.ToolPath args

// push package (and try again if something fails)
let rec private publish parameters =
    let tracing = enableProcessTracing
    enableProcessTracing <- false
    let source = if isNullOrEmpty parameters.PublishUrl then "" else sprintf "-s %s" parameters.PublishUrl
    let args = 
        sprintf "push \"%s\" %s %s" 
            (packageFileName parameters |> FullName) 
            parameters.AccessKey source

    if tracing then         
        tracefn "%s %s in WorkingDir: %s" parameters.ToolPath (replaceAccessKey parameters.AccessKey args) (FullName parameters.WorkingDir)

    let result =
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- FullName parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
        
    enableProcessTracing <- tracing
    if result <> 0 then 
        if parameters.PublishTrials > 0 then 
            publish { parameters with PublishTrials = parameters.PublishTrials - 1 } 
        else
            failwithf "Error during NuGet push. %s %s" parameters.ToolPath args

// push package to symbol server (and try again if something fails)
let rec private publishSymbols parameters =
    let tracing = enableProcessTracing
    enableProcessTracing <- false
    let args = sprintf "push -source %s \"%s\" %s" parameters.PublishUrl (packageFileName parameters) parameters.AccessKey

    if tracing then 
        args
            |> replaceAccessKey parameters.AccessKey
            |> tracefn "%s %s" parameters.ToolPath 

    let result = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- FullName parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
        
    enableProcessTracing <- tracing
    if result <> 0 then 
        if parameters.PublishTrials > 0 then 
            publishSymbols { parameters with PublishTrials = parameters.PublishTrials - 1 }
        else
            failwithf "Error during NuGet symbol push. %s %s" parameters.ToolPath args     

/// Creates a new NuGet package based on the nuspecFile
let NuGetPack setParams nuspecFile =
    traceStartTask "NuGet-Pack" nuspecFile
    let parameters = NuGetDefaults() |> setParams
    pack parameters nuspecFile

    traceEndTask "NuGet-Pack" nuspecFile

/// Publishes a NuGet package to the nuget server
let NuGetPublish setParams  =
    let parameters = NuGetDefaults() |> setParams
    traceStartTask "NuGet-Push" (packageFileName parameters)
    publish parameters

    traceEndTask "NuGet-Push" (packageFileName parameters)


/// Creates a new NuGet package   
let NuGet setParams nuSpec =
    traceStartTask "NuGet" nuSpec
    let parameters = NuGetDefaults() |> setParams
    try    
        let nuspecFile = createNuspecFile parameters nuSpec

        packSymbols parameters
        pack parameters nuspecFile
               
        if parameters.Publish then 
            publish parameters 
            if parameters.ProjectFile <> null then 
                publishSymbols parameters
    with
    | exn -> 
        exn.Message
          |> replaceAccessKey parameters.AccessKey
          |> failwith

    traceEndTask "NuGet" nuSpec


type NuSpecPackage = {
    Id : string
    Version : string
    Authors : string
    Owners : string
    Url: string
    IsLatestVersion: bool
    Created: DateTime
    Published: DateTime
    PackageHash: string
    PackageHashAlgorithm: string
    LicenseUrl : string
    ProjectUrl : string
    RequireLicenseAcceptance : bool
    Description : string
    Language : string
    Tags : string
}
with
    member x.Name = sprintf "%s %s" x.Id x.Version
    override x.ToString() = x.Name
    member x.DirectoryName = sprintf "%s.%s" x.Id x.Version
    member x.FileName = sprintf "%s.%s.nupkg" x.Id x.Version

let getNuspecProperties (nuspec : string) =
    let doc = XMLDoc nuspec
    let namespaces = ["x","http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"]
    let getValue name = 
        try
            doc
            |> XPathValue ("x:metadata/x:" + name) namespaces
        with
        | exn -> String.Empty

    {
       Id = getValue "id"
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
    

let feedUrl = "http://go.microsoft.com/fwlink/?LinkID=206669"

let private webClient = new System.Net.WebClient()

let discoverRepoUrl = 
    lazy (     
        let resp = webClient.DownloadString(feedUrl)
        let doc = XMLDoc resp

        doc.["service"].GetAttribute("xml:base"))

let getRepoUrl() = discoverRepoUrl.Force()      

let extractFeedPackageFromXml (entry:Xml.XmlNode) =
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
      Url = entry.["content"].GetAttribute("src")}

let getPackage repoUrl packageName version =
    let url:string = repoUrl + "Packages(Id='" + packageName + "',Version='" + version + "')"
    let resp = webClient.DownloadString(url)
    let doc = XMLDoc resp
   
    extractFeedPackageFromXml doc.["entry"]

let getFeedPackagesFromUrl (url:string) =
    let resp = webClient.DownloadString(url)
    let doc = XMLDoc resp
   
    [for entry in doc.["feed"].GetElementsByTagName("entry") -> extractFeedPackageFromXml entry]

let getLatestPackage repoUrl packageName =
    repoUrl + "Packages()?$filter=(Id%20eq%20'" + packageName + "')%20and%20IsLatestVersion"
    |> getFeedPackagesFromUrl
    |> Seq.head

let downloadPackage targetDir (package:NuSpecPackage) =
    ensureDirectory targetDir    
    let targetFileName = targetDir @@ package.FileName
    tracefn "Downloading package %s %s from %s and saving it to %s" package.Id package.Version package.Url targetFileName
    webClient.DownloadFile(package.Url,targetFileName)
    targetFileName