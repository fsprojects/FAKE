module Fake.Runtime.FakeRuntime

open System
open System.IO
open Fake.Runtime
open Paket

type FakeSection =
 | PaketDependencies of Paket.Dependencies * Lazy<Paket.DependenciesFile> * group : String option

let readAllLines (r : TextReader) =
  seq {
    let mutable line = r.ReadLine()
    while not (isNull line) do
      yield line
      line <- r.ReadLine()
  }
let private dependenciesFileName = "paket.dependencies"

type InlinePaketDependenciesSection =
  { Header : string
    Section : string }

let writeFixedPaketDependencies scriptCacheDir (f : InlinePaketDependenciesSection) =
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
    PaketDependencies (Dependencies dependenciesFile, (lazy DependenciesFile.ReadFromFile dependenciesFile), None)
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
    let fullpath = Path.GetFullPath file
    PaketDependencies (Dependencies fullpath, (lazy DependenciesFile.ReadFromFile fullpath), group)
  | _ -> failwithf "unknown dependencies header '%s'" f.Header

let tryReadPaketDependenciesFromScript (tokenized:Fake.Runtime.FSharpParser.TokenizedScript) cacheDir (scriptPath:string) =
  let pRefStr = "paket:"
  let grRefStr = "groupref"
  let groupReferences, paketLines =
    FSharpParser.findInterestingItems tokenized
    |> Seq.choose (fun item -> 
        match item with
        | FSharpParser.InterestingItem.Reference ref when ref.StartsWith pRefStr ->
          let sub = ref.Substring (pRefStr.Length)
          Some (sub.TrimStart[|' '|])
        | _ -> None)
    |> Seq.toList
    |> List.partition (fun ref -> ref.StartsWith(grRefStr, System.StringComparison.OrdinalIgnoreCase))
  let paketCode =
    paketLines
    |> String.concat "\n"
  let paketGroupReferences =
    groupReferences
    |> List.map (fun groupRefString ->
      let raw = groupRefString.Substring(grRefStr.Length).Trim()
      let commentStart = raw.IndexOf "//"
      if commentStart >= 0 then raw.Substring(0, commentStart).Trim()
      else raw)

  if paketCode <> "" && paketGroupReferences.Length > 0 then
    failwith "paket code in combination with a groupref is currently not supported!"

  if paketGroupReferences.Length > 1 then
    failwith "multiple paket groupref are currently not supported!"

  if paketCode <> "" then
    let fixDefaults (paketCode:string) =
      let lines = paketCode.Split([|'\r';'\n'|]) |> Array.map (fun line -> line.ToLower().TrimStart())
      let storageRef = "storage"
      let sourceRef = "source"
      let frameworkRef = "framework"
      let restrictionRef = "restriction"
      let containsStorage = lines |> Seq.exists (fun line -> line.StartsWith(storageRef))
      let containsSource = lines |> Seq.exists (fun line -> line.StartsWith(sourceRef))
      let containsFramework = lines |> Seq.exists (fun line -> line.StartsWith(frameworkRef))
      let containsRestriction = lines |> Seq.exists (fun line -> line.StartsWith(restrictionRef))
      paketCode
      |> fun p -> if containsStorage then p else "storage: none" + "\n" + p
      |> fun p -> if containsSource then p else "source https://api.nuget.org/v3/index.json" + "\n" + p
      |> fun p -> if containsFramework || containsRestriction then p 
                  else "framework: netstandard2.0" + "\n" + p

    { Header = "paket-inline"
      Section = fixDefaults paketCode }
    |> writeFixedPaketDependencies cacheDir
    |> Some
  else
    let file = dependenciesFileName
    match paketGroupReferences with
    | [] ->
      None
    | group :: _ ->
      let fullpath = Path.GetFullPath file
      PaketDependencies (Dependencies fullpath, (lazy DependenciesFile.ReadFromFile fullpath), Some group)
      |> Some


type AssemblyData =
  { IsReferenceAssembly : bool
    Info : Runners.AssemblyInfo }

let paketCachingProvider (script:string) (logLevel:Trace.VerboseLevel) cacheDir (paketApi:Paket.Dependencies) (paketDependenciesFile:Lazy<Paket.DependenciesFile>) group =
  use __ = Fake.Profile.startCategory Fake.Profile.Category.Paket
  let groupStr = match group with Some g -> g | None -> "Main"
  let groupName = Paket.Domain.GroupName (groupStr)
