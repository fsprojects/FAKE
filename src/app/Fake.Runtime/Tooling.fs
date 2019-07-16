/// This module is to provide tooling support for FAKE scripts and provide common operations
module Fake.Runtime.Tooling

(*
Remarks:
While this module is part of Fake.Runtime it is strictly speaking not part of `fake.exe`.
Instead this code is referenced by tooling providers. Therefore we need some assumptions:
- This code needs to be compatible with different `fake.exe` versions
  This is relevant especially for cache entries, we can try to use them but should not make them invalid when user is still on an older version if possible.
- This code needs to be lightweight (ie not write any files users might not have under gitignore)
*)
open System
open Fake.Runtime
open Fake.Runtime.Trace
open System
open System.IO
open System.IO.Compression
open System.Threading.Tasks
open System.Net
open Newtonsoft.Json.Linq

let private compatibleFakeVersion = "5.15.3"
let private fakeDownloadUri version =
    sprintf "https://github.com/fsharp/FAKE/releases/download/%s/fake-dotnetcore-portable.zip" version
    |> Uri
let internal downloadAndExtract (uri:Uri) directory = async {
    use client = new WebClient()
    client.Headers.Add("user-agent", "Ionide")
    let tempFile = Path.GetTempFileName()
    try
      do! client.DownloadFileTaskAsync(uri, tempFile) |> Async.AwaitTask
      ZipFile.ExtractToDirectory(tempFile, directory)
    finally
      try
        File.Delete tempFile
      with e -> ()
}
let private downloadAndGetFakeDll uri directory = async {
    let fakeDll = Path.Combine(directory, "fake.dll")
    if File.Exists fakeDll then
      return fakeDll
    else    
      do! downloadAndExtract uri directory
      if not (File.Exists fakeDll) then
        failwithf "No 'fake.dll' in '%s' after downloading fake" directory
      return fakeDll
}
let mutable private fakeDll = None
let private locker = obj()
let private getFakeRuntimeAsTask() =
    let installRuntime () =
      // use ~/.fsac if possible
      let usersDir = Environment.GetFolderPath Environment.SpecialFolder.UserProfile
      let fsac = System.IO.Path.Combine(usersDir, ".fsac")
      if not (Directory.Exists fsac) then Directory.CreateDirectory fsac |> ignore
      let runtimeDir = Path.Combine(fsac, "fake", compatibleFakeVersion)
      if not (Directory.Exists runtimeDir) then Directory.CreateDirectory runtimeDir |> ignore
      let downloadUri = fakeDownloadUri compatibleFakeVersion
      let t = downloadAndGetFakeDll downloadUri runtimeDir |> Async.StartAsTask
      t
    let checkAndReturn (s:Task<string>) =
      if s.IsCompleted && not (File.Exists s.Result) then installRuntime()
      else s
    match fakeDll with
    | None ->
      lock locker (fun _ ->
        match fakeDll with
        | None ->
          let t = installRuntime ()
          fakeDll <- Some t
          t
        | Some s -> checkAndReturn s
      )
    | Some s -> checkAndReturn s

let getFakeRuntime () : Async<string> =
    getFakeRuntimeAsTask () |> Async.AwaitTask  
type Declaration =
    { File : string
      Line : int
      Column : int }
type FakeContext =
    { DotNetRuntime : string }
/// a target dependency, either a hard or a soft dependency.
type Dependency =
    { Name : string
      Declaration : Declaration }
/// a FAKE target, its description and its relations to other targets (dependencies), including the declaration lines of the target and the dependencies.           
type Target =
    { Name : string
      HardDependencies : Dependency []
      SoftDependencies : Dependency []
      Declaration : Declaration
      Description : string }
type internal DebugTraceListener(printer) =
    interface ITraceListener with
        /// Writes the given message to the Console.
        member this.Write msg = 
          //let color = colorMap msg
          match msg with
          | ImportantMessage text | ErrorMessage text ->
            //writeText true color true text
            printer true text
          | LogMessage(text, newLine) | TraceMessage(text, newLine) ->
            //writeText false color newLine text
            printer false text
let mutable private loggingSetup = false
let private checkLogging () =
    if not loggingSetup then failwithf "Please call 'setupLogging' first"

let setupLogging (printer : bool -> string -> unit) : unit  =
    if not loggingSetup then
        listeners.Clear()
        listeners.Add(DebugTraceListener(printer))
        loggingSetup <- true

type DetectionInfo =
    internal
        { Config : Runners.FakeConfig
          Prepared : FakeRuntime.PrepareInfo }          

/// Detect if a given file is a FAKE script
let detectFakeScript (file) : DetectionInfo option =
    if not (File.Exists file) || not (file.EndsWith ".fsx") then None
    else
        checkLogging()
        let config = 
          { FakeRuntime.createConfigSimple Verbose [] file [] true false with
              // This prevents `Paket.Core` to overwrite `Paket.Restore.targets`
              UseSimpleRestore = true }
        // the `tryPrepareFakeScript` function will not write anything to the FileSystem if the script is not a FAKE script.        
        match FakeRuntime.tryPrepareFakeScript config with
        | FakeRuntime.TryPrepareInfo.Prepared prepared -> Some { Config = config; Prepared = prepared }
        | _ -> None
