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
open Microsoft.FSharp.Compiler.SimpleSourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler



/// Handles a cache store operation, this should not throw as it is executed in a finally block and
/// therefore might eat other exceptions. And a caching error is not critical.
let private handleCoreCaching (context:FakeContext) (compiledAssembly:string) (errors:string) =
    try
        let wishName = context.CachedAssemblyFileName

        if not <| Directory.Exists context.FakeDirectory then
            let di = Directory.CreateDirectory context.FakeDirectory
            di.Attributes <- FileAttributes.Directory ||| FileAttributes.Hidden

        let destinationFile = FileInfo(context.CachedAssemblyFilePath)
        let targetDirectory = destinationFile.Directory

        if (not <| targetDirectory.Exists) then targetDirectory.Create()
        if (destinationFile.Exists) then destinationFile.Delete()

        File.Copy (compiledAssembly, wishName)

        { MaybeCompiledAssembly = Some destinationFile.FullName
          Warnings = errors }
    with ex ->
        // Caching errors are not critical, and we shouldn't throw in a finally clause.
        traceFAKE "CACHING ERROR - please open a issue on FAKE and /cc @matthid\n\nError: %O" ex

        { MaybeCompiledAssembly = None
          Warnings = errors }


/// public, because it is used by test code
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

let tryRunCached (c:CoreCacheInfo) (context:FakeContext) : Exception option =
    if context.Config.PrintDetails then trace "Using cache"
    let exampleName, _, parseName = nameParser context.Config.ScriptFilePath

    use execContext = Fake.Core.Context.FakeExecutionContext.Create true context.Config.ScriptFilePath []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
    Yaaf.FSharp.Scripting.Helper.consoleCapture context.Config.Out context.Config.Err (fun () ->
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
              None
          with ex ->
              Some ex
        | None -> failwithf "We could not find a type similar to '%s' containing a 'main@' method in the cached assembly (%s)!" exampleName c.CompiledAssembly)


let runUncached (context:FakeContext) : ResultCoreCacheInfo * Exception option =
    let co = context.Config.CompileOptions
    let options =
        [co.AdditionalArguments; ["-o"; "test.dll" ] ]
        |> List.concat
        |> FsiOptions.ofArgs
        |> fun f ->
            { f with
                References = f.References @ co.CompileReferences }
    if context.Config.PrintDetails then
      Trace.tracefn "FSC Args: %A" (options.AsArgs |> Seq.toList)

    let fsc = FSharpChecker.Create()
    let errors, returnCode = fsc.Compile (options.AsArgs) |> Async.RunSynchronously
    if returnCode <> 0 then failwithf "Compilation failed: %A" errors
    if errors.Length > 0 then
        Trace.traceFAKE "Warnings: %A" errors

    use execContext = Fake.Core.Context.FakeExecutionContext.Create false context.Config.ScriptFilePath []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

    let errorsString = sprintf "%A" errors

    let cacheInfo = handleCoreCaching context "test.dll" errorsString
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