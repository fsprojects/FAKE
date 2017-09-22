/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.CompileRunner
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
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler



/// Handles a cache store operation, this should not throw as it is executed in a finally block and
/// therefore might eat other exceptions. And a caching error is not critical.
let private handleCoreCaching (context:FakeContext) (compiledAssembly:string) (errors:string) =
   { MaybeCompiledAssembly = Some compiledAssembly
     Warnings = errors }

/// public, because it is used by test code
let nameParser cachedAssemblyFileName scriptFileName =
    let noExtension = Path.GetFileNameWithoutExtension(scriptFileName)
    let className =
        sprintf "<StartupCode$%s>.$%s%s$%s"
          cachedAssemblyFileName
          (noExtension.Substring(0, 1).ToUpper())
          (noExtension.Substring(1))
          (Path.GetExtension(scriptFileName).Substring(1))

    let parseName (n:string) =
        if n = className then Some ()
        else None
    className, parseName

let tryRunCached (c:CoreCacheInfo) (context:FakeContext) : Exception option =
    if context.Config.PrintDetails then trace "Using cache"
    let exampleName, parseName = nameParser context.CachedAssemblyFileName context.Config.ScriptFilePath

    use execContext = Fake.Core.Context.FakeExecutionContext.Create true context.Config.ScriptFilePath []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
    Yaaf.FSharp.Scripting.Helper.consoleCapture context.Config.Out context.Config.Err (fun () ->
        let fullPath = System.IO.Path.GetFullPath c.CompiledAssembly
        let ass = context.AssemblyContext.LoadFromAssemblyPath fullPath
        let types =
            try ass.GetTypes()
            with :? ReflectionTypeLoadException as ref ->
                traceFAKE "Could not load types of compiled script:"
                for err in ref.LoaderExceptions do
                    if context.Config.PrintDetails then
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
          try mainMethod.Invoke(null, [||])
              |> ignore
              None
          with
          | :? TargetInvocationException as targetInvocation when not (isNull targetInvocation.InnerException) ->
              Some targetInvocation.InnerException
          | ex ->
              Some ex
        | None -> failwithf "We could not find a type similar to '%s' containing a 'main@' method in the cached assembly (%s)!" exampleName c.CompiledAssembly)


let runUncached (context:FakeContext) : ResultCoreCacheInfo * Exception option =

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
        [co.AdditionalArguments; [ "--targetprofile:netcore"; "--nowin32manifest"; "-o"; wishPath; context.Config.ScriptFilePath ] ]
        |> List.concat
        |> FsiOptions.ofArgs
        |> fun f ->
            { f with
                References = f.References @ co.CompileReferences }
    let args =
        options.AsArgs |> Seq.toList
        |> List.filter (fun arg -> arg <> "--")
    let formatError (e:FSharpErrorInfo) =
         sprintf "%s (%d,%d)-(%d,%d): %A FS%04d: %s" e.FileName e.StartLineAlternate e.StartColumn e.EndLineAlternate e.EndColumn e.Severity e.ErrorNumber e.Message
    let formatErrors errors =
        System.String.Join("\n", errors |> Seq.map formatError)
    if context.Config.PrintDetails then
      Trace.tracefn "FSC Args: %A" (args)

    let fsc = FSharpChecker.Create()
    let errors, returnCode = fsc.Compile (args |> List.toArray) |> Async.RunSynchronously
    if returnCode <> 0 then failwithf "Compilation failed: \n%s" (formatErrors errors)

    use execContext = Fake.Core.Context.FakeExecutionContext.Create false context.Config.ScriptFilePath []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

    let errorsString = formatErrors errors

    let cacheInfo = handleCoreCaching context wishPath errorsString
    match cacheInfo.AsCacheInfo with
    | None -> failwithf "Expected caching to work after a successfull compilation"
    | Some c ->
        cacheInfo, tryRunCached c context

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
