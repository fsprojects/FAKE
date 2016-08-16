module Fake.Runtime.FakeRuntime

open System
open System.IO
open Fake.Runtime

#if DOTNETCORE

type RawFakeSection =
  { Header : string
    Section : string }

let readFakeSection (scriptText:string) =
  let startString = "(* -- Fake Dependencies "
  let endString = "-- Fake Dependencies -- *)"
  let start = scriptText.IndexOf(startString) + startString.Length
  let endIndex = scriptText.IndexOf(endString) - 1
  if (start >= endIndex) then
    None
  else
    let fakeSectionWithVersion = scriptText.Substring(start, endIndex - start)
    let newLine = fakeSectionWithVersion.IndexOf("\n")
    let header = fakeSectionWithVersion.Substring(0, newLine).Trim()
    let fakeSection = fakeSectionWithVersion.Substring(newLine).Trim()
    Some { Header = header; Section = fakeSection}

type FakeSection =
 | PaketDependencies of Paket.Dependencies * group : String option

let readAllLines (r : TextReader) =
  seq {
    let mutable line = r.ReadLine()
    while not (isNull line) do
      yield line
      line <- r.ReadLine()
  }
let private dependenciesFileName = "paket.dependencies"
let parseHeader scriptCacheDir (f : RawFakeSection) =
  match f.Header with
  | "paket-inline" ->
    let dependenciesFile = Path.Combine(scriptCacheDir, dependenciesFileName)
    let fixedSection =
      f.Section.Split([| "\r\n"; "\r"; "\n" |], System.StringSplitOptions.None)
      |> Seq.map (fun line ->
        let replacePaketCommand (command:string) (line:string) =
          let trimmed = line.Trim()
          if trimmed.StartsWith command then
            let restString = trimmed.Substring(command.Length).Trim()
            let isValidPath = try Path.GetFullPath restString |> ignore; true with _ -> false
            let isAbsoluteUrl = match Uri.TryCreate(restString, UriKind.Absolute) with | true, _ -> true | _ -> false
            if isAbsoluteUrl || not isValidPath || Path.IsPathRooted restString then line
            else line.Replace(restString, Path.Combine("..", "..", restString))
          else line
        line
        |> replacePaketCommand "source"
        |> replacePaketCommand "cache"
      )
    File.WriteAllLines(dependenciesFile, fixedSection)
    PaketDependencies (Paket.Dependencies(dependenciesFile), None)
  | "paket.dependencies" ->
    let groupStart = "group "
    let fileStart = "file "
    let readLine (l:string) : (string * string) option =
      if l.StartsWith groupStart then ("group", (l.Substring groupStart.Length).Trim()) |> Some
      elif l.StartsWith fileStart then ("file", (l.Substring fileStart.Length).Trim()) |> Some
      elif String.IsNullOrWhiteSpace l then None
      else failwithf "Cannot recognise line in dependency section: '%s'" l
    let options =
      (use r = new StringReader(f.Section)
       readAllLines r |> Seq.toList)
      |> Seq.choose readLine
      |> dict
    let group =
      match options.TryGetValue "group" with
      | true, gr -> Some gr
      | _ -> None
    let file =
      match options.TryGetValue "file" with
      | true, depFile -> depFile
      | _ -> dependenciesFileName
    PaketDependencies (Paket.Dependencies(Path.GetFullPath file), group)
  | _ -> failwithf "unknown dependencies header '%s'" f.Header 