#if DOTNETCORE
  //let framework = Paket.FrameworkIdentifier.DotNetCoreApp (Paket.DotNetCoreAppVersion.V2_0)
  let framework = Paket.FrameworkIdentifier.DotNetStandard (Paket.DotNetStandardVersion.V2_0)
#else
  let framework = Paket.FrameworkIdentifier.DotNetFramework (Paket.FrameworkVersion.V4_6)
#endif
  let lockFilePath = Paket.DependenciesFile.FindLockfile paketApi.DependenciesFile
  let parent s = Path.GetDirectoryName s
  let comb name s = Path.Combine(s, name)
  let assemblyCacheHashFile = Path.Combine(cacheDir, "assemblies.cached")
  let assemblyCacheFile = Path.Combine(cacheDir, "assemblies.txt")

#if DOTNETCORE
  let getCurrentSDKReferenceFiles() =
    // We need use "real" reference assemblies as using the currently running runtime assemlies doesn't work:
    // see https://github.com/fsharp/FAKE/pull/1695

    // Therefore we download the reference assemblies (the NETStandard.Library package)
    // and add them in addition to what we have resolved, 
    // we use the sources in the paket.dependencies to give the user a chance to overwrite.

    // Note: This package/version needs to updated together with our "framework" variable below and needs to 
    // be compatible with the runtime we are currently running on.
    let rootDir = Directory.GetCurrentDirectory()
    let packageName = Domain.PackageName("NETStandard.Library")
    let version = SemVer.Parse("2.0.2")
    let existingpkg = NuGetCache.GetTargetUserNupkg packageName version
    let extractedFolder =
      if File.Exists existingpkg then
        // Shortcut in order to prevent requests to nuget sources if we have it downloaded already
        Path.GetDirectoryName existingpkg
      else
        let sources = paketDependenciesFile.Value.Groups.[groupName].Sources
        let versions =
          Paket.NuGet.GetVersions false None rootDir (PackageResolver.GetPackageVersionsParameters.ofParams sources groupName packageName)
          |> Async.RunSynchronously
          |> dict
        let source =
          match versions.TryGetValue(version) with
          | true, v when v.Length > 0 -> v |> Seq.head
          | _ -> failwithf "Could not find package '%A' with version '%A' in any package source of group '%A', but fake needs this package to compile the script" packageName version groupName    
        
        let _, extractedFolder =
          Paket.NuGet.DownloadAndExtractPackage
            (None, rootDir, false, PackagesFolderGroupConfig.NoPackagesFolder,
             source, [], Paket.Constants.MainDependencyGroup,
             packageName, version, PackageResolver.ResolvedPackageKind.Package, false, false, false, false)
          |> Async.RunSynchronously
        extractedFolder
    let sdkDir = Path.Combine(extractedFolder, "build", "netstandard2.0", "ref")
    Directory.GetFiles(sdkDir, "*.dll")
    |> Seq.toList
#endif

  let writeIntellisenseFile cacheDir (context : Paket.LoadingScripts.ScriptGeneration.PaketContext) =
    // Write loadDependencies file (basically only for editor support)
    let intellisenseFile = Path.Combine (cacheDir, Runners.loadScriptName)
    if logLevel.PrintVerbose then Trace.log <| sprintf "Writing '%s'" intellisenseFile
    let groupScripts = Paket.LoadingScripts.ScriptGeneration.generateScriptContent context
    let _, groupScript =
      match groupScripts with
      | [] -> failwith "generateScriptContent returned []"
      | [h] -> failwithf "generateScriptContent returned a single item: %A" h
      | [ _, scripts; _, [groupScript] ] -> scripts, groupScript
      | _ -> failwithf "generateScriptContent returned %A" groupScripts

    
    let rootDir = DirectoryInfo cacheDir
    //for sd in scripts do
    //    let scriptPath = Path.Combine (rootDir.FullName , sd.PartialPath)
    //    let scriptDir = Path.GetDirectoryName scriptPath |> Path.GetFullPath |> DirectoryInfo
    //    scriptDir.Create()
    //    sd.Save rootDir

    let content = groupScript.RenderDirect rootDir (FileInfo intellisenseFile)

    // TODO: Make sure to create #if !FAKE block, because we don't actually need it.
    let intellisenseContents =
      [| "// This file is automatically generated by FAKE"
         "// This file is needed for IDE support only"
         "#if !FAKE"
         content
         "#endif" |]
    File.WriteAllLines (intellisenseFile, intellisenseContents)
    
  let lockFile = lazy LockFile.LoadFrom(lockFilePath.FullName)
  let cache = lazy DependencyCache(lockFile.Value)
  let retrieveInfosUncached () =
    match lockFile.Value.Groups |> Map.tryFind groupName with
    | Some g -> ()
    | None -> failwithf "The group '%s' was not found in the lockfile. You might need to run 'paket install' first!" groupName.Name
    
    let (cache:DependencyCache) = cache.Value
    let orderedGroup = cache.OrderedGroups groupName // lockFile.GetGroup groupName
    
    //dependencyCacheProfile.Dispose()

    let rid =
