/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.CompileRunner

open Fake.Runtime.Trace
open Fake.Runtime.Runners
#if NETSTANDARD1_6
open System.Runtime.Loader
#endif

open System.Reflection
open System
open System.IO
open Yaaf.FSharp.Scripting
open Microsoft.FSharp.Compiler.SourceCodeServices


/// Handles a cache store operation, this should not throw as it is executed in a finally block and
/// therefore might eat other exceptions. And a caching error is not critical.
let private handleCoreCaching (context:FakeContext) (compiledAssembly:string) (errors:string) =
   { MaybeCompiledAssembly = Some compiledAssembly
     Warnings = errors }

/// public, because it is used by test code
let nameParser cachedAssemblyFileName scriptFileName =
    let noExtension = Path.GetFileNameWithoutExtension(scriptFileName)
    let inline fixNamespace (n:string) =
        n.Replace(".", "-")
    let className =
        sprintf "<StartupCode$%s>.$%s%s$%s"
          (fixNamespace cachedAssemblyFileName)
          (noExtension.Substring(0, 1).ToUpper())
          (noExtension.Substring(1))
          (Path.GetExtension(scriptFileName).Substring(1))

    let parseName (n:string) =
        if n = className then Some ()
        else None
    className, parseName

let tryRunCached (c:CoreCacheInfo) (context:FakeContext) : Exception option =
    use untilInvoke = Fake.Profile.startCategory Fake.Profile.Category.Analyzing
    if context.Config.VerboseLevel.PrintVerbose then trace "Using cache"
    let exampleName, parseName = nameParser context.CachedAssemblyFileName context.Config.ScriptFilePath

    use execContext = Fake.Core.Context.FakeExecutionContext.Create true context.Config.ScriptFilePath context.Config.ScriptArgs
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
    let result =
      Yaaf.FSharp.Scripting.Helper.consoleCapture context.Config.Out context.Config.Err (fun () ->
        let fullPath = System.IO.Path.GetFullPath c.CompiledAssembly
        let ass = context.AssemblyContext.LoadFromAssemblyPath fullPath
        let types =
            try ass.GetTypes()
            with :? ReflectionTypeLoadException as ref ->
                traceFAKE "Could not load types of compiled script:"
                for err in ref.LoaderExceptions do
                    if context.Config.VerboseLevel.PrintVerbose then
                        traceFAKE " - %O" err
                    else
                        traceFAKE " - %s" err.Message
                ref.Types
        match types
              |> Seq.filter (fun t -> parseName t.FullName |> Option.isSome)
              |> Seq.map (fun t -> t.GetMethod("main@", BindingFlags.InvokeMethod ||| BindingFlags.Public ||| BindingFlags.Static))
              |> Seq.filter (isNull >> not)
              |> Seq.tryHead with
        | Some mainMethod ->
          untilInvoke.Dispose()
          try use __  = Fake.Profile.startCategory Fake.Profile.Category.UserTime
              mainMethod.Invoke(null, [||]) |> ignore
              None
          with
          | :? TargetInvocationException as targetInvocation when not (isNull targetInvocation.InnerException) ->
              Some targetInvocation.InnerException
          | ex ->
              Some ex
        | None -> failwithf "We could not find a type similar to '%s' containing a 'main@' method in the cached assembly (%s)!" exampleName c.CompiledAssembly)

    use __ = Fake.Profile.startCategory Fake.Profile.Category.Cleanup
    (execContext :> System.IDisposable).Dispose()
    result

let runUncached (context:FakeContext) : ResultCoreCacheInfo * Exception option =
    use untilCompileFinished  = Fake.Profile.startCategory Fake.Profile.Category.Compiling
    let wishPath = context.CachedAssemblyFilePath + ".dll"

    if not <| Directory.Exists context.FakeDirectory then
        let di = Directory.CreateDirectory context.FakeDirectory
        di.Attributes <- FileAttributes.Directory ||| FileAttributes.Hidden

    let destinationFile = FileInfo(context.CachedAssemblyFilePath)
    let targetDirectory = destinationFile.Directory

    if (not <| targetDirectory.Exists) then targetDirectory.Create()
    if (destinationFile.Exists) then destinationFile.Delete()

    let co = context.Config.CompileOptions
    // see https://github.com/fsharp/FSharp.Compiler.Service/issues/755
    // see https://github.com/fsharp/FSharp.Compiler.Service/issues/799
    let options =
        { co.FsiOptions with
            FullPaths = true
            ScriptArgs = "--simpleresolution" :: "--targetprofile:netstandard" :: "--nowin32manifest" :: "-o" :: wishPath :: context.Config.ScriptFilePath :: co.FsiOptions.ScriptArgs
        }
    let args =
        options.AsArgs |> Seq.toList
        |> List.filter (fun arg -> arg <> "--")
    if context.Config.VerboseLevel.PrintVerbose then
      Trace.tracefn "FSC Args: [\"%s\"]" (String.Join("\";\n\"", args))

    let fsc = FSharpChecker.Create()
    let errors, returnCode = fsc.Compile (("fake.exe" :: args) |> List.toArray) |> Async.RunSynchronously
    untilCompileFinished.Dispose()
    let errors =
        errors
        |> Seq.filter (fun e -> e.ErrorNumber <> 213 && not (e.Message.StartsWith "'paket:"))
        |> Seq.toList
    let compilationExn = CompilationException errors
    let cacheInfo = handleCoreCaching context wishPath compilationExn.Message
    if returnCode = 0 then
        use execContext = Fake.Core.Context.FakeExecutionContext.Create false context.Config.ScriptFilePath []
        Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
        match cacheInfo.AsCacheInfo with
        | None -> failwithf "Expected caching to work after a successfull compilation"
        | Some c ->
            cacheInfo, tryRunCached c context
    else cacheInfo, Some (compilationExn:>exn)

let runFakeScript (cache:CoreCacheInfo option) (context:FakeContext) : ResultCoreCacheInfo * Exception option =
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