let paketCachingProvider printDetails cacheDir (paketDependencies:Paket.Dependencies) group =
  let groupStr = match group with Some g -> g | None -> "Main"
  let groupName = Paket.Domain.GroupName (groupStr)
  let framework = Paket.FrameworkIdentifier.DotNetStandard (Paket.DotNetStandardVersion.V1_6)
  let lockFilePath = Paket.DependenciesFile.FindLockfile paketDependencies.DependenciesFile
  let parent s = Path.GetDirectoryName s
  let comb name s = Path.Combine(s, name)
  let paketDependenciesHashFile = cacheDir |> comb "paket.depedencies.sha1"
  let saveDependenciesHash () =
    File.WriteAllText (paketDependenciesHashFile, HashGeneration.getStringHash (File.ReadAllText paketDependencies.DependenciesFile))
  let restoreOrUpdate () =
    if printDetails then Trace.log "Restoring with paket..."
    
    // Check if lockfile is outdated
    let hash = HashGeneration.getStringHash (File.ReadAllText paketDependencies.DependenciesFile)
    if File.Exists lockFilePath.FullName && (not <| File.Exists paketDependenciesHashFile || File.ReadAllText paketDependenciesHashFile <> hash) then
      Trace.log "paket lockfile is outdated..."
      File.Delete lockFilePath.FullName

    // Update
    if not <| File.Exists lockFilePath.FullName then
      paketDependencies.UpdateGroup(groupStr, false, false, false, false, false, Paket.SemVerUpdateMode.NoRestriction, false)
      |> ignore
      saveDependenciesHash ()

    // Restore
    paketDependencies.Restore(false, group, [], false, true)
    |> ignore
    let lockFile = paketDependencies.GetLockFile()
    let lockGroup = lockFile.GetGroup groupName
    
    // Write loadDependencies file (basically only for editor support)
    let loadFile = Path.Combine (cacheDir, "loadDependencies.fsx")
    if printDetails then Trace.log <| sprintf "Writing '%s'" loadFile
    // TODO: Make sure to create #if !FAKE block, because we don't actually need it.
    File.WriteAllText (loadFile, """printfn "loading dependencies... " """)
    
    // Retrieve assemblies
    lockGroup.Resolution
    |> Seq.map (fun kv -> 
      let packageName = kv.Key
      let package = kv.Value
      package)
    |> Seq.toList
    |> Paket.LoadingScripts.PackageAndAssemblyResolution.getPackageOrderResolvedPackage
    |> Seq.collect (fun p ->
      let installModel =
        paketDependencies.GetInstalledPackageModel(group, p.Name.ToString())
          .ApplyFrameworkRestrictions(Paket.Requirements.getRestrictionList p.Settings.FrameworkRestrictions)
      Paket.LoadingScripts.PackageAndAssemblyResolution.getDllsWithinPackage framework installModel)
    |> Seq.choose (fun fi ->
      let fullName = fi.FullName
      try let assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly fullName
          { ScriptRunner.AssemblyInfo.FullName = assembly.Name.FullName
            ScriptRunner.AssemblyInfo.Version = assembly.Name.Version.ToString()
            ScriptRunner.AssemblyInfo.Location = fullName } |> Some
      with e -> (if printDetails then Trace.log <| sprintf "Could not load '%s': %O" fullName e); None)
    |> Seq.toList
  // Restore or update immediatly, because or everything might be OK -> cached path.
  let mutable compileAssemblies = restoreOrUpdate()
  { new CoreCache.ICachingProvider with
      member x.CleanCache context =
        if printDetails then Trace.log "Invalidating cache..."
      member __.TryLoadCache (context) =
          let references = compileAssemblies |> List.map (fun (a:ScriptRunner.AssemblyInfo) -> a.Location)
          // TODO: Bug, Use runtime assemblies instead (currently not implemented in Paket...)!
          let runtimeAssemblies =
              compileAssemblies
              |> List.filter (fun (a:ScriptRunner.AssemblyInfo) -> 
                  not (a.Location.Contains("/ref/")))
          let fsiOpts = context.Config.CompileOptions.AdditionalArguments |> Yaaf.FSharp.Scripting.FsiOptions.ofArgs
          let newAdditionalArgs =
              { fsiOpts with
                  NoFramework = true
                  Debug = Some Yaaf.FSharp.Scripting.DebugMode.Portable }
              |> (fun options -> options.AsArgs)
              |> Seq.toList
          { context with
              Config =
                { context.Config with
                    CompileOptions =
                      { context.Config.CompileOptions with
                          AdditionalArguments = newAdditionalArgs
                          RuntimeDependencies = runtimeAssemblies @ context.Config.CompileOptions.RuntimeDependencies
                          CompileReferences = references @ context.Config.CompileOptions.CompileReferences
                      }
                }
          },
          None
          //Some { CompiledAssembly = cachedDll; Warnings = warningText }
      member x.SaveCache (context, cache) = () }

let restoreDependencies printDetails cacheDir section =
  match section with
  | PaketDependencies (paketDependencies, group) ->
    paketCachingProvider printDetails cacheDir paketDependencies group
    

let prepareFakeScript printDetails script =
  // read dependencies from the top
  let scriptDir = Path.GetDirectoryName (script)
  let cacheDir = Path.Combine(scriptDir, ".fake", Path.GetFileName(script))
  Directory.CreateDirectory (cacheDir) |> ignore
  let scriptText = File.ReadAllText(script)
  let section = readFakeSection scriptText
  match section with
  | Some s ->
    let section = parseHeader cacheDir s
    restoreDependencies printDetails cacheDir section
  | None ->
    if printDetails then Trace.traceFAKE "No dependencies section found in script: %s" script
    CoreCache.Cache.defaultProvider

let prepareAndRunScriptRedirect printDetails fsiOptions scriptPath envVars onErrMsg onOutMsg useCache =
  let provider = prepareFakeScript printDetails scriptPath
  use out = Yaaf.FSharp.Scripting.ScriptHost.CreateForwardWriter onOutMsg
  use err = Yaaf.FSharp.Scripting.ScriptHost.CreateForwardWriter onErrMsg
  let config =
    { ScriptRunner.FakeConfig.PrintDetails = printDetails
      ScriptRunner.FakeConfig.ScriptFilePath = scriptPath
      ScriptRunner.FakeConfig.CompileOptions = 
        { CompileReferences = []
          RuntimeDependencies = []
          AdditionalArguments = fsiOptions }
      ScriptRunner.FakeConfig.UseCache = useCache
      ScriptRunner.FakeConfig.Out = out
      ScriptRunner.FakeConfig.Err = err
      ScriptRunner.FakeConfig.Environment = envVars }
  CoreCache.runScriptWithCacheProvider config provider

let prepareAndRunScript printDetails fsiOptions scriptPath envVars useCache =
  prepareAndRunScriptRedirect printDetails fsiOptions scriptPath envVars (printf "%s") (printf "%s") useCache

#endif