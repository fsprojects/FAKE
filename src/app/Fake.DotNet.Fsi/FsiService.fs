module Fake.DotNet.Fsi.Service

open System
open System.Text.RegularExpressions
open System.Diagnostics
open System.IO
open System.Xml.Linq
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.DotNet.Fsi

open Microsoft.FSharp.Compiler.Interactive.Shell
open System.Reflection

let hashRegex = Text.RegularExpressions.Regex("(?<script>.+)_(?<hash>[a-zA-Z0-9]+)(\.dll|_config\.xml|_warnings\.txt)$", System.Text.RegularExpressions.RegexOptions.Compiled)

let createDirectiveRegex id =
    Regex("^\s*#" + id + "\s*(@\"|\"\"\"|\")(?<path>.+?)(\"\"\"|\")", RegexOptions.Compiled ||| RegexOptions.Multiline)

let loadRegex = createDirectiveRegex "load"
let rAssemblyRegex = createDirectiveRegex "r"
let searchPathRegex = createDirectiveRegex "I"

let private extractDirectives (regex : Regex) scriptContents =
    regex.Matches scriptContents
    |> Seq.cast<Match>
    |> Seq.map(fun m -> m.Groups.Item("path").Value)

type Script = {
    Content : string
    Location : string
    SearchPaths : string seq
    IncludedAssemblies : Lazy<string seq>
}

let getAllScriptContents (pathsAndContents : seq<Script>) =
    pathsAndContents |> Seq.map(fun s -> s.Content)

let getIncludedAssembly scriptContents = extractDirectives rAssemblyRegex scriptContents

let getSearchPaths scriptContents = extractDirectives searchPathRegex scriptContents

let rec getAllScripts scriptPath : seq<Script> =
    let scriptContents = File.ReadAllText scriptPath
    let searchPaths = getSearchPaths scriptContents |> Seq.toList

    let loadedContents =
        extractDirectives loadRegex scriptContents
        |> Seq.collect (fun path ->
            if path.StartsWith ".fake" then
                Seq.empty
            else
                let path =
                    if Path.IsPathRooted path then
                        path
                    else
                        let pathMaybe =
                            ["./"] @ searchPaths
                            |> List.map(fun searchPath ->
                                if Path.IsPathRooted searchPath then
                                    Path.Combine(searchPath, path)
                                else
                                    Path.Combine(Path.GetDirectoryName scriptPath, searchPath, path))
                            |> List.tryFind File.Exists

                        match pathMaybe with
                        | None -> failwithf "Could not find script '%s' in any paths searched. Searched paths:\n%A" path searchPaths
                        | Some x -> x
                getAllScripts path
        )
    let s =
      { Location = scriptPath
        Content = scriptContents
        SearchPaths = searchPaths
        IncludedAssemblies = lazy(getIncludedAssembly scriptContents) }
    Seq.concat [List.toSeq [s]; loadedContents]

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

    type AssemblyInfo = {
        Location : string
        FullName : string
        Version : string
    }

    type CacheConfig = {
        Assemblies : AssemblyInfo seq
    }
    let read (path : string) : CacheConfig =
        let doc = XDocument.Load(path)
        //let root = doc.Descendants() |> Seq.exactlyOne
        let assembliesEle = doc.Descendants(xname "Assemblies") |> Seq.exactlyOne
        let assemblies =
            assembliesEle.Descendants()
            |> Seq.map(fun assemblyEle ->
                let get name = assemblyEle.Attribute(xname name).Value
                { Location = get "Location"
                  FullName = get "FullName"
                  Version = get "Version" })
        { Assemblies = assemblies }

type private CacheInfo =
  {
    ScriptFileName : string
    ScriptFilePath : string
    ScriptHash : string
    AssemblyPath : string
    AssemblyWarningsPath : string
    CacheConfigPath : string
    CacheConfig : Lazy<Cache.CacheConfig>
    IsValid : bool
  }

let getScriptHash pathsAndContents fsiOptions =
    let fullContents = getAllScriptContents pathsAndContents |> String.concat "\n"
    let fsiOptions = fsiOptions |> String.concat "\n"
    let paths = pathsAndContents |> Seq.map(fun x -> x.Location |> Path.normalizeFileName) |> String.concat "\n"

    let hasher = HashLib.HashFactory.Checksum.CreateCRC32a()
    hasher.ComputeString(fullContents + paths + fsiOptions).ToString()

