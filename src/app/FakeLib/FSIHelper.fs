
[<AutoOpen>]
/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.FSIHelper

open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Text.RegularExpressions
open System.Xml.Linq
open Yaaf.FSharp.Scripting

let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\4.0\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

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

let getScriptHash pathsAndContents fsiOptions =
    let fullContents = getAllScriptContents pathsAndContents |> String.concat "\n"
    let fsiOptions = fsiOptions |> String.concat "\n"
    let paths = pathsAndContents |> Seq.map(fun x -> x.Location |> EnvironmentHelper.normalizePath) |> String.concat "\n"
    
    let hasher = HashLib.HashFactory.Checksum.CreateCRC32a()
    hasher.ComputeString(fullContents + paths + fsiOptions).ToString()

module private Cache =
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
/// The path to the F# Interactive tool.
let fsiPath =
    let ev = environVar "FSI"
    if not (isNullOrEmpty ev) then ev else
    if isUnix then
        let paths = appSettings "FSIPath" FSIPath
        // The standard name on *nix is "fsharpi"
        match tryFindFile paths "fsharpi" with
        | Some file -> file
        | None -> 
        // The early F# 2.0 name on *nix was "fsi"
        match tryFindFile paths "fsi" with
        | Some file -> file
        | None -> "fsharpi"
    else
        let dir = Path.GetDirectoryName fullAssemblyPath
        let fi = fileInfo (Path.Combine(dir, "fsi.exe"))
        if fi.Exists then fi.FullName else
        findPath "FSIPath" FSIPath "fsi.exe"

type FsiArgs =
    FsiArgs of string list * string * string list with
    static member parse (args:string array) =
        //Find first arg that does not start with - (as these are fsi options that precede the fsx).
        match args |> Array.tryFindIndex (fun arg -> arg.StartsWith("-") = false) with
        | Some(i) ->
            let fsxPath = args.[i]
            if fsxPath.EndsWith(".fsx", StringComparison.InvariantCultureIgnoreCase) then
                let fsiOpts = if i > 0 then args.[0..i-1] else [||]
                let scriptArgs = if args.Length > (i+1) then args.[i+1..] else [||]
                Choice1Of2(FsiArgs(fsiOpts |> List.ofArray, fsxPath, scriptArgs |> List.ofArray))
            else Choice2Of2(sprintf "Expected argument %s to be the build script path, but it does not have the .fsx extension." fsxPath) 
        | None -> Choice2Of2("Unable to locate the build script path.") 
    
let private FsiStartInfo workingDirectory (FsiArgs(fsiOptions, scriptPath, scriptArgs)) environmentVars =
    (fun (info: ProcessStartInfo) ->
        info.FileName <- fsiPath
        info.Arguments <- String.concat " " (fsiOptions @ [scriptPath] @ scriptArgs)
        info.WorkingDirectory <- workingDirectory
        let setVar k v =
            info.EnvironmentVariables.[k] <- v
        for (k, v) in environmentVars do
            setVar k v
        setVar "MSBuild"  msBuildExe
        setVar "GIT" Git.CommandHelper.gitPath
        setVar "FSI" fsiPath)

/// Creates a ProcessStartInfo which is configured to the F# Interactive.
let fsiStartInfo script workingDirectory env info =
    FsiStartInfo workingDirectory (FsiArgs([], script, [])) env info

/// Run the given buildscript with fsi.exe
let executeFSI workingDirectory script env =
    let (result, messages) =
        ExecProcessRedirected
            (fsiStartInfo script workingDirectory env)
            TimeSpan.MaxValue
    Thread.Sleep 1000
    (result, messages)

/// Run the given build script with fsi.exe and allows for extra arguments to FSI.
let executeFSIWithArgs workingDirectory script extraFsiArgs env =
    let result = ExecProcess (FsiStartInfo workingDirectory (FsiArgs(extraFsiArgs, script, [])) env) TimeSpan.MaxValue
    Thread.Sleep 1000
    result = 0

