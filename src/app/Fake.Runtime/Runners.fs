/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.Runners
open Fake.Runtime.Environment
open Fake.Runtime.Trace
#if NETSTANDARD1_6
open System.Runtime.Loader
#endif

open System.Reflection
open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Xml.Linq
open Yaaf.FSharp.Scripting

open FSharp.Compiler.SourceCodeServices

module internal ExnHelper =
   let formatError (e:FSharpErrorInfo) =
     sprintf "%s (%d,%d)-(%d,%d): %A FS%04d: %s" e.FileName e.StartLineAlternate e.StartColumn e.EndLineAlternate e.EndColumn e.Severity e.ErrorNumber e.Message
   let formatErrors errors =
        System.String.Join("\n", errors |> Seq.map formatError)

type CompilationErrors =
  { Errors : FSharpErrorInfo list }
  member x.FormattedErrors = ExnHelper.formatErrors x.Errors
  static member ofErrors errors = { Errors = errors }

#if !NETSTANDARD1_6
type AssemblyLoadContext () =
  member x.LoadFromAssemblyPath (loc:string) =
    Reflection.Assembly.LoadFrom(loc)
  member x.LoadFromAssemblyName(fullname:AssemblyName)=
    Reflection.Assembly.Load(fullname)
#endif

type AssemblyInfo =
  { FullName : string
    Version : string
    Location : string }
  static member ofLocation (loc:string) =
    let n = Mono.Cecil.AssemblyDefinition.ReadAssembly(loc).Name
    { FullName = n.FullName; Version = n.Version.ToString(); Location = loc }

type NativeLibrary =
    { File : string }
type CompileOptions = 
    internal { FsiOptions : FsiOptions }
    member x.AsArgs = x.FsiOptions.AsArgs

type RuntimeOptions =
    internal { _RuntimeDependencies : AssemblyInfo list; _NativeLibraries : NativeLibrary list }
    member x.RuntimeDependencies : AssemblyInfo list = x._RuntimeDependencies
    member x.NativeLibraries : NativeLibrary list = x._NativeLibraries
type FakeConfig =
  { VerboseLevel : Trace.VerboseLevel
    ScriptFilePath : string
    ScriptTokens : Lazy<Fake.Runtime.FSharpParser.TokenizedScript>
    RuntimeOptions : RuntimeOptions
    CompileOptions : CompileOptions
    UseCache : bool
    UseSimpleRestore : bool
    RestoreOnlyGroup : bool
    Out: TextWriter
    Err: TextWriter
    ScriptArgs: string list }
  member x.FsArgs = x.CompileOptions.FsiOptions.AsArgs

let fsiAssemblyName = "removeme"
let cachedAssemblyPrefix = "FAKE_CACHE_"
// This file is created immediately in order to make fsc happy
let loadScriptName = "intellisense.fsx"
// This file is created lazily and is not used by fsc (only for intellisense).
let loadScriptLazyName = "intellisense_lazy.fsx"

type RunResult =
  | CompilationError of CompilationErrors
  | RuntimeError of Exception
  | SuccessRun of warnings:string

type ResultCoreCacheInfo =
  { MaybeCompiledAssembly : string option
    Warnings : string }
    member x.AsCacheInfo =
      match x.MaybeCompiledAssembly with
      | Some c -> Some { CompiledAssembly = c; Warnings = x.Warnings }
      | None -> None
and CoreCacheInfo =
  { CompiledAssembly : string
    Warnings : string }
    member x.AsResult =
      { MaybeCompiledAssembly = Some x.CompiledAssembly
        Warnings = x.Warnings }
type FakeContext =
  { Config : FakeConfig
    CreateAssemblyContext : unit -> AssemblyLoadContext
    FakeDirectory : string
    Hash : string }
    member x.FileName = Path.GetFileNameWithoutExtension x.Config.ScriptFilePath
    member x.FileNameWithExtension = Path.GetFileName x.Config.ScriptFilePath
    member x.HashPath = Path.Combine(x.FakeDirectory, x.FileNameWithExtension, x.FileName + "_" + x.Hash)
    member x.CachedAssemblyFileName = x.FileName + "_" + x.Hash
    member x.CachedAssemblyFilePath = Path.Combine(x.FakeDirectory, x.FileNameWithExtension, x.CachedAssemblyFileName)