/// gets a cache entry for the given script.
/// We need to consider fsiOptions as they might contain --defines.
let private getCacheInfoFromScript printDetails fsiOptions scriptPath =
    let allScriptContents = getAllScripts scriptPath
    let scriptHash = getScriptHash allScriptContents fsiOptions
    //TODO this is only calculating the hash for the input file, not anything #load-ed

    let scriptFileName = Path.GetFileName(scriptPath)
    let hashPath = (Path.GetDirectoryName scriptPath) + "/.fake/" + scriptFileName + "_" + scriptHash
    let assemblyPath = hashPath + ".dll"
    let assemblyWarningsPath = hashPath + "_warnings.txt"
    let cacheConfigPath = hashPath + "_config.xml"
    let cacheConfig = lazy Cache.read cacheConfigPath
    let cacheValid =
        let cacheFilesExistAndAreValid =
            File.Exists(assemblyPath) &&
            File.Exists(cacheConfigPath) &&
            File.Exists(assemblyWarningsPath) &&
            cacheConfig.Value.Assemblies |> Seq.length > 0
        if cacheFilesExistAndAreValid then
            let currentlyLoaded =
                AppDomain.CurrentDomain.GetAssemblies()
                |> Seq.map (fun a -> let n = a.GetName() in n.Name, a)
                |> Seq.groupBy fst
                |> Seq.map (fun (name, group) -> name, group |> Seq.sortByDescending (fun (_, a) -> a.GetName().Version) |> Seq.head |> snd)
                |> dict
            let loadedAssemblies =
                cacheConfig.Value.Assemblies
                |> Seq.choose (fun assemInfo ->
                    try let name = AssemblyName(assemInfo.FullName)
                        match currentlyLoaded.TryGetValue name.Name with
                        | true, assem -> Some (assemInfo, assem)
                        | _ ->
                            let assem =
                                if assemInfo.Location <> "" then
                                    Reflection.Assembly.LoadFrom(assemInfo.Location)
                                else Reflection.Assembly.Load(assemInfo.FullName)
                            Some(assemInfo, assem)
                    with ex -> if printDetails then Trace.tracef "Unable to find assembly %A" assemInfo
                               None)
                |> Seq.toList
            let knownAssemblies =
                loadedAssemblies
                |> List.map (fun (assemInfo, assem) -> assemInfo.FullName, assem)
                |> dict
            let assemVersionValidCount =
                loadedAssemblies
                |> Seq.filter(fun (assemInfo, assem) ->
                    assem.GetName().Version.ToString() = assemInfo.Version)
                |> Seq.length
            AppDomain.CurrentDomain.add_AssemblyResolve(new ResolveEventHandler(fun _ ev ->
                let name = AssemblyName(ev.Name)
                match knownAssemblies.TryGetValue(ev.Name) with
                | true, a ->
                    if printDetails then Trace.tracefn "Redirect assembly load to known assembly: %s" ev.Name
                    a
                | _ ->
                    let token = name.GetPublicKeyToken()
                    match loadedAssemblies
                          |> Seq.map snd
                          |> Seq.tryFind (fun asem ->
                              let n = asem.GetName()
                              n.Name = name.Name &&
                              (isNull token || // When null accept what we have.
                                n.GetPublicKeyToken() = token)) with
                    | Some (asem) ->
                        Trace.traceFAKE "Redirect assembly from '%s' to '%s'" ev.Name asem.FullName
                        asem
                    | _ ->
                        if not (ev.Name.StartsWith("FSharp.Compiler.Service.resources"))
                        && not (ev.Name.StartsWith("FSharp.Compiler.Service.MSBuild")) then
                            if printDetails then Trace.traceFAKE "Could not resolve '%s'" ev.Name
                        null))
            assemVersionValidCount = Seq.length cacheConfig.Value.Assemblies
        else
            false
    { ScriptFileName = scriptFileName
      ScriptFilePath = scriptPath
      ScriptHash = scriptHash
      AssemblyPath = assemblyPath
      AssemblyWarningsPath = assemblyWarningsPath
      CacheConfigPath = cacheConfigPath
      CacheConfig = cacheConfig
      IsValid = cacheValid }