let getProjectOptions { Config = config; Prepared = prepared } : string[] =
    checkLogging()
    let prov = FakeRuntime.restoreAndCreateCachingProvider prepared
    let context, cache = CoreCache.prepareContext config prov
    let args = context.Config.CompileOptions.AsArgs
    let args =
      args |> Seq.toList
      |> List.filter (fun arg -> arg <> "--")
    
    "--simpleresolution" :: "--targetprofile:netstandard" :: "--nowin32manifest" :: args
    |> List.toArray

type GetTargetsWarningOrErrorType =
    | NoFakeScript = 1
    | MissingFakeCoreTargets = 2
    /// Most likely due to missing `Target.initEnvironment()`
    | MissingNavigationInfo = 4
    | FakeCoreTargetsOlderThan5_15 = 3
    | ExecutionError = 5
    /// Most likely due to missing `Target.runOrDefault`
    | EmptyInfoFile = 5
type WarningOrError =
  { Type : GetTargetsWarningOrErrorType 
    Message : string }  
type GetTargetsResult = { WarningsAndErrors : WarningOrError []; Targets : Target [] }

let private runProcess (log: bool -> string -> unit) (workingDir: string) (exePath: string) (args: string) =
    let psi = System.Diagnostics.ProcessStartInfo()
    psi.FileName <- exePath
    psi.WorkingDirectory <- workingDir
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.Arguments <- args
    psi.CreateNoWindow <- true
    psi.UseShellExecute <- false

    use p = new System.Diagnostics.Process()
    p.StartInfo <- psi
    p.OutputDataReceived.Add(fun ea -> log false (ea.Data))
    p.ErrorDataReceived.Add(fun ea -> log true (ea.Data))

    p.Start() |> ignore
    p.BeginOutputReadLine()
    p.BeginErrorReadLine()
    p.WaitForExit()

    let exitCode = p.ExitCode

    exitCode

let private formatOutput (lines:ResizeArray<bool*string>) =
  sprintf "Output: \n%s" (String.Join("\n", lines |> Seq.map (fun (isErr, msg) -> (if isErr then "Err: " else "Out: ") + msg)))

let private getTargetsLegacy (file:string) (ctx:FakeContext) : Async<GetTargetsResult> = async {
    // pre Fake.Core.Targets upgrade
    let decl = 
        { File = null
          Line = 0
          Column = 0 }
    let! rt = getFakeRuntime ()
    let lines = ResizeArray<_>()
    let fileName = Path.GetFileName file
    let workingDir = Path.GetDirectoryName file
    let fakeArgs = sprintf "-s run \"%s\" -- --list" fileName
    let args = sprintf "\"%s\" %s" rt fakeArgs
    let exitCode = runProcess (fun isErr msg -> lines.Add(isErr, msg)) workingDir ctx.DotNetRuntime args
    if exitCode <> 0 then
      let msg = sprintf "Running Script 'fake %s' failed (%d). %s" fakeArgs exitCode (formatOutput lines)
      return { WarningsAndErrors = [| { Type = GetTargetsWarningOrErrorType.ExecutionError; Message = msg } |]; Targets = [||] }
    else
      let targets =
        lines
        |> Seq.choose (fun (isErr, msg) -> if isErr then None else Some msg)
        |> Seq.filter (isNull >> not)
        |> Seq.choose (fun line ->
          // heuristic
          if line.StartsWith "   " then
            let targetNameAndDesc = line.Substring(3)
            // If people use ' - ' in their target name we are lost...
            let splitIdx = targetNameAndDesc.IndexOf(" - ")
            let targetName, description =
              if splitIdx > 0 then targetNameAndDesc.Substring(0, splitIdx), targetNameAndDesc.Substring(splitIdx + 3)
              else targetNameAndDesc, ""
            { Name = targetName
              HardDependencies = [||]
              SoftDependencies = [||]
              Declaration = decl
              Description = description }
            |> Some
          else None)
        |> Seq.toArray
      return { WarningsAndErrors = [||]; Targets = targets }
}
let private getTargetsJson (file:string) (ctx:FakeContext) : Async<GetTargetsResult> = async {
    // with --write-info support
    let! rt = getFakeRuntime ()
    let lines = ResizeArray<_>()
    let fileName = Path.GetFileName file
    let resultsFile = Path.GetTempFileName()
    try
      let workingDir = Path.GetDirectoryName file
      // the nocache option is needed because if a script only changes whitespace,
      // FAKE is clever enough to not recompile
      let fakeArgs = sprintf "-s run --nocache --fsiargs \"--debug:portable --optimize-\" \"%s\" -- --write-info \"%s\"" fileName resultsFile
      let args = sprintf "\"%s\" %s" rt fakeArgs
      let exitCode = runProcess (fun isErr msg -> lines.Add(isErr, msg)) workingDir ctx.DotNetRuntime args
      if exitCode <> 0 then
        let msg = sprintf "Running Script 'fake %s' failed (%d). %s" fakeArgs exitCode (formatOutput lines)
        return { WarningsAndErrors = [| { Type = GetTargetsWarningOrErrorType.ExecutionError; Message = msg } |]; Targets = [||] }
      else
        let jsonStr = File.ReadAllText resultsFile
        if String.IsNullOrEmpty jsonStr then
          let msg = sprintf "Running Script 'fake %s' did not create an info file. Are you missing the `Target.runOrDefault` call at the end of your script?" fakeArgs
          return { WarningsAndErrors = [| { Type = GetTargetsWarningOrErrorType.EmptyInfoFile; Message = msg } |]; Targets = [||] }
        else
          let jobj = JObject.Parse jsonStr
          let parseStringWithNull (t:JToken) =
              if isNull t || t.Type = JTokenType.Null then null
              else string t 
          let parseDecl (t:JToken) =
              { File = parseStringWithNull t.["file"]; Line = int t.["line"]; Column = int t.["column"] }
          let parseDep (t:JToken) =
              { Name = string t.["name"]; Declaration = parseDecl t.["declaration"] }
          let parseArray parseItem (a:JToken) =
              (a :?> JArray)
              |> Seq.map parseItem
              |> Seq.toArray
          let parseTarget (t:JToken) =
              { Name = string t.["name"]
                Declaration = parseDecl t.["declaration"]
                HardDependencies = parseArray parseDep t.["hardDependencies"]
                SoftDependencies = parseArray parseDep t.["softDependencies"]
                Description = string t.["description"] }
          let jTargets = jobj.["targets"] :?> JArray
          let targets =
            jTargets
            |> Seq.map parseTarget
            |> Seq.toArray
          return { WarningsAndErrors = [||]; Targets = targets }
    finally
      try File.Delete resultsFile with e -> ()     
}      
let private getTargetsVersion (context:Runners.FakeContext) : Version option =
    let runtimeDeps : Runners.AssemblyInfo list = context.Config.RuntimeOptions.RuntimeDependencies
    let targetVersion =
      runtimeDeps
      |> Seq.map (fun r -> System.Reflection.AssemblyName(r.FullName))
      |> Seq.tryFind (fun a -> a.Name = "Fake.Core.Target")
      |> Option.map (fun a -> a.Version)
    targetVersion

