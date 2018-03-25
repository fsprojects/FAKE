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
open System.Xml.Linq
open Yaaf.FSharp.Scripting



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


type ScriptCompileOptions =
  { CompileReferences : string list
    RuntimeDependencies : AssemblyInfo list
    AdditionalArguments : string list }

type FakeConfig =
  { VerboseLevel : Trace.VerboseLevel
    ScriptFilePath : string
    CompileOptions : ScriptCompileOptions
    UseCache : bool
    Out: TextWriter
    Err: TextWriter
    Environment: seq<string * string> }

let fsiAssemblyName = "removeme"
let cachedAssemblyPrefix = "FAKE_CACHE_"

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
    AssemblyContext : AssemblyLoadContext
    FakeDirectory : string
    Hash : string }
    member x.FileName = Path.GetFileNameWithoutExtension x.Config.ScriptFilePath
    member x.FileNameWithExtension = Path.GetFileName x.Config.ScriptFilePath
    member x.HashPath = Path.Combine(x.FakeDirectory, x.FileNameWithExtension, x.FileName + "_" + x.Hash)
    member x.CachedAssemblyFileName = x.FileName + "_" + x.Hash
    member x.CachedAssemblyFilePath = Path.Combine(x.FakeDirectory, x.FileNameWithExtension, x.CachedAssemblyFileName)