/// because it is used by test code
let nameParser scriptFileName =
    let noExtension = Path.GetFileNameWithoutExtension(scriptFileName)
    let startString = "<StartupCode$FSI_"
    let endString =
      sprintf "_%s%s$%s"
        (noExtension.Substring(0, 1).ToUpper())
        (noExtension.Substring(1))
        (Path.GetExtension(scriptFileName).Substring(1))
    let fullName i = sprintf "%s%s>.$FSI_%s%s" startString i i endString
    let exampleName = fullName "0001"
    let parseName (n:string) =
        if n.Length >= exampleName.Length &&
            n.Substring(0, startString.Length) = startString &&
            n.Substring(n.Length - endString.Length) = endString then
            let num = n.Substring(startString.Length, 4)
            assert (fullName num = n)
            Some (num)
        else None
    exampleName, fullName, parseName

/// Run a script from the cache
let private runScriptCached printDetails cacheInfo out err =
    if printDetails then Trace.trace "Using cache"
    let exampleName, fullName, parseName = nameParser cacheInfo.ScriptFileName
    try
        use execContext = Fake.Core.Context.FakeExecutionContext.Create true cacheInfo.ScriptFilePath []
        Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
        Yaaf.FSharp.Scripting.Helper.consoleCapture out err (fun () ->
            let ass = Reflection.Assembly.LoadFrom(cacheInfo.AssemblyPath)
            match ass.GetTypes()
                  |> Seq.filter (fun t -> parseName t.FullName |> Option.isSome)
                  |> Seq.map (fun t -> t.GetMethod("main@", BindingFlags.InvokeMethod ||| BindingFlags.Public ||| BindingFlags.Static))
                  |> Seq.filter (isNull >> not)
                  |> Seq.tryHead with
            | Some mainMethod ->
              try mainMethod.Invoke(null, [||])
                  |> ignore
                  true
              with
              | ex ->
                  Trace.TraceError (ex.ToString())
                  false
            | None -> failwithf "We could not find a type similar to '%s' containing a 'main@' method in the cached assembly (%s)!" exampleName cacheInfo.AssemblyPath)
    finally
        try
            Trace.traceFAKE "%s" (File.ReadAllText cacheInfo.AssemblyWarningsPath)
        with e -> Trace.traceError (e.ToString())