// filePath -> hash * Task<Results>
let private getTargetsDict = new System.Collections.Concurrent.ConcurrentDictionary<string, string * Task<GetTargetsResult>>()
let getTargets (file:string) (ctx:FakeContext) : Async<GetTargetsResult> = async {
    checkLogging()
    match detectFakeScript file with
    | None ->
      return { WarningsAndErrors = [|{ Type = GetTargetsWarningOrErrorType.NoFakeScript; Message = "This file is not a valid FAKE 5 script" }|]; Targets = [||] }// Ok [| errorTarget file "not a FAKE 5 script" "This file is not a valid FAKE 5 script" |]
    | Some { Config = config; Prepared = prepared } ->
        // check cache
        let cacheDir = prepared._CacheDir
        let tokenized = config.ScriptTokens.Value
        let scripts = HashGeneration.getAllScripts false [] tokenized file
        let hash = HashGeneration.getScriptHash scripts []
        let startProcess () =
            async {
                let! rt = getFakeRuntime ()
                let prov = FakeRuntime.restoreAndCreateCachingProvider prepared
                let context, cache = CoreCache.prepareContext config prov
                match getTargetsVersion context with
                | None ->
                  return { WarningsAndErrors = [| { Type = GetTargetsWarningOrErrorType.MissingFakeCoreTargets; Message = "The 'Fake.Core.Target' package is not referenced." }|]; Targets = [||] }
                | Some v when v < Version(5, 15) ->
                  let! resp = getTargetsLegacy file ctx
                  return { resp with WarningsAndErrors = [| yield! resp.WarningsAndErrors; yield { Type = GetTargetsWarningOrErrorType.FakeCoreTargetsOlderThan5_15; Message = "this script should be updated to at least Fake.Core.Target 5.15" }|]; }
                | Some v ->
                  // Use newer logic
                  let! resp = getTargetsJson file ctx
                  let warnings =
                    if resp.Targets.Length > 0 then
                      if isNull resp.Targets.[0].Declaration.File then [|{ Type = GetTargetsWarningOrErrorType.MissingNavigationInfo; Message="navigation is missing, are you missing 'Target.initEnvironment()` at the top?"}|]
                      else [||]
                    else [||]
                  return { resp with WarningsAndErrors = [|yield! resp.WarningsAndErrors; yield! warnings|] }
            }
            |> Async.StartAsTask
             
        // Cache targets until file is modified.      
        let hash, task = getTargetsDict.AddOrUpdate(file, (fun _ -> hash, startProcess ()), (fun _ (oldHash, oldTask) -> if oldHash = hash then hash, oldTask else hash, startProcess()))
        return! task |> Async.AwaitTask
}
  