/// Run the given build script with fsi.exe and allows for extra arguments to the script. Returns output.
let executeFSIWithScriptArgsAndReturnMessages script (scriptArgs: string[]) =
    let (result, messages) =
        ExecProcessRedirected (fun si ->
            FsiStartInfo "" (FsiArgs([], script, scriptArgs |> List.ofArray)) [] si)
            TimeSpan.MaxValue
    Thread.Sleep 1000
    (result, messages)

open Microsoft.FSharp.Compiler.Interactive.Shell
open System.Reflection

type private AssemblySource = 
| GAC
| Disk

let hashRegex = Text.RegularExpressions.Regex("(?<script>.+)_(?<hash>[a-zA-Z0-9]+\.dll$)", System.Text.RegularExpressions.RegexOptions.Compiled)

type private CacheInfo =
  {
    ScriptFileName : string
    AssemblyPath : string
    AssemblyWarningsPath : string
    CacheConfigPath : string
    CacheConfig : Lazy<Cache.CacheConfig>
    IsValid : bool
  }

let private getCacheInfoFromScript printDetails fsiOptions scriptPath =
    let allScriptContents = getAllScripts scriptPath
    let scriptHash = lazy (getScriptHash allScriptContents fsiOptions)
    //TODO this is only calculating the hash for the input file, not anything #load-ed
    
    let scriptFileName = Path.GetFileName(scriptPath)
    let hashPath = "./.fake/" + scriptFileName + "_" + scriptHash.Value
    let assemblyPath = hashPath + ".dll"
    let assemblyWarningsPath = hashPath + "_warnings.txt"
    let cacheConfigPath = hashPath + "_config.xml"
    let cacheConfig = lazy Cache.read cacheConfigPath
    let cacheValid =
        let cacheFilesExist = 
            File.Exists(assemblyPath) &&
            File.Exists(cacheConfigPath) &&
            File.Exists(assemblyWarningsPath)
        if cacheFilesExist then
            let loadedAssemblies =
                cacheConfig.Value.Assemblies
                |> Seq.choose (fun assemInfo ->
                    try
                        let assem = 
                            if assemInfo.Location <> "" then
                                Reflection.Assembly.LoadFrom(assemInfo.Location)
                            else
                                Reflection.Assembly.Load(assemInfo.FullName)
                        Some(assemInfo, assem)
                    with
                    | ex -> 
                        if printDetails then tracef "Unable to find assembly %A" assemInfo
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
            AppDomain.CurrentDomain.add_AssemblyResolve(new ResolveEventHandler(fun sender ev ->
              let name = AssemblyName(ev.Name)
              match knownAssemblies.TryGetValue(ev.Name) with
              | true, a ->
                if printDetails then tracefn "Redirect assembly load to known assembly: %s" ev.Name
                a
              | _ ->
                match loadedAssemblies
                      |> Seq.map snd
                      |> Seq.tryFind (fun asem ->
                          let n = asem.GetName()
                          n.Name = name.Name &&
                          n.GetPublicKeyToken() = name.GetPublicKeyToken()) with
                | Some (asem) ->
                    traceFAKE "Redirect assembly from '%s' to '%s'" ev.Name asem.FullName
                    asem
                | _ ->
                    null))
            assemVersionValidCount = Seq.length cacheConfig.Value.Assemblies
        else
            false
    { ScriptFileName = scriptFileName
      AssemblyPath = assemblyPath
      AssemblyWarningsPath = assemblyWarningsPath
      CacheConfigPath = cacheConfigPath
      CacheConfig = cacheConfig
      IsValid = cacheValid }

/// Run the given FAKE script with fsi.exe at the given working directory. Provides full access to Fsi options and args. Redirect output and error messages.
let internal runFAKEScriptWithFsiArgsAndRedirectMessages printDetails (FsiArgs(fsiOptions, scriptPath, scriptArgs)) env onErrMsg onOutMsg useCache cleanCache =

    if printDetails then traceFAKE "Running Buildscript: %s" scriptPath

    // Add arguments to the Environment
    for (k,v) in env do
      Environment.SetEnvironmentVariable(k, v, EnvironmentVariableTarget.Process)

    // Create an env var that only contains the build script args part from the --fsiargs (or "").
    Environment.SetEnvironmentVariable("fsiargs-buildscriptargs", String.Join(" ", scriptArgs))

    let options =
        fsiOptions
        |> FsiOptions.ofArgs

    let handleException (ex : Exception) =
        onErrMsg (ex.ToString())

    let scriptPath =
        if Path.IsPathRooted scriptPath then
            scriptPath
        else
            Path.Combine(Directory.GetCurrentDirectory(), scriptPath)

    let cacheInfo = getCacheInfoFromScript printDetails fsiOptions scriptPath
    let getScriptAndHash fileName =
        let matched = hashRegex.Match(fileName)
        matched.Groups.Item("script").Value, matched.Groups.Item("hash").Value

    if useCache && cacheInfo.IsValid then
        
        if printDetails then trace "Using cache"
        let noExtension = Path.GetFileNameWithoutExtension(cacheInfo.ScriptFileName)

        let startString = "<StartupCode$FSI_"
        let endString =
          sprintf "_%s%s$%s"
            (noExtension.Substring(0, 1).ToUpper())
            (noExtension.Substring(1))
            (Path.GetExtension(cacheInfo.ScriptFileName).Substring(1))
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
        try
            match Reflection.Assembly.LoadFrom(cacheInfo.AssemblyPath)
                  .GetTypes()
                  |> Seq.filter (fun t -> parseName t.FullName |> Option.isSome)
                  |> Seq.tryHead with
            | Some mainModule ->
              try
                  mainModule.InvokeMember(
                      "main@",
                      BindingFlags.InvokeMethod ||| BindingFlags.Public ||| BindingFlags.Static,
                      null, null, [||])
                  |> ignore
                  true
              with
              | ex ->
                  handleException ex
                  false
            | None -> failwithf "We could not find a type similar to '%s' in the cached assembly!" exampleName
        finally
            try
                traceFAKE "%s" (File.ReadAllText cacheInfo.AssemblyWarningsPath)
            with e -> traceError (e.ToString())
    else
        let cacheDir = DirectoryInfo(Path.Combine(".",".fake"))
        if useCache then
            if cacheDir.Exists then
                let oldFiles = 
                    cacheDir.GetFiles()
                    |> Seq.filter(fun file -> 
                        let oldScriptName, _ = getScriptAndHash(file.Name)
                        oldScriptName = cacheInfo.ScriptFileName)

                if (oldFiles |> Seq.length) > 0 then
                    if cleanCache then
                        for file in oldFiles do
                            file.Delete()
                    if printDetails then trace "Cache is invalid, recompiling"
                else 
                    if printDetails then trace "Cache doesn't exist"
            else
                if printDetails then trace "Cache doesn't exist"

        // Contains warnings and errors about the build script.
        let fsiErrorOutput = new System.Text.StringBuilder()
        let session =
          try ScriptHost.Create
                (options, preventStdOut = true,
                  fsiErrWriter = ScriptHost.CreateForwardWriter
                    ((fun s ->
                        if String.IsNullOrWhiteSpace s |> not then
                            fsiErrorOutput.AppendLine s |> ignore),
                      removeNewLines = true),
                  outWriter = ScriptHost.CreateForwardWriter onOutMsg,
                  errWriter = ScriptHost.CreateForwardWriter onErrMsg)
          with :? FsiEvaluationException as e ->
              traceError "FsiEvaluationSession could not be created."
              traceError e.Result.Error.Merged
              reraise ()

        try
            try
                session.EvalScript scriptPath
                true
            with :? FsiEvaluationException as eval ->
                // Write Script Warnings & Errors at the end
                handleException eval
                false
        finally
            // Write Script Warnings & Errors at the end
            traceFAKE "%O" fsiErrorOutput
            // Cache in the error case as well.
            try
                if useCache && not cacheInfo.IsValid then
                    session.DynamicAssemblyBuilder.Save("FSI-ASSEMBLY.dll")
                    if not <| Directory.Exists cacheDir.FullName then
                        let di = Directory.CreateDirectory cacheDir.FullName
                        di.Attributes <- FileAttributes.Directory ||| FileAttributes.Hidden

                    let destinationFile = FileInfo(cacheInfo.AssemblyPath)
                    let targetDirectory = destinationFile.Directory

                    if (not <| targetDirectory.Exists) then targetDirectory.Create()
                    if (destinationFile.Exists) then destinationFile.Delete()

                    File.WriteAllText(cacheInfo.AssemblyWarningsPath, fsiErrorOutput.ToString())
                    File.Move("FSI-ASSEMBLY.dll", cacheInfo.AssemblyPath)
                    
                    if File.Exists("FSI-ASSEMBLY.pdb") then
                        File.Delete("FSI-ASSEMBLY.pdb")
                    if File.Exists("FSI-ASSEMBLY.dll.mdb") then
                        File.Delete("FSI-ASSEMBLY.dll.mdb")

                    let dynamicAssemblies =
                        System.AppDomain.CurrentDomain.GetAssemblies()
                        |> Seq.filter(fun assem -> assem.IsDynamic)
                        |> Seq.map(fun assem -> assem.GetName().Name)
                        |> Seq.filter(fun assem -> assem <> "FSI-ASSEMBLY")
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
                            // They are not dynamic, but can't be re-used either.
                            |> Seq.filter(fun assem -> not <| assem.GetName().Name.StartsWith("CompiledRazorTemplates.Dynamic.RazorEngine_"))
                        
                        let cacheConfig : XDocument = Cache.create assemblies
                        cacheConfig.Save(cacheInfo.CacheConfigPath)
                        if printDetails then trace (System.Environment.NewLine + "Saved cache")
            with ex ->
                handleException ex
                reraise()

/// Run the given buildscript with fsi.exe and allows for extra arguments to the script. Returns output.
let executeBuildScriptWithArgsAndFsiArgsAndReturnMessages script (scriptArgs: string[]) (fsiArgs:string[]) useCache cleanCache =
    let messages = ref []
    let appendMessage isError msg =
        traceUnknown msg // Some test code expects that the executed script writes to stdout
        messages := { IsError = isError
                      Message = msg
                      Timestamp = DateTimeOffset.UtcNow } :: !messages
    let result =
        runFAKEScriptWithFsiArgsAndRedirectMessages
            true (FsiArgs(fsiArgs |> List.ofArray, script, scriptArgs |> List.ofArray)) []
            (appendMessage true) (appendMessage false) useCache cleanCache
    (result, !messages)

/// Run the given buildscript with fsi.exe and allows for extra arguments to the script. Returns output.
let executeBuildScriptWithArgsAndReturnMessages script (scriptArgs: string[]) useCache cleanCache =
    executeBuildScriptWithArgsAndFsiArgsAndReturnMessages script scriptArgs [||] useCache cleanCache

/// Run the given buildscript with fsi.exe at the given working directory.  Provides full access to Fsi options and args.
let runBuildScriptWithFsiArgsAt printDetails (FsiArgs(fsiOptions, script, scriptArgs)) env useCache cleanCache =
    runFAKEScriptWithFsiArgsAndRedirectMessages
        printDetails (FsiArgs(fsiOptions, script, scriptArgs)) env
        traceError traceUnknown
        useCache
        cleanCache

/// Run the given buildscript with fsi.exe at the given working directory.
let runBuildScriptAt printDetails script extraFsiArgs env useCache cleanCache =
    runBuildScriptWithFsiArgsAt printDetails (FsiArgs(extraFsiArgs, script, [])) env useCache cleanCache

/// Run the given buildscript with fsi.exe
let runBuildScript printDetails script extraFsiArgs env useCache cleanCache =
    runBuildScriptAt printDetails script extraFsiArgs env useCache cleanCache
