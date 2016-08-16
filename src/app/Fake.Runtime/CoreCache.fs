/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.CoreCache
open Fake.Runtime.Environment
open Fake.Runtime.Trace
open Fake.Runtime.ScriptRunner
open Fake.Runtime.HashGeneration
#if NETSTANDARD1_6
open System.Runtime.Loader
#endif

open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Text.RegularExpressions
open System.Xml.Linq
open Yaaf.FSharp.Scripting
open System.Reflection

type ICachingProvider =
    abstract TryLoadCache : context:FakeContext -> FakeContext * CoreCacheInfo option
    abstract SaveCache : context:FakeContext * cache:CoreCacheInfo -> unit
    abstract CleanCache : context:FakeContext -> unit

module internal Cache =
    let xname name = XName.Get(name)
    let create (loadedAssemblies : Reflection.Assembly seq) =
        let xelement name = XElement(xname name)
        let xattribute name value = XAttribute(xname name, value)

        let doc = XDocument()
        let root = xelement "FAKECache"
        doc.Add(root)
        let assemblies = xelement "Assemblies"
        root.Add(assemblies)

        let assemNodes =
            loadedAssemblies
            |> Seq.map(fun assem ->
                let ele = xelement "Assembly"
                ele.Add(xattribute "Location" assem.Location)
                ele.Add(xattribute "FullName" assem.FullName)
                ele.Add(xattribute "Version" (assem.GetName().Version.ToString()))
                ele)
            |> Seq.iter(assemblies.Add)
        doc

    let read (path : string) = 
        let doc = XDocument.Load(path)
        //let root = doc.Descendants() |> Seq.exactlyOne
        let assembliesEle = doc.Descendants(xname "Assemblies") |> Seq.exactlyOne
        assembliesEle.Descendants()
        |> Seq.map(fun assemblyEle ->
            let get name = assemblyEle.Attribute(xname name).Value
            { Location = get "Location"
              FullName = get "FullName"
              Version = get "Version" })
        |> Seq.toList
        
    let warningsFileName (f:FakeContext) = f.HashPath + "_warnings.txt"
    let cleanFiles filesGen f =
      filesGen
      |> List.map (fun gen -> gen f)
      |> List.filter File.Exists
      |> List.iter File.Delete
    let tryLoadDefault (context:FakeContext) =
      let cachedDll = context.CachedAssemblyFilePath
      if File.Exists cachedDll then
        let warnings = warningsFileName context
        let warningText = File.ReadAllText warnings
        Some { CompiledAssembly = cachedDll; Warnings = warningText }
      else None
    let defaultProvider =
#if !NETSTANDARD1_6
        let xmlFileName (f:FakeContext) = f.HashPath + "_config.xml"
        let cleanFiles = cleanFiles [ warningsFileName; xmlFileName ]
        { new ICachingProvider with
            member __.CleanCache context = cleanFiles context
            member __.TryLoadCache (context) =
                let xmlFile = xmlFileName context
                match tryLoadDefault context with
                | Some config when File.Exists xmlFile ->
                    let readXml = read xmlFile
                    { context with
                        Config =
                          { context.Config with
                              CompileOptions =
                                { context.Config.CompileOptions with
                                    RuntimeDependencies = context.Config.CompileOptions.RuntimeDependencies @ readXml
                                    CompileReferences = context.Config.CompileOptions.CompileReferences @ (readXml |> List.map (fun x -> x.Location))
                                }
                          }
                    }, Some config
                | _ -> context, None
            //member x.GetAssembliesFromCache c = c.Assemblies
            member x.SaveCache (context, cache) =
                let xmlFile = xmlFileName context
                let warnings = warningsFileName context
                let dynamicAssemblies =
                    System.AppDomain.CurrentDomain.GetAssemblies()
                    |> Seq.filter(fun assem -> assem.IsDynamic)
                    |> Seq.map(fun assem -> assem.GetName().Name)
                    |> Seq.filter(fun assem -> assem <> fsiAssemblyName)
                    |> Seq.filter(fun assem -> not <| assem.StartsWith cachedAssemblyPrefix)
                    // General Reflection.Emit helper (most likely harmless to ignore)
                    |> Seq.filter(fun assem -> assem <> "Anonymously Hosted DynamicMethods Assembly")
                    // RazorEngine generated
                    |> Seq.filter(fun assem -> assem <> "RazorEngine.Compilation.ImpromptuInterfaceDynamicAssembly")
                    |> Seq.cache
                if dynamicAssemblies |> Seq.length > 0 then
                    let msg =
                        sprintf "Dynamic assemblies were generated during evaluation of script (%s).\nCan not save cache."
                            (System.String.Join(", ", dynamicAssemblies))
                    trace msg
                else
                    let assemblies =
                        System.AppDomain.CurrentDomain.GetAssemblies()
                        |> Seq.filter(fun assem -> not assem.IsDynamic)
                        |> Seq.filter(fun assem -> not <| assem.GetName().Name.StartsWith cachedAssemblyPrefix)
                        // They are not dynamic, but can't be re-used either.
                        |> Seq.filter(fun assem -> not <| assem.GetName().Name.StartsWith("CompiledRazorTemplates.Dynamic.RazorEngine_"))

                    let cacheConfig : XDocument = create assemblies
                    cacheConfig.Save (xmlFile)
                    File.WriteAllText (warnings, cache.Warnings) }