#if DOTNETCORE
        let ridString = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier()
#else
        let ridString = "win"
#endif
        Paket.Rid.Of(ridString)

    // get runtime graph
    let graph =
      async {
        if logLevel.PrintVerbose then Trace.log <| sprintf "Calculating the runtime graph..."
        use runtimeGraphProfile = Fake.Profile.startCategory Fake.Profile.Category.PaketRuntimeGraph
        let result =
          orderedGroup
          |> Seq.choose (fun p ->
            RuntimeGraph.getRuntimeGraphFromNugetCache cacheDir (Some PackagesFolderGroupConfig.NoPackagesFolder) groupName p.Resolved)
          |> RuntimeGraph.mergeSeq
        runtimeGraphProfile.Dispose()
        return result
      }
      |> Async.StartAsTask

    let filterValidAssembly (isSdk, isReferenceAssembly, fi:FileInfo) =
        let fullName = fi.FullName
        try let assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly fullName
            { IsReferenceAssembly = isReferenceAssembly
              Info =
                { Runners.AssemblyInfo.FullName = assembly.Name.FullName
                  Runners.AssemblyInfo.Version = assembly.Name.Version.ToString()
                  Runners.AssemblyInfo.Location = fullName } } |> Some
        with e -> 
            if logLevel.PrintVerbose then Trace.log <| sprintf "Could not load '%s': %O" fullName e
            None

    // Retrieve assemblies
    use __ = Fake.Profile.startCategory Fake.Profile.Category.PaketGetAssemblies
    if logLevel.PrintVerbose then Trace.log <| sprintf "Retrieving the assemblies (rid: '%O')..." rid

    orderedGroup
    |> Seq.filter (fun p ->
      if p.Name.ToString() = "Microsoft.FSharp.Core.netcore" then
        eprintfn "Ignoring 'Microsoft.FSharp.Core.netcore' please tell the package authors to fix their package and reference 'FSharp.Core' instead."
        false
      else true)
    |> Seq.map (fun p -> async {
      match cache.InstallModelTask groupName p.Name with
      | None -> return failwith "InstallModel not cached?"
      | Some installModelTask ->
        let! installModel = installModelTask |> Async.AwaitTask
        let installModel =
          installModel
            .ApplyFrameworkRestrictions(Paket.Requirements.getExplicitRestriction p.Settings.FrameworkRestrictions)
        let targetProfile = Paket.TargetProfile.SinglePlatform framework
  
        let refAssemblies =
          installModel.GetCompileReferences targetProfile
          |> Seq.map (fun fi -> true, FileInfo fi.Path)
        let runtimeAssemblies =
          installModel.GetRuntimeAssemblies graph.Result rid targetProfile
          |> Seq.map (fun fi -> false, FileInfo fi.Library.Path)
        let result =
          Seq.append runtimeAssemblies refAssemblies
          |> Seq.filter (fun (_, r) -> r.Extension = ".dll" || r.Extension = ".exe" )
          |> Seq.map (fun (isRef, fi) -> false, isRef, fi)
          |> Seq.choose filterValidAssembly
          |> Seq.toList
        return result })
    |> Async.Parallel
    |> fun asy ->
        let work = asy |> Async.StartAsTask
#if DOTNETCORE
        let sdkRefs =
            (getCurrentSDKReferenceFiles()
                 |> Seq.map (fun file -> true, true, FileInfo file)
                 |> Seq.choose filterValidAssembly)
#endif  
        work.Result
        |> Seq.collect id
#if DOTNETCORE
        // Append sdk files as references in order to properly compile, for runtime we can default to the default-load-context.
        |> Seq.append sdkRefs
