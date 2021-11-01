/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.ScriptRunner
open Fake.Runtime.Environment
open Fake.Runtime.Trace
open Fake.Runtime.Runners
#if NETSTANDARD1_6
open System.Runtime.Loader
#endif

open System.Reflection
open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Text.RegularExpressions
open System.Xml.Linq
open Yaaf.FSharp.Scripting


/// Handles a cache store operation, this should not throw as it is executed in a finally block and
/// therefore might eat other exceptions. And a caching error is not critical.
let private handleCoreCaching (context:FakeContext) (session:IFsiSession) fsiErrorOutput =
    try
        let wishName = context.CachedAssemblyFileName
        let d = session.DynamicAssemblyBuilder
        let name = fsiAssemblyName
#if NETSTANDARD1_6
        ignore d
        failwith "Wow. DotNetcore currently doesn't support saving dynamic assemblies. See https://github.com/dotnet/coreclr/issues/1709, https://github.com/dotnet/corefx/issues/4491. As it only hits F# it will probably never be implemented ;). One way to solve this would be to use IKVM.Reflection or Mono.Cecil in FSharp.Compiler.Service. But that's probably a lot of work. Feel free to start :). For now Caching cannot work."
#else
        d.Save(name + ".dll")
#endif
        if not <| Directory.Exists context.FakeDirectory then
            let di = Directory.CreateDirectory context.FakeDirectory
            di.Attributes <- FileAttributes.Directory ||| FileAttributes.Hidden

        let destinationFile = FileInfo(context.CachedAssemblyFilePath)
        let targetDirectory = destinationFile.Directory

        if (not <| targetDirectory.Exists) then targetDirectory.Create()
        if (destinationFile.Exists) then destinationFile.Delete()

        try
            // Now we change the AssemblyName of the written Assembly via Mono.Cecil.
            // Strictly speaking this is not needed, however this helps with executing
            // the test suite, as the runtime will only load a single
            // FSI-ASSEMBLY with version 0.0.0.0 by using LoadFrom...
#if NETSTANDARD1_6
            let reader =
                let searchpaths =
                    [ AppContext.BaseDirectory ]
                let resolve name =
                    let n = AssemblyName(name)
                    // Maybe we have a runtime or reference assembly available
                    match (context.Config.CompileOptions.FsiOptions.References |> List.map AssemblyInfo.ofLocation) @ context.Config.RuntimeOptions.RuntimeDependencies
                          |> List.tryFind (fun a -> a.FullName = name) with
                    | Some f -> f.Location
                    | None ->
                        match searchpaths
                              |> Seq.collect (fun p -> Directory.GetFiles(p, "*.dll"))
                              |> Seq.tryFind (fun f -> f.ToLowerInvariant().Contains(n.Name.ToLowerInvariant())) with
                        | Some f -> f
                        | None ->
                            failwithf "Could not resolve '%s'" name
                { new Mono.Cecil.IAssemblyResolver with
                    member x.Dispose() = ()
                    //member x.Resolve (name : string) =
                    //    Mono.Cecil.AssemblyDefinition.ReadAssembly(
                    //        resolve name,
                    //        new Mono.Cecil.ReaderParameters(AssemblyResolver = x))
                    //member x.Resolve (name : string, parms : Mono.Cecil.ReaderParameters) =
                    //    Mono.Cecil.AssemblyDefinition.ReadAssembly(resolve name, parms)
                    member x.Resolve (name : Mono.Cecil.AssemblyNameReference) =
                        Mono.Cecil.AssemblyDefinition.ReadAssembly(
                            resolve name.FullName,
                            new Mono.Cecil.ReaderParameters(AssemblyResolver = x))
                    //    x.Resolve(name.FullName)
                    member x.Resolve (name : Mono.Cecil.AssemblyNameReference, parms : Mono.Cecil.ReaderParameters) =
                        x.Resolve(name, parms) }
#else
            let reader = new Mono.Cecil.DefaultAssemblyResolver() // see https://github.com/fsharp/FAKE/issues/1084
            reader.AddSearchDirectory (Path.GetDirectoryName fakePath)
            reader.AddSearchDirectory (Path.GetDirectoryName typeof<string option>.Assembly.Location)
#endif
            let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
            let asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(name + ".dll", readerParams)
            asem.Name <- new Mono.Cecil.AssemblyNameDefinition(wishName, new Version(0,0,1))
            asem.Write(wishName + ".dll")
            File.Move(wishName + ".dll", destinationFile.FullName)
        with exn ->
            // If cecil fails we might want to trigger a warning, but you know what?
            // we can continue using the FSI-ASSEMBLY.dll
            traceFAKE "Warning (please open an issue on FAKE and /cc @matthid): %O" exn
            File.Move(name + ".dll", destinationFile.FullName)

        for name in [ name; wishName ] do
            for ext in [ ".dll"; ".pdb"; ".dll.mdb" ] do
                if File.Exists(name + ext) then
                    File.Delete(name + ext)

        { MaybeCompiledAssembly = Some destinationFile.FullName
          Warnings = fsiErrorOutput }
    with ex ->
#if NETSTANDARD1_6
        traceFAKE "Caching is not working on dotnetcore: %s" ex.Message