#else
        let cleanFiles = cleanFiles [ warningsFileName ]
        { new ICachingProvider with
            member __.CleanCache context = cleanFiles context
            member __.TryLoadCache (context) =
                traceFAKE "Default caching is disabled on dotnetcore, see https://github.com/dotnet/coreclr/issues/919#issuecomment-219212910"
                
                let fsiOpts = context.Config.CompileOptions.AdditionalArguments |> FsiOptions.ofArgs
                if not fsiOpts.NoFramework then // Caller should take care!
                    let basePath = System.AppContext.BaseDirectory
                    let references =
                        System.IO.Directory.GetFiles(basePath, "*.dll")
                        |> Seq.filter (fun r -> not (System.IO.Path.GetFileName(r).ToLowerInvariant().StartsWith("api-ms")))
                        |> Seq.choose (fun r ->
                            try Some (AssemblyInfo.ofLocation r)
                            with e -> None)
                        |> Seq.toList
                    let newAdditionalArgs =
                        { fsiOpts with
                            NoFramework = true
                            Debug = Some DebugMode.Portable }
                        |> (fun options -> options.AsArgs)
                        |> Seq.toList
                    { context with
                        Config =
                          { context.Config with
                              CompileOptions =
                                { context.Config.CompileOptions with
                                    AdditionalArguments = newAdditionalArgs
                                    RuntimeDependencies = references @ context.Config.CompileOptions.RuntimeDependencies
                                    CompileReferences =
                                        (references |> List.map (fun r -> r.Location)) @ context.Config.CompileOptions.CompileReferences
                                }
                          }
                    }, None
                else context, None
            member x.SaveCache (context, cache) = () }
#endif

let fakeDirectoryName = ".fake"

let prepareContext (config:FakeConfig) (cache:ICachingProvider) =
    let fsiOptions = FsiOptions.ofArgs (config.CompileOptions.AdditionalArguments)
    let newFsiOptions =
      { fsiOptions with
#if !NETSTANDARD1_6
          Defines = "FAKE" :: fsiOptions.Defines 
#else
          Defines = "DOTNETCORE" :: "FAKE" :: fsiOptions.Defines 
#endif
      }
    let config = 
      { config with 
          FakeConfig.CompileOptions = 
            { config.CompileOptions with
                AdditionalArguments = newFsiOptions.AsArgs |> Array.toList } }
    let allScriptContents = getAllScripts newFsiOptions.Defines config.ScriptFilePath
    let getOpts (c:ScriptCompileOptions) = c.AdditionalArguments @ c.CompileReferences
    let scriptHash = getScriptHash allScriptContents (getOpts config.CompileOptions)
    //TODO this is only calculating the hash for the input file, not anything #load-ed
    let fakeDir = Path.Combine(Path.GetDirectoryName config.ScriptFilePath, fakeDirectoryName)
    let context =
      { FakeContext.Config = config
        FakeDirectory = fakeDir
        Hash = scriptHash }
    cache.TryLoadCache context




#if !NETSTANDARD1_6
type AssemblyLoadContext () =
  member x.LoadFromAssemblyPath (loc:string) =
    Reflection.Assembly.LoadFrom(loc)
  member x.LoadFromAssemblyName(fullname:AssemblyName)= 
    Reflection.Assembly.Load(fullname)
#endif

let loadAssembly (loadContext:AssemblyLoadContext) printDetails (assemInfo:AssemblyInfo) =
    try let assem =
            if assemInfo.Location <> "" then
                try
                    Some assemInfo.Location, loadContext.LoadFromAssemblyPath(assemInfo.Location)
                with :? FileLoadException as e ->
                    if printDetails then
                        Trace.tracefn "Error while loading assembly: %O" e
                    let assemblyName = new System.Reflection.AssemblyName(assemInfo.FullName)
                    let asem = System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(assemblyName.Name))
                    if printDetails then
                        Trace.traceFAKE "recovered and used already loaded assembly '%s' instead of '%s' ('%s')" asem.FullName assemInfo.FullName assemInfo.Location
                    None, asem
            else None, loadContext.LoadFromAssemblyName(new AssemblyName(assemInfo.FullName))
        Some(assem)
    with ex ->
        if printDetails then tracef "Unable to find assembly %A. (Error: %O)" assemInfo ex
        None