#endif  
    // If we have multiple select one
    |> Seq.groupBy (fun ass -> ass.IsReferenceAssembly, System.Reflection.AssemblyName(ass.Info.FullName).Name)
    |> Seq.map (fun (_, group) -> group |> Seq.maxBy(fun ass -> ass.Info.Version))
    |> Seq.toList

  let restoreOrUpdate () =
    if logLevel.PrintVerbose then Trace.log "Restoring with paket..."

    // Update
    let localLock = script + ".lock" // the primary lockfile-path </> lockFilePath.FullName is implementation detail
    let needLocalLock = lockFilePath.FullName.Contains (Path.GetFullPath cacheDir) // Only primary if not external already.
    let localLockText = lazy File.ReadAllText localLock
    if needLocalLock && File.Exists localLock && (not (File.Exists lockFilePath.FullName) || localLockText.Value <> File.ReadAllText lockFilePath.FullName) then
      File.Copy(localLock, lockFilePath.FullName)
    if needLocalLock && not (File.Exists localLock) then
      File.Delete lockFilePath.FullName
    if not <| File.Exists lockFilePath.FullName then
      if logLevel.PrintVerbose then Trace.log "Lockfile was not found. We will update the dependencies and write our own..."
      try
        paketApi.UpdateGroup(groupStr, false, false, false, false, false, Paket.SemVerUpdateMode.NoRestriction, false)
        |> ignore
      with
      | e when e.Message.Contains "Did you restore groups" ->
        // See https://github.com/fsharp/FAKE/issues/1672
        // and https://github.com/fsprojects/Paket/issues/2785
        // We do a restore anyway.
        eprintfn "paket update has thrown an error: %O" e
        ()
      if needLocalLock then File.Copy(lockFilePath.FullName, localLock)
    
    // TODO: Check if restore is up-to date and skip all paket calls (load assembly-list from a new cache)
    // Restore
    paketApi.Restore((*false, group, [], false, true*))
    |> ignore
    
  // Restore load-script, as we don't need it create it in the background.
  let writeIntellisenseTask =
    lazy
        async {
          try
              cache.Value.StartSetupGroup(groupName) |> ignore
              do! cache.Value.AwaitFinishSetup()
              writeIntellisenseFile cacheDir {
                Cache = cache.Value
                ScriptType = Paket.LoadingScripts.ScriptGeneration.ScriptType.FSharp
                Groups = [groupName]
                DefaultFramework = false, (Paket.FrameworkIdentifier.DotNetFramework (Paket.FrameworkVersion.V4_7_1))
              }
          with e ->
              eprintfn "Failed to write intellisense script: %O" e
        } |> Async.StartAsTask

  let readFromCache () =
      File.ReadLines(assemblyCacheFile)
      |> Seq.map (fun line ->
        let splits = line.Split(';')
        let isRef = bool.Parse splits.[0]
        let ver = splits.[1]
        let loc = splits.[2]
        let fullName = splits.[3]
        { IsReferenceAssembly = isRef
          Info =
            { Runners.AssemblyInfo.FullName = fullName
              Runners.AssemblyInfo.Version = ver
              Runners.AssemblyInfo.Location = loc } })
      |> Seq.toList
      
  let writeToCache (list:AssemblyData list) =
      list 
      |> Seq.map (fun item -> 
        sprintf "%b;%s;%s;%s" 
            item.IsReferenceAssembly
            item.Info.Version
            item.Info.Location
            item.Info.FullName)
      |> fun lines -> File.WriteAllLines(assemblyCacheFile, lines)
      File.Copy(lockFilePath.FullName, assemblyCacheHashFile)
      ()
      
  let getKnownAssemblies () =
    let inline getUncached () =
        let list = retrieveInfosUncached()
        writeIntellisenseTask.Value |> ignore
        writeToCache list
        list
        
    if File.Exists assemblyCacheHashFile && File.Exists assemblyCacheFile && File.ReadAllText assemblyCacheHashFile = File.ReadAllText lockFilePath.FullName then 
        // get assembly list from cache
        try readFromCache()
        with e ->
            eprintfn "Caching assembly list failed: %O" e
            getUncached()
    else
        getUncached()
  
  // Restore or update immediatly, because or everything might be OK -> cached path.
  //let knownAssemblies, writeIntellisenseTask = restoreOrUpdate()
  do restoreOrUpdate()
  let knownAssemblies = lazy getKnownAssemblies()

  if logLevel.PrintVerbose then
    Trace.tracefn "Known assemblies: \n\t%s" (System.String.Join("\n\t", knownAssemblies.Value |> Seq.map (fun a -> sprintf " - %s: %s (%s)" (if a.IsReferenceAssembly then "ref" else "lib") a.Info.Location a.Info.Version)))
  { new CoreCache.ICachingProvider with
      member x.CleanCache context =
        if logLevel.PrintVerbose then Trace.log "Invalidating cache..."
        let assemblyPath, warningsFile = context.CachedAssemblyFilePath + ".dll", context.CachedAssemblyFilePath + ".warnings"
        try File.Delete warningsFile; File.Delete assemblyPath
        with e -> Trace.traceError (sprintf "Failed to delete cached files: %O" e)
      member __.TryLoadCache (context) =
          let references =
              knownAssemblies.Value
              |> List.filter (fun a -> a.IsReferenceAssembly)
              |> List.map (fun (a:AssemblyData) -> a.Info.Location)
          let runtimeAssemblies =
              knownAssemblies.Value
              |> List.filter (fun a -> not a.IsReferenceAssembly)
              |> List.map (fun a -> a.Info)
          let newAdditionalArgs =
              { context.Config.CompileOptions.FsiOptions with
                  NoFramework = true
                  References = references @ context.Config.CompileOptions.FsiOptions.References
                  Debug = Some Yaaf.FSharp.Scripting.DebugMode.Portable }
          { context with
              Config =
                { context.Config with
                    CompileOptions = 
                      { context.Config.CompileOptions with 
                         FsiOptions = newAdditionalArgs
                         RuntimeDependencies = runtimeAssemblies @ context.Config.CompileOptions.RuntimeDependencies }
                }
          },
          let assemblyPath, warningsFile = context.CachedAssemblyFilePath + ".dll", context.CachedAssemblyFilePath + ".warnings"
          if File.Exists (assemblyPath) && File.Exists (warningsFile) then
              Some { CompiledAssembly = assemblyPath; Warnings = File.ReadAllText(warningsFile) }
          else None
      member x.SaveCache (context, cache) =
          if logLevel.PrintVerbose then Trace.log "saving cache..."
          File.WriteAllText (context.CachedAssemblyFilePath + ".warnings", cache.Warnings)
          if writeIntellisenseTask.IsValueCreated then 
            writeIntellisenseTask.Value.Wait() }