/// Handles a cache store operation, this should not throw as it is executed in a finally block and
/// therefore might eat other exceptions. And a caching error is not critical.
let private handleCaching printDetails (session:IFsiSession) fsiErrorOutput (cacheDir:DirectoryInfo) cacheInfo  =
    try
        let wishName = "FAKE_CACHE_" + Path.GetFileNameWithoutExtension cacheInfo.ScriptFileName + "_" + cacheInfo.ScriptHash
        let d = session.DynamicAssemblyBuilder
        let name = "FSI-ASSEMBLY"
        d.Save(name + ".dll")
        if not <| Directory.Exists cacheDir.FullName then
            let di = Directory.CreateDirectory cacheDir.FullName
            di.Attributes <- FileAttributes.Directory ||| FileAttributes.Hidden

        let destinationFile = FileInfo(cacheInfo.AssemblyPath)
        let targetDirectory = destinationFile.Directory

        if (not <| targetDirectory.Exists) then targetDirectory.Create()
        if (destinationFile.Exists) then destinationFile.Delete()

        File.WriteAllText(cacheInfo.AssemblyWarningsPath, fsiErrorOutput.ToString())

        try
            // Now we change the AssemblyName of the written Assembly via Mono.Cecil.
            // Strictly speaking this is not needed, however this helps with executing
            // the test suite, as the runtime will only load a single
            // FSI-ASSEMBLY with version 0.0.0.0 by using LoadFrom...
            let reader = new Mono.Cecil.DefaultAssemblyResolver() // see https://github.com/fsharp/FAKE/issues/1084
            reader.AddSearchDirectory (Path.GetDirectoryName TraceHelper.fakePath)
            reader.AddSearchDirectory (Path.GetDirectoryName typeof<string option>.Assembly.Location)
            let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
            ( use asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(name + ".dll", readerParams)
              asem.Name <- new Mono.Cecil.AssemblyNameDefinition(wishName, new Version(0,0,1))
              asem.Write(wishName + ".dll"))
            File.Move(wishName + ".dll", cacheInfo.AssemblyPath)
        with exn ->
            // If cecil fails we might want to trigger a warning, but you know what?
            // we can continue using the FSI-ASSEMBLY.dll
            Trace.traceFAKE "Warning (please open an issue on FAKE and /cc @matthid): %O" exn
            File.Move(name + ".dll", cacheInfo.AssemblyPath)

        for name in [ name; wishName ] do
            for ext in [ ".dll"; ".pdb"; ".dll.mdb" ] do
                if File.Exists(name + ext) then
                    try
                        File.Delete(name + ext)
                    with e ->
                        if printDetails then
                            printfn "Could not delete '%s' check whats wrong: %O" (name + ext) e

        let dynamicAssemblies =
            System.AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.filter(fun assem -> assem.IsDynamic)
            |> Seq.map(fun assem -> assem.GetName().Name)
            |> Seq.filter(fun assem -> assem <> "FSI-ASSEMBLY")
            |> Seq.filter(fun assem -> not <| assem.StartsWith "FAKE_CACHE_")
            // General Reflection.Emit helper (most likely harmless to ignore)
            |> Seq.filter(fun assem -> assem <> "Anonymously Hosted DynamicMethods Assembly")
            // RazorEngine generated
            |> Seq.filter(fun assem -> assem <> "RazorEngine.Compilation.ImpromptuInterfaceDynamicAssembly")
            |> Seq.cache
        if dynamicAssemblies |> Seq.length > 0 then
            let msg =
                sprintf "Dynamic assemblies were generated during evaluation of script (%s).\nCan not save cache."
                    (System.String.Join(", ", dynamicAssemblies))
            Trace.trace msg

        else
            let assemblies =
                System.AppDomain.CurrentDomain.GetAssemblies()
                |> Seq.filter(fun assem -> not assem.IsDynamic)
                |> Seq.filter(fun assem -> not <| assem.GetName().Name.StartsWith "FAKE_CACHE_")
                // They are not dynamic, but can't be re-used either.
                |> Seq.filter(fun assem -> not <| assem.GetName().Name.StartsWith("CompiledRazorTemplates.Dynamic.RazorEngine_"))

            let cacheConfig : XDocument = Cache.create assemblies
            cacheConfig.Save(cacheInfo.CacheConfigPath)
            if printDetails then Trace.trace (System.Environment.NewLine + "Saved cache")
    with ex ->
        // Caching errors are not critical, and we shouldn't throw in a finally clause.
        Trace.traceFAKE "CACHING ERROR - please open a issue on FAKE and /cc @matthid\n\nError: %O" ex
        if File.Exists cacheInfo.AssemblyWarningsPath then
            // Invalidates the cache
            try File.Delete cacheInfo.AssemblyWarningsPath with _ -> ()

/// Run a given script unchacked, saves the cache if useCache is set to true.
/// deletes any existing caching for the given script.
let private runScriptUncached (useCache, fsiOptions) printDetails (cacheInfo:CacheInfo) out err =
    let options = FsiOptions.ofArgs fsiOptions
#if DEBUG
    let options = { options with Debug = Some DebugMode.Full }
