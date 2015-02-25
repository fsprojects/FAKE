/// Contains helper functions and task which allow to inspect, create and publish [NuGet](https://www.nuget.org/) packages with [Paket](http://fsprojects.github.io/Paket/index.html).
module Fake.Paket

open System
open System.IO
open System.Xml.Linq
open System.Xml.Linq

/// Paket pack parameter type
type PaketPackParams = 
    { ToolPath : string
      TimeOut : TimeSpan
      Version : string
      ReleaseNotes : string
      WorkingDir : string
      OutputPath : string }

/// Paket pack default parameters  
let PaketPackDefaults() : PaketPackParams = 
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      Version = 
          if not isLocalBuild then buildVersion
          else "0.1.0.0"
      ReleaseNotes = null
      WorkingDir = "."
      OutputPath = "./temp" }

/// Paket push parameter type
type PaketPushParams = 
    { ToolPath : string
      TimeOut : TimeSpan
      PublishUrl : string
      EndPoint : string
      WorkingDir : string
      ApiKey : string }

/// Paket push default parameters
let PaketPushDefaults() : PaketPushParams = 
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      PublishUrl = "https://nuget.org"
      EndPoint =  "/api/v2/package"
      WorkingDir = "."
      ApiKey = null }

/// Creates a new NuGet package by using Paket pack on all paket.template files in the working directory.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default parameters.
let Pack setParams =     
    let parameters : PaketPackParams = PaketPackDefaults() |> setParams
    traceStartTask "PaketPack" parameters.WorkingDir

    let xmlEncode (notEncodedText : string) = 
        if System.String.IsNullOrWhiteSpace notEncodedText then ""
        else XText(notEncodedText).ToString().Replace("ß", "&szlig;")
    
    let packResult = 
        ExecProcess 
            (fun info -> 
            info.FileName <- parameters.ToolPath
            info.Arguments <- sprintf "pack output %s version \"%s\" releaseNotes \"%s\"" parameters.OutputPath 
                                  parameters.Version (xmlEncode parameters.ReleaseNotes)) parameters.TimeOut
    
    if packResult <> 0 then failwithf "Error during packing %s." parameters.WorkingDir
    traceEndTask "PaketPack" parameters.WorkingDir

/// Pushes all NuGet packages in the working dir to the server by using Paket push.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default parameters.
let Push setParams = 
    let parameters : PaketPushParams = PaketPushDefaults() |> setParams

    let packages = !! (parameters.WorkingDir @@ "/**/*.nupkg") |> Seq.toList
    traceStartTask "PaketPush" (separated ", " packages)
    for package in packages do
        let pushResult = 
            ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.Arguments <- sprintf "push url %s endpoint %s file %s" parameters.PublishUrl package
                if parameters.ApiKey <> null then
                  info.Arguments <- sprintf "%s apikey %s" info.Arguments parameters.ApiKey) System.TimeSpan.MaxValue
        if pushResult <> 0 then failwithf "Error during pushing %s." package
    traceEndTask "PaketPush" (separated ", " packages)
