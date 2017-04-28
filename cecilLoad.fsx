(* -- Fake Dependencies paket.dependencies
file ./paket.dependencies
group NetcoreBuild
-- Fake Dependencies -- *)

#if DOTNETCORE
// We need to use this for now as "regular" Fake breaks when its caching logic cannot find "loadDependencies.fsx".
// This is the reason why we need to checkin the "loadDependencies.fsx" file for now...
#cd ".fake"
#cd __SOURCE_FILE__
#load "loadDependencies.fsx"
#cd __SOURCE_DIRECTORY__

open System
open System.IO
open System.Reflection
open Fake.Core
open Fake.Core.BuildServer
open Fake.Core.Environment
open Fake.Core.Trace
open Fake.Core.Targets
open Fake.Core.TargetOperators
open Fake.Core.String
open Fake.Core.SemVer
open Fake.Core.ReleaseNotes
open Fake.Core.Process
open Fake.Core.Globbing
open Fake.Core.Globbing.Operators
open Fake.IO.FileSystem
open Fake.IO.FileSystem.FileFilter
open Fake.IO.Zip
open Fake.IO.FileSystem.Directory
open Fake.IO.FileSystem.File
open Fake.IO.FileSystem.Operators
open Fake.IO.FileSystem.Shell
open Fake.DotNet.AssemblyInfoFile
open Fake.DotNet.AssemblyInfoFile.AssemblyInfo
open Fake.DotNet.MsBuild
open Fake.DotNet.Cli
open Fake.Testing.Common
open Fake.DotNet.Testing.MSpec
open Fake.DotNet.Testing.XUnit2
open Fake.DotNet.Testing.NUnit3
open Fake.DotNet.NuGet.NuGet
open Fake.Core.Globbing.Tools
open Fake.Windows

let currentDirectory = Shell.pwd()
#else
//#if DESIGNTIME
#I @"packages/build/FAKE/tools/"
#r @"FakeLib.dll"
//#else
//#r "src/app/FakeLib/bin/Debug/FakeLib.dll"
//#endif
#r @"packages/Mono.Cecil/lib/net40/Mono.Cecil.dll"
#I "packages/build/SourceLink.Fake/tools/"
#load "packages/build/SourceLink.Fake/tools/SourceLink.fsx"

open Fake
open Fake.Git
open Fake.FSharpFormatting
open System.IO
open SourceLink
open Fake.ReleaseNotesHelper
open Fake.AssemblyInfoFile
open Fake.Testing.XUnit2
open Fake.Testing.NUnit3
#endif

// properties
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"]
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/fsharp"

let release = LoadReleaseNotes "RELEASE_NOTES.md"

let packages =
    ["FAKE.Core",projectDescription
     "FAKE.Gallio",projectDescription + " Extensions for Gallio"
     "FAKE.IIS",projectDescription + " Extensions for IIS"
     "FAKE.FluentMigrator",projectDescription + " Extensions for FluentMigrator"
     "FAKE.SQL",projectDescription + " Extensions for SQL Server"
     "FAKE.Experimental",projectDescription + " Experimental Extensions"
     "FAKE.Deploy.Lib",projectDescription + " Extensions for FAKE Deploy"
     projectName,projectDescription + " This package bundles all extensions."
     "FAKE.Lib",projectDescription + " FAKE helper functions as library"]

let buildDir = "./build"
let testDir = "./test"
let docsDir = "./docs"
let apidocsDir = "./docs/apidocs/"
let nugetDir = "./nuget"
let reportDir = "./report"
let packagesDir = "./packages"
let buildMergedDir = buildDir </> "merged"

let additionalFiles = [
    "License.txt"
    "README.markdown"
    "RELEASE_NOTES.md"
    "./packages/FSharp.Core/lib/net40/FSharp.Core.sigdata"
    "./packages/FSharp.Core/lib/net40/FSharp.Core.optdata"]

// Targets
Target "Clean" (fun _ ->
    !! "src/*/*/bin"
    ++ "src/*/*/obj"
    |> CleanDirs

    CleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDir; reportDir])

Target "RenameFSharpCompilerService" (fun _ ->
  for packDir in ["FSharp.Compiler.Service";"netcore"</>"FSharp.Compiler.Service"] do
    // for framework in ["net40"; "net45"] do
    for framework in ["netstandard1.6"; "net45"] do
      let dir = __SOURCE_DIRECTORY__ </> "packages"</>packDir</>"lib"</>framework
      let targetFile = dir </>  "FAKE.FSharp.Compiler.Service.dll"
      DeleteFile targetFile

#if DOTNETCORE
      let reader =
          let searchpaths =
              [ dir; __SOURCE_DIRECTORY__ </> "packages/FSharp.Core/lib/net40" ]
          let resolve name =
              let n = AssemblyName(name)
              match searchpaths
                      |> Seq.collect (fun p -> Directory.GetFiles(p, "*.dll"))
                      |> Seq.tryFind (fun f -> f.ToLowerInvariant().Contains(n.Name.ToLowerInvariant())) with
              | Some f -> f
              | None ->
                  failwithf "Could not resolve '%s'" name
          let readAssemblyE (name:string) (parms: Mono.Cecil.ReaderParameters) =
              Mono.Cecil.AssemblyDefinition.ReadAssembly(
                  resolve name,
                  parms)
          let readAssembly (name:string) (x:Mono.Cecil.IAssemblyResolver) =
              readAssemblyE name (new Mono.Cecil.ReaderParameters(AssemblyResolver = x))
          { new Mono.Cecil.IAssemblyResolver with
              member x.Dispose () = ()
              //member x.Resolve (name : string) = readAssembly name x
              //member x.Resolve (name : string, parms : Mono.Cecil.ReaderParameters) = readAssemblyE name parms
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference) = readAssembly name.FullName x
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference, parms : Mono.Cecil.ReaderParameters) = readAssemblyE name.FullName parms
               }
#else
      let reader = new Mono.Cecil.DefaultAssemblyResolver()
      reader.AddSearchDirectory(dir)
      reader.AddSearchDirectory(__SOURCE_DIRECTORY__ </> "packages/FSharp.Core/lib/net40")
#endif
      let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
      let asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(dir </>"FSharp.Compiler.Service.dll", readerParams)
      asem.Name <- new Mono.Cecil.AssemblyNameDefinition("FAKE.FSharp.Compiler.Service", new System.Version(1,0,0,0))
      asem.Write(dir</>"FAKE.FSharp.Compiler.Service.dll")
)

echo "testcecilLoad"