#endif

    let getScriptAndHash fileName =
        let matched = hashRegex.Match(fileName)
        matched.Groups.Item("script").Value, matched.Groups.Item("hash").Value
    let cacheDir = DirectoryInfo(Path.Combine(Path.GetDirectoryName(cacheInfo.ScriptFilePath),".fake"))
    if useCache then
        // If we are here that proably means that
        // when trying to load the cached version something went wrong...
        if cacheDir.Exists then
            let oldFiles =
                cacheDir.GetFiles()
                |> Seq.filter(fun file ->
                    let oldScriptName, _ = getScriptAndHash(file.Name)
                    oldScriptName = cacheInfo.ScriptFileName)
                |> Seq.toList

            if not <| List.isEmpty oldFiles then
                if printDetails then Trace.trace "Cache is invalid, recompiling"
                oldFiles
                |> List.map (fun file ->
                    try file.Delete(); true
                    // File might be locked (for example when executing the test suite!)
                    with :? UnauthorizedAccessException ->
                        Trace.traceFAKE "Unable to access %s" file.FullName
                        false)
                |> List.exists id |> not
                // we could not delete a single file -> cache was not invalidated
                |> function
                    | true ->
                        Trace.traceError (sprintf "Unable to invalidate cache for '%s', please delete the .fake folder!" cacheInfo.ScriptFileName)
                    | _ -> ()
            else
                if printDetails then Trace.trace "Cache doesn't exist"
        else
            if printDetails then Trace.trace "Cache doesn't exist"

    // Contains warnings and errors about the build script.
    let doTrace = Environment.environVar "FAKE_TRACE" = "true"
    if printDetails && doTrace then
        // "Debug" is for FCS debugging, use a debug build to get more output...
        Debug.AutoFlush <- true
        let logToConsole = true
        let logToFile = true
        try
          let allTraceOptions =
            TraceOptions.Callstack ||| TraceOptions.DateTime ||| TraceOptions.LogicalOperationStack |||
            TraceOptions.ProcessId ||| TraceOptions.ThreadId ||| TraceOptions.Timestamp
          let noTraceOptions = TraceOptions.None
          let svclogFile = "FAKE.svclog"
          System.Diagnostics.Trace.AutoFlush <- true

          let setupListener traceOptions levels (listener:TraceListener) =
            [ Yaaf.FSharp.Scripting.Log.source ]
            |> Seq.iter (fun source ->
                source.Switch.Level <- System.Diagnostics.SourceLevels.All
                source.Listeners.Add listener |> ignore)
            listener.Filter <- new EventTypeFilter(levels)
            listener.TraceOutputOptions <- traceOptions
            Debug.Listeners.Add(listener) |> ignore

          if logToConsole then
            new ConsoleTraceListener()
            |> setupListener noTraceOptions System.Diagnostics.SourceLevels.Verbose

          if logToFile then
            if System.IO.File.Exists svclogFile then System.IO.File.Delete svclogFile
            new XmlWriterTraceListener(svclogFile)
            |> setupListener allTraceOptions System.Diagnostics.SourceLevels.All

          // Test that everything works
          Yaaf.FSharp.Scripting.Log.infof "Yaaf.FSharp.Scripting Logging setup!"
        with e ->
          printfn "Yaaf.FSharp.Scripting Logging setup failed: %A" e
    let fsiErrorOutput = new System.Text.StringBuilder()
    let session =
      try ScriptHost.Create
            (options, preventStdOut = true,
              reportGlobal = doTrace,
              fsiErrWriter = ScriptHost.CreateForwardWriter
                ((fun s ->
                    if String.IsNullOrWhiteSpace s |> not then
                        fsiErrorOutput.AppendLine s |> ignore),
                  removeNewLines = true),
              outWriter = out,
              errWriter = err)
      with :? FsiEvaluationException as e ->
          Trace.traceError "FsiEvaluationSession could not be created."
          Trace.traceError e.Result.Error.Merged
          reraise ()

    try
        try
            use execContext = Fake.Core.Context.FakeExecutionContext.Create false cacheInfo.ScriptFilePath []
            Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
            session.EvalScript cacheInfo.ScriptFilePath
            true
        with :? FsiEvaluationException as eval ->
            Trace.traceFAKE "%O" eval
            false
    finally
        // Write Script Warnings & Errors at the end
        let strFsiErrorOutput = fsiErrorOutput.ToString()
        if strFsiErrorOutput <> "" then
            Trace.traceFAKE "%O" strFsiErrorOutput
        // Cache in the error case as well.
        if useCache && not cacheInfo.IsValid then
            // See https://github.com/fsharp/FAKE/pull/1534
            let doCaching =
                match Environment.monoVersion with
                | None -> true
                | Some (display, version) ->
                    match version with
                    | Some v ->
                        if v.Major = 5 && v.Minor = 0 && v.Build = 0 then
                            Trace.traceFAKE "We don't try to cache, see https://github.com/fsharp/FAKE/pull/1534"
                            false
                        else true
                    | None ->
                        Trace.traceFAKE "Couldn't extract mono version from '%s', please report this as issue" display
                        true
            if doCaching then
                try
                    handleCaching printDetails session fsiErrorOutput cacheDir cacheInfo
                with e ->
                    // See https://github.com/fsharp/FAKE/pull/1534
                    Trace.traceFAKE "Error in FAKE-Caching (might be a bug in the runtime, use the no-cache option to get rid of this warning): %O" e