let restoreDependencies script logLevel cacheDir section =
  match section with
  | PaketDependencies (paketDependencies, paketDependenciesFile, group) ->
    paketCachingProvider script logLevel cacheDir paketDependencies paketDependenciesFile group

let tryFindGroupFromDepsFile scriptDir =
    let depsFile = Path.Combine(scriptDir, "paket.dependencies")
    if File.Exists (depsFile) then
        match
            File.ReadAllLines(depsFile)
            |> Seq.map (fun l -> l.Trim())
            |> Seq.fold (fun (takeNext, result) l ->
                // find '// [ FAKE GROUP ]' and take the next one.
                match takeNext, result with
                | _, Some s -> takeNext, Some s
                | true, None ->
                    if not (l.ToLowerInvariant().StartsWith "group") then
                        Trace.traceFAKE "Expected a group after '// [ FAKE GROUP]' comment, but got %s" l
                        false, None
                    else
                        let splits = l.Split([|" "|], StringSplitOptions.RemoveEmptyEntries)
                        if splits.Length < 2 then
                            Trace.traceFAKE "Expected a group name after '// [ FAKE GROUP]' comment, but got %s" l
                            false, None
                        else
                            false, Some (splits.[1])
                | _ -> if l.Contains "// [ FAKE GROUP ]" then true, None else false, None) (false, None)
            |> snd with
        | Some group ->
            let fullpath = Path.GetFullPath depsFile
            PaketDependencies (Dependencies fullpath, (lazy DependenciesFile.ReadFromFile fullpath), Some group)
            |> Some
        | _ -> None
    else None