#else
        // Caching errors are not critical, and we shouldn't throw in a finally clause.
        traceFAKE "CACHING ERROR - please open a issue on FAKE and /cc @matthid\n\nError: %O" ex
#endif
        { MaybeCompiledAssembly = None
          Warnings = fsiErrorOutput }


/// public, because it is used by test code
let nameParser (scriptFileName:string) =
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

let tryRunCached (c:CoreCacheInfo) (context:FakeContext) : RunResult =
    if context.Config.VerboseLevel.PrintVerbose then trace "Using cache"
    let exampleName, _, parseName = nameParser context.Config.ScriptFilePath

    use execContext = Fake.Core.Context.FakeExecutionContext.Create true context.Config.ScriptFilePath []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
    let run () =
#if NETSTANDARD1_6
        let loadContext = AssemblyLoadContext.Default
        let ass = loadContext.LoadFromAssemblyPath(c.CompiledAssembly)
#else
        let ass = Reflection.Assembly.LoadFrom(c.CompiledAssembly)
#endif
        match ass.GetTypes()
              |> Seq.filter (fun t -> parseName t.FullName |> Option.isSome)
              |> Seq.map (fun t -> t.GetMethod("main@", BindingFlags.InvokeMethod ||| BindingFlags.Public ||| BindingFlags.Static))
              |> Seq.filter (isNull >> not)
              |> Seq.tryHead with
        | Some mainMethod ->
          try mainMethod.Invoke(null, [||])
              |> ignore
              RunResult.SuccessRun c.Warnings
          with ex ->
              RunResult.RuntimeError ex
        | None -> failwithf "We could not find a type similar to '%s' containing a 'main@' method in the cached assembly (%s)!" exampleName c.CompiledAssembly
    match context.Config.Redirect with
    | Some r -> Yaaf.FSharp.Scripting.Helper.consoleCapture r.Out r.Err run
    | None -> run()

let runUncached (context:FakeContext) : ResultCoreCacheInfo * RunResult =
    let co = context.Config.CompileOptions
    let options =  co.FsiOptions
    if context.Config.VerboseLevel.PrintVerbose then
      Trace.tracefn "FSI Args: %A" (options.AsArgs |> Seq.toList)
(*
    let cacheDir = context.ScriptFile.ScriptFakeDirectory
    if context.UseCache then
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
                if printDetails then trace "Cache is invalid, recompiling"
                oldFiles
                |> List.map (fun file ->
                    try file.Delete(); true
                    // File might be locked (for example when executing the test suite!)
                    with :? UnauthorizedAccessException ->
                        traceFAKE "Unable to access %s" file.FullName
                        false)
                |> List.exists id |> not
                // we could not delete a single file -> cache was not invalidated
                |> function
                    | true ->
                        traceError (sprintf "Unable to invalidate cache for '%s', please delete the .fake folder!" cacheInfo.ScriptFileName)
                    | _ -> ()
            else
                if printDetails then trace "Cache doesn't exist"
        else
            if printDetails then trace "Cache doesn't exist"
*)
    // Contains warnings and errors about the build script.
    let fsiErrorOutput = new System.Text.StringBuilder()
    use execContext = Fake.Core.Context.FakeExecutionContext.Create false context.Config.ScriptFilePath []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
    let session =
      try 
          match context.Config.Redirect with
          | Some r ->
              ScriptHost.Create
                (options, preventStdOut = true,
                  fsiErrWriter = ScriptHost.CreateForwardWriter
                    ((fun s ->
                        if String.IsNullOrWhiteSpace s |> not then
                            fsiErrorOutput.AppendLine s |> ignore),
                      removeNewLines = true),
                  outWriter = r.Out,
                  errWriter = r.Err)
          | None ->
              ScriptHost.Create
                (options, preventStdOut = true,
                  fsiErrWriter = ScriptHost.CreateForwardWriter
                    ((fun s ->
                        if String.IsNullOrWhiteSpace s |> not then
                            fsiErrorOutput.AppendLine s |> ignore),
                      removeNewLines = true))

      with :? FsiEvaluationException as e ->
          traceError "FsiEvaluationSession could not be created."
          traceError e.Result.Error.Merged
          reraise ()

    //session.EvalInteraction "let mutable __hook = ref Unchecked.defaultof<Fake.Core.Context.FakeExecutionContext>"
    //let __hook = session.EvalExpression<Fake.Core.Context.FakeExecutionContext ref> "__hook"
    //__hook := execContext
    //session.EvalInteraction "Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake !__hook)"

    let result =
        try
            session.EvalScriptAsInteraction context.Config.ScriptFilePath
            RunResult.SuccessRun ""
        with :? FsiEvaluationException as eval ->
            RunResult.RuntimeError (eval :> Exception)

    let strFsiErrorOutput = fsiErrorOutput.ToString()
    handleCoreCaching context session strFsiErrorOutput, result

let runFakeScript (cache:CoreCacheInfo option) (context:FakeContext) : ResultCoreCacheInfo * RunResult =
    match cache with
    | Some c when context.Config.UseCache ->
        try c.AsResult, tryRunCached c context
        with cacheError ->
            traceFAKE """CACHING WARNING
this might happen after Updates...
please open a issue on FAKE and /cc @matthid ONLY IF this happens reproducibly)

Error: %O""" cacheError
            runUncached context
    | _ ->
        runUncached context