let findAndLoadInRuntimeDeps (loadContext:AssemblyLoadContext) (name:AssemblyName) printDetails (runtimeDependencies:AssemblyInfo list) =
    let strName = name.FullName
    if printDetails then tracefn "Trying to resolve: %s" strName
    let getAssemblyFromType (t:System.Type) =
#if NETSTANDARD1_6
      t.GetTypeInfo().Assembly
#else
      t.Assembly
#endif
    
    // These guys need to be handled carefully, they must only exist a single time in memory
    let wellKnownAssemblies = 
      [ getAssemblyFromType typeof<Fake.Core.Context.FakeExecutionContext> ]

    let isPerfectMatch, result =
      match wellKnownAssemblies |> List.tryFind (fun a -> a.GetName().Name = name.Name) with
      | Some a ->
        a.FullName = strName, (Some (None, a))
      | None ->
        match runtimeDependencies |> List.tryFind (fun r -> r.FullName = strName) with
        | Some a ->
            true, loadAssembly loadContext printDetails a
        | _ ->
            let token = name.GetPublicKeyToken()
            match runtimeDependencies
                  |> Seq.map (fun r -> AssemblyName(r.FullName), r)
                  |> Seq.tryFind (fun (n, _) ->
                      n.Name = name.Name &&
                      (isNull token || // When null accept what we have.
                          n.GetPublicKeyToken() = token)) with
            | Some (_, info) ->
                false, loadAssembly loadContext printDetails info
            | _ ->
                false, None
    match result with
    | Some (location, a) ->
        if isPerfectMatch then
            if printDetails then tracefn "Redirect assembly load to known assembly: %s (%A)" strName location
        else
            traceFAKE "Redirect assembly from '%s' to '%s' (%A)" strName a.FullName location
        a
    | _ ->
        if printDetails then tracefn "Could not resolve: %s" strName
        null

#if NETSTANDARD1_6
// See https://github.com/dotnet/coreclr/issues/6411
type FakeLoadContext (printDetails:bool, dependencies:AssemblyInfo list) =
  inherit AssemblyLoadContext()
  let basePath = System.AppContext.BaseDirectory
  let references =
      System.IO.Directory.GetFiles(basePath, "*.dll")
      |> Seq.filter (fun r -> not (System.IO.Path.GetFileName(r).ToLowerInvariant().StartsWith("api-ms")))
      |> Seq.choose (fun r ->
          try Some (AssemblyInfo.ofLocation r)
          with e -> None)
      |> Seq.toList
  let allReferences = references @ dependencies
  override x.Load(assem:AssemblyName) =
       findAndLoadInRuntimeDeps x assem printDetails allReferences
#endif

let setupAssemblyResolver (context:FakeContext) =
    
#if NETSTANDARD1_6
    let globalLoadContext = AssemblyLoadContext.Default
    // See https://github.com/dotnet/coreclr/issues/6411
    let fakeLoadContext = new FakeLoadContext(context.Config.PrintDetails, context.Config.CompileOptions.RuntimeDependencies)
#else
    let loadContext = new AssemblyLoadContext()
#endif



#if NETSTANDARD1_6
    globalLoadContext.add_Resolving(new Func<AssemblyLoadContext, AssemblyName, Assembly>(fun _ name ->
        let strName = name.FullName
        fakeLoadContext.LoadFromAssemblyName(name)
#else
    AppDomain.CurrentDomain.add_AssemblyResolve(new ResolveEventHandler(fun _ ev ->
        let strName = ev.Name
        let name = AssemblyName(strName)
        findAndLoadInRuntimeDeps loadContext name context.Config.PrintDetails context.Config.CompileOptions.RuntimeDependencies
#endif
        ))

let runScriptWithCacheProvider (config:FakeConfig) (cache:ICachingProvider) =
    let newContext, cacheInfo =  prepareContext config cache

    setupAssemblyResolver newContext

    // Add arguments to the Environment
    for (k,v) in config.Environment do
      setEnvironVar k v

    // Create an env var that only contains the build script args part from the --fsiargs (or "").
    setEnvironVar "fsiargs-buildscriptargs" (String.Join(" ", config.CompileOptions.AdditionalArguments))

    let resultCache, result = runFakeScript cacheInfo newContext

    match result with
    | Some err ->
        traceFAKE "%O" err
    | _ -> ()

    if resultCache.Warnings <> "" then
        traceFAKE "%O" resultCache.Warnings
    
    match resultCache.AsCacheInfo with
    | Some newCache ->
        cache.SaveCache(newContext, newCache)
    | _ -> ()

    // Return if the script suceeded
    result.IsNone