let prepareFakeScript (tokenized:Lazy<Fake.Runtime.FSharpParser.TokenizedScript>) logLevel script =
    // read dependencies from the top
    let scriptDir = Path.GetDirectoryName (script)
    let cacheDir = Path.Combine(scriptDir, ".fake", Path.GetFileName(script))
    Directory.CreateDirectory (cacheDir) |> ignore
    
    let scriptSectionHashFile = Path.Combine(cacheDir, "fake-section.cached")
    let scriptSectionCacheFile = Path.Combine(cacheDir, "fake-section.txt")
    let inline getSectionUncached () =
        use __ = Fake.Profile.startCategory Fake.Profile.Category.Analyzing
        let newSection = tryReadPaketDependenciesFromScript tokenized.Value cacheDir script
        match newSection with
        | Some s -> Some s
        | None ->
          tryFindGroupFromDepsFile scriptDir
    let writeToCache (section : FakeSection option) =
        match section with 
        | Some (PaketDependencies(p, _, group)) ->
            sprintf "paket: %s, %s" p.DependenciesFile (match group with | Some g -> g | _ -> "<null>")
        | None -> "none"
        |> fun t -> File.WriteAllText(scriptSectionCacheFile, t)
        File.Copy (script, scriptSectionHashFile)
        
    let readFromCache () =
        let t = File.ReadAllText(scriptSectionCacheFile).Trim()
        if t.StartsWith("paket:") then 
            let s = t.Substring("paket: ".Length)
            let splits = s.Split(',')
            let depsFile = splits.[0]
            let group =
                let trimmed = splits.[1].Trim()
                if trimmed = "<null>" then None else Some trimmed
            let fullpath = Path.GetFullPath depsFile
            PaketDependencies (Dependencies fullpath, (lazy DependenciesFile.ReadFromFile fullpath), group) |> Some
        else None
        
    let section =
        let inline getUncached () =
            let section = getSectionUncached()
            writeToCache section
            section
            
        if File.Exists scriptSectionHashFile && File.Exists scriptSectionCacheFile && File.ReadAllText scriptSectionHashFile = File.ReadAllText script then 
            // get assembly list from cache
            try readFromCache()
            with e ->
                eprintfn "Caching fake section failed: %O" e
                getUncached()
        else
            getUncached()

    match section with
    | Some section ->
        restoreDependencies script logLevel cacheDir section
    | None ->
        let defaultPaketCode = """
source https://api.nuget.org/v3/index.json
storage: none
framework: netstandard2.0
nuget FSharp.Core
        """
        if Environment.environVar "FAKE_ALLOW_NO_DEPENDENCIES" <> "true" then
          Trace.traceFAKE """Consider adding your dependencies via `#r` dependencies, for example add '#r "paket: nuget FSharp.Core //"'.
See https://fake.build/fake-fake5-modules.html for details. 
If you know what you are doing you can silence this warning by setting the environment variable 'FAKE_ALLOW_NO_DEPENDENCIES' to 'true'"""
        let section =
          { Header = "paket-inline"
            Section = defaultPaketCode }
          |> writeFixedPaketDependencies cacheDir        
        restoreDependencies script logLevel cacheDir section

let prepareAndRunScriptRedirect (logLevel:Trace.VerboseLevel) (fsiOptions:string list) scriptPath scriptArgs onErrMsg onOutMsg useCache =

  if logLevel.PrintVerbose then Trace.log (sprintf "prepareAndRunScriptRedirect(Script: %s, fsiOptions: %A)" scriptPath (System.String.Join(" ", fsiOptions)))
  let fsiOptionsObj = Yaaf.FSharp.Scripting.FsiOptions.ofArgs fsiOptions
  let newFsiOptions =
    { fsiOptionsObj with
#if !NETSTANDARD1_6
        Defines = "FAKE" :: fsiOptionsObj.Defines
#else
        Defines = "DOTNETCORE" :: "FAKE" :: fsiOptionsObj.Defines
#endif
      }
  use out = Yaaf.FSharp.Scripting.ScriptHost.CreateForwardWriter onOutMsg
  use err = Yaaf.FSharp.Scripting.ScriptHost.CreateForwardWriter onErrMsg
  let tokenized = lazy (File.ReadLines scriptPath |> FSharpParser.getTokenized scriptPath ("FAKE_DEPENDENCIES" :: newFsiOptions.Defines))
  let config =
    { Runners.FakeConfig.VerboseLevel = logLevel
      Runners.FakeConfig.ScriptFilePath = scriptPath
      Runners.FakeConfig.ScriptTokens = tokenized
      Runners.FakeConfig.CompileOptions = 
        { FsiOptions = newFsiOptions; RuntimeDependencies = [] }
      Runners.FakeConfig.UseCache = useCache
      Runners.FakeConfig.Out = out
      Runners.FakeConfig.Err = err
      Runners.FakeConfig.ScriptArgs = scriptArgs }
  let provider = prepareFakeScript tokenized logLevel scriptPath
  CoreCache.runScriptWithCacheProvider config provider

let inline prepareAndRunScript logLevel fsiOptions scriptPath scriptArgs useCache =
  prepareAndRunScriptRedirect logLevel fsiOptions scriptPath scriptArgs (printf "%s") (printf "%s") useCache

