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
      LockDependencies : bool
      ReleaseNotes : string
      BuildConfig : string
      TemplateFile : string
      WorkingDir : string
      OutputPath : string }

/// Paket pack default parameters  
let PaketPackDefaults() : PaketPackParams = 
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      Version = null
      LockDependencies = false
      ReleaseNotes = null
      BuildConfig = null
      TemplateFile = null
      WorkingDir = "."
      OutputPath = "./temp" }

/// Paket push parameter type
type PaketPushParams = 
    { ToolPath : string
      TimeOut : TimeSpan
      PublishUrl : string
      EndPoint : string
      WorkingDir : string
      DegreeOfParallelism : int
      ApiKey : string }

/// Paket push default parameters
let PaketPushDefaults() : PaketPushParams = 
    { ToolPath = (findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket")) @@ "paket.exe"
      TimeOut = System.TimeSpan.MaxValue
      PublishUrl = null
      EndPoint =  null
      WorkingDir = "./temp"
      DegreeOfParallelism = 8
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
    let buildConfig = if String.IsNullOrWhiteSpace parameters.BuildConfig then "" else " buildconfig " + toParam parameters.BuildConfig
    let templateFile = if String.IsNullOrWhiteSpace parameters.TemplateFile then "" else " templatefile " + toParam parameters.TemplateFile
    let lockDependencies = if parameters.LockDependencies then " lock-dependencies" else ""

    let packResult = 
        let cmdArgs = sprintf "%s%s%s%s%s" version releaseNotes buildConfig templateFile lockDependencies
        ExecProcess 
            (fun info -> 
            info.FileName <- parameters.ToolPath
            info.Arguments <- sprintf "pack output %s %s" parameters.OutputPath cmdArgs) parameters.TimeOut
    
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
                                info.Arguments <- sprintf "push %s%s%s file %s" url endpoint key (toParam package)) parameters.TimeOut
                        if pushResult <> 0 then failwithf "Error during pushing %s." package })

            Async.Parallel tasks
            |> Async.RunSynchronously
            |> ignore

    else
        for package in packages do
            let pushResult = 
                ExecProcess (fun info -> 
                    info.FileName <- parameters.ToolPath
                    info.Arguments <- sprintf "push %s%s%s file %s" url endpoint key (toParam package)) parameters.TimeOut
            if pushResult <> 0 then failwithf "Error during pushing %s." package 

    traceEndTask "PaketPush" (separated ", " packages)
