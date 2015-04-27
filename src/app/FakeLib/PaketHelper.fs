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
      Version = null
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
      RunInParallel : bool
      ApiKey : string }

/// Paket push default parameters
let PaketPushDefaults() : PaketPushParams = 
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
      TimeOut = System.TimeSpan.MaxValue
      PublishUrl = null
      EndPoint =  null
      WorkingDir = "./temp"
      RunInParallel = true
      ApiKey = null }

/// Creates a new NuGet package by using Paket pack on all paket.template files in the working directory.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default parameters.
let Pack setParams =     
    let parameters : PaketPackParams = PaketPackDefaults() |> setParams
    traceStartTask "PaketPack" parameters.WorkingDir

    let xmlEncode (notEncodedText : string) = 
        if String.IsNullOrWhiteSpace notEncodedText then ""
        else XText(notEncodedText).ToString().Replace("ß", "&szlig;")

    let version = if String.IsNullOrWhiteSpace parameters.Version then "" else " version " + toParam parameters.Version
    let releaseNotes = if String.IsNullOrWhiteSpace parameters.ReleaseNotes then "" else " releaseNotes " + toParam (xmlEncode parameters.ReleaseNotes)
      
    let packResult = 
        ExecProcess 
            (fun info -> 
            info.FileName <- parameters.ToolPath
            info.Arguments <- sprintf "pack output %s%s%s" parameters.OutputPath version releaseNotes) parameters.TimeOut
    
    if packResult <> 0 then failwithf "Error during packing %s." parameters.WorkingDir
    traceEndTask "PaketPack" parameters.WorkingDir

/// Pushes all NuGet packages in the working dir to the server by using Paket push.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default parameters.
let Push setParams = 
    let parameters : PaketPushParams = PaketPushDefaults() |> setParams

    let packages = !! (parameters.WorkingDir @@ "/**/*.nupkg") |> Seq.toList    
    let url = if String.IsNullOrWhiteSpace parameters.PublishUrl then "" else " url " + toParam parameters.PublishUrl
    let endpoint = if String.IsNullOrWhiteSpace parameters.EndPoint then "" else " endpoint " + toParam parameters.EndPoint
    let key = if String.IsNullOrWhiteSpace parameters.ApiKey then "" else " apikey " + toParam parameters.ApiKey

    traceStartTask "PaketPush" (separated ", " packages)

    let tasks = 
        packages
        |> Seq.toArray
        |> Array.map (fun package -> async {
                let pushResult = 
                    ExecProcess (fun info -> 
                        info.FileName <- parameters.ToolPath
                        info.Arguments <- sprintf "push %s%s%s file %s" url endpoint key (toParam package)) parameters.TimeOut
                if pushResult <> 0 then failwithf "Error during pushing %s." package }) 

    if parameters.RunInParallel then         
        Async.Parallel tasks
        |> Async.RunSynchronously
        |> ignore
    else
        for task in tasks do
            Async.RunSynchronously task

    traceEndTask "PaketPush" (separated ", " packages)