/// Run the given FAKE script with fsi.exe at the given working directory. Provides full access to Fsi options and args. Redirect output and error messages.
let internal runFAKEScriptWithFsiArgsAndRedirectMessages printDetails (FsiArgs(fsiOptions, scriptPath, scriptArgs)) env onErrMsg onOutMsg useCache =
    if printDetails then Trace.traceFAKE "Running Buildscript: %s" scriptPath

    if printDetails then
      System.AppDomain.CurrentDomain.add_AssemblyResolve(
        new System.ResolveEventHandler(fun _ e ->
            if not (e.Name.StartsWith("FSharp.Compiler.Service.resources"))
            && not (e.Name.StartsWith("FSharp.Compiler.Service.MSBuild")) then
                Trace.trace <| sprintf "FAKE: Trying to resolve %s" e.Name
            null))

    // Add arguments to the Environment
    for (k,v) in env do
      Environment.SetEnvironmentVariable(k, v, EnvironmentVariableTarget.Process)

    // Create an env var that only contains the build script args part from the --fsiargs (or "").
    Environment.SetEnvironmentVariable("fsiargs-buildscriptargs", String.Join(" ", scriptArgs))

    let scriptPath =
        if Path.IsPathRooted scriptPath then
            scriptPath
        else
            Path.Combine(Directory.GetCurrentDirectory(), scriptPath)

    let cacheInfo = getCacheInfoFromScript printDetails fsiOptions scriptPath

    use out = ScriptHost.CreateForwardWriter onOutMsg
    use err = ScriptHost.CreateForwardWriter onErrMsg
    if useCache && cacheInfo.IsValid then
        try
            runScriptCached printDetails cacheInfo out err
        with ex ->
            Trace.traceFAKE """CACHING WARNING
this might happen after Updates...
please open a issue on FAKE and /cc @matthid ONLY IF this happens reproducibly)

Error: %O""" ex
            // Invalidates the cache
            runScriptUncached (useCache, fsiOptions) printDetails cacheInfo out err
    else
        runScriptUncached (useCache, fsiOptions) printDetails cacheInfo out err

let internal onMessage isError =
    let printer = if isError && CoreTracing.importantMessagesToStdErr then eprintf else printf
    printer "%s"

/// Run the given buildscript with fsi.exe and allows for extra arguments to the script. Returns output.
let executeBuildScriptWithArgsAndFsiArgsAndReturnMessages script (scriptArgs: string[]) (fsiArgs:string[]) useCache =

    let messages = ref []
    let appendMessage isError msg =
        onMessage isError msg // For the tests to be more realistic
        messages := { IsError = isError
                      Message = msg
                      Timestamp = DateTimeOffset.UtcNow } :: !messages
    let result =
        runFAKEScriptWithFsiArgsAndRedirectMessages
            true (FsiArgs(fsiArgs |> List.ofArray, script, scriptArgs |> List.ofArray)) []
            (appendMessage true) (appendMessage false) useCache
    (result, !messages |> List.rev)

/// Run the given buildscript with fsi.exe and allows for extra arguments to the script. Returns output.
let executeBuildScriptWithArgsAndReturnMessages script (scriptArgs: string[]) useCache =
    executeBuildScriptWithArgsAndFsiArgsAndReturnMessages script scriptArgs [||] useCache

/// Run the given buildscript with fsi.exe at the given working directory.  Provides full access to Fsi options and args.
let runBuildScriptWithFsiArgsAt printDetails (FsiArgs(fsiOptions, script, scriptArgs)) env useCache =
    runFAKEScriptWithFsiArgsAndRedirectMessages
        printDetails (FsiArgs(fsiOptions, script, scriptArgs)) env
        (onMessage true) (onMessage false)
        useCache

/// Run the given buildscript with fsi.exe
let runBuildScript printDetails script extraFsiArgs env useCache =
    runBuildScriptWithFsiArgsAt printDetails (FsiArgs(extraFsiArgs, script, [])) env useCache
