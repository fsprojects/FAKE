/// This module contains a file pattern globbing implementation.
namespace Fake.IO
open System.Collections.Generic

type IGlobbingPattern =
    inherit IEnumerable<string>
    abstract BaseDirectory : string
    abstract Includes : string list
    abstract Excludes : string list

namespace Fake.IO.Globbing
open Fake.IO
open System.Collections.Generic

type LazyGlobbingPattern =
    { BaseDirectory : string
      Includes : string list
      Excludes : string list }
    
    interface IGlobbingPattern with
        member this.BaseDirectory = this.BaseDirectory
        member this.Includes = this.Includes
        member this.Excludes = this.Excludes

    interface IEnumerable<string> with
        
        member this.GetEnumerator() = 
            let hashSet = HashSet()
            
            let excludes = 
                seq { 
                    for pattern in this.Excludes do
                        yield! Glob.search this.BaseDirectory pattern
                }
                |> Set.ofSeq
            
            let files = 
                seq { 
                    for pattern in this.Includes do
                        yield! Glob.search this.BaseDirectory pattern
                }
                |> Seq.filter (fun x -> not (Set.contains x excludes))
                |> Seq.filter (fun x -> hashSet.Add x)
            
            files.GetEnumerator()
        
        member this.GetEnumerator() = (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

type ResolvedGlobbingPattern =
    { BaseDirectory : string
      Includes : string list
      Excludes : string list
      Results : string list }
    
    interface IGlobbingPattern with
        member this.BaseDirectory = this.BaseDirectory
        member this.Includes = this.Includes
        member this.Excludes = this.Excludes

    interface IEnumerable<string> with
        member this.GetEnumerator() = (this.Results :> IEnumerable<string>).GetEnumerator()
        member this.GetEnumerator() = (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

namespace Fake.IO
open System.IO
open Fake.IO.Globbing

[<AutoOpen>] // A bit of a hack but we need those extensions for backwards compat.
module GlobbingPatternExtensions =
    type IGlobbingPattern with
        member internal this.Pattern =
            match this with
            | :? LazyGlobbingPattern as l -> l
            | _ ->
                { BaseDirectory = this.BaseDirectory
                  Includes = this.Includes
                  Excludes = this.Excludes }
        member this.Resolve() =
            match this with
            | :? ResolvedGlobbingPattern as res -> res :> IGlobbingPattern
            | _ ->
                let list =
                    this
                    |> Seq.toList
                { BaseDirectory = this.BaseDirectory
                  Includes = this.Includes
                  Excludes = this.Excludes
                  Results = list } :> IGlobbingPattern
        /// Adds the given pattern to the file includes
        member this.And pattern = { this.Pattern with Includes = this.Includes @ [ pattern ] } :> IGlobbingPattern
        
        /// Ignores files with the given pattern
        member this.ButNot pattern = { this.Pattern with Excludes = pattern :: this.Excludes } :> IGlobbingPattern
        
        /// Sets a directory as BaseDirectory.
        member this.SetBaseDirectory(dir : string) = { this.Pattern with BaseDirectory = dir.TrimEnd(Path.DirectorySeparatorChar) } :> IGlobbingPattern
        
        /// Checks if a particular file is matched
        member this.IsMatch (path : string) =
            let fullDir pattern = 
                if Path.IsPathRooted(pattern) then
                    pattern
                else
                    System.IO.Path.Combine(this.BaseDirectory, pattern)
            let fullPath = Path.GetFullPath path
            let included = 
                this.Includes
                |> Seq.exists(fun fileInclude ->
                    Glob.isMatch (fullDir fileInclude) fullPath
                )
            let excluded = 
                this.Excludes
                |> Seq.exists(fun fileExclude ->
                    Glob.isMatch (fullDir fileExclude) fullPath
                )

            included && not excluded


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GlobbingPattern =
    let private defaultBaseDir = Path.GetFullPath "."

    /// Include files
    let create x = 
        { BaseDirectory = defaultBaseDir
          Includes = [ x ]
          Excludes = [] } :> IGlobbingPattern

    /// Start an empty globbing pattern from the specified directory
    let createFrom (dir : string) =
        { BaseDirectory = dir
          Includes = []
          Excludes = [] } :> IGlobbingPattern

    /// Sets a directory as baseDirectory for fileIncludes. 
    let setBaseDir (dir : string) (fileIncludes : IGlobbingPattern) = fileIncludes.SetBaseDirectory dir

    /// Get base include directories. Used to get a smaller set of directories from a globbing pattern.
    let getBaseDirectoryIncludes (fileIncludes: IGlobbingPattern) =
            let directoryIncludes = fileIncludes.Includes |> Seq.map (fun file -> Globbing.Glob.getRoot fileIncludes.BaseDirectory file)

            // remove subdirectories
            directoryIncludes
            |> Seq.filter (fun d ->
                            directoryIncludes
                            |> Seq.exists (fun p -> d.StartsWith (p + string System.IO.Path.DirectorySeparatorChar) && p <> d)
                            |> not)
            |> Seq.toList

namespace Fake.IO.Globbing

open Fake.IO
open System.IO

// Compat
[<System.Obsolete("Please use IGlobbingPattern instead")>]
type FileIncludes = IGlobbingPattern

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<System.Obsolete("Please use GlobbingPattern instead")>]
module FileIncludes =
    /// Include files
    [<System.Obsolete("Please use GlobbingPattern.create instead")>]
    let Include x = GlobbingPattern.create x

    /// Sets a directory as baseDirectory for fileIncludes. 
    [<System.Obsolete("Please use GlobbingPattern instead")>]
    let SetBaseDir (dir : string) (fileIncludes : IGlobbingPattern) = GlobbingPattern.setBaseDir dir fileIncludes

/// Contains operators to find and process files.
/// This module is part of the `Fake.IO.FileSystem` package
///
/// ### Simple glob using as list
///
///     #r "paket: nuget Fake.IO.FileSystem //"
///     open Fake.IO.Globbing.Operators
///     let csProjectFiles = !! "src/*.csproj"
///     
///     for projectFile in csProjectFiles do
///         printf "F# ProjectFile: %s" projectFile
///
/// ### Combine globs
///
///     #r "paket: nuget Fake.IO.FileSystem //"
///     open Fake.IO.Globbing.Operators
///     let projectFiles =
///         !! "src/*/*.*proj"
///         ++ "src/*/*.target"
///         -- "src/*/*.vbproj"
///     
///     for projectFile in projectFiles do
///         printf "ProjectFile: %s" projectFile
///
/// ### Forward globs to tasks
///
///     #r "paket:
///     nuget Fake.Core.Target
///     nuget Fake.IO.FileSystem //"
///     open Fake.Core
///     open Fake.IO
///     open Fake.IO.Globbing.Operators
///     Target.create "Clean" (fun _ ->
///        !! "src/*/*/obj/**/*.nuspec"
///        |> File.deleteAll
///     )
///
module Operators =
    /// Add Include operator
    let inline (++) (x : IGlobbingPattern) pattern = x.And pattern

    /// Exclude operator
    let inline (--) (x : IGlobbingPattern) pattern = x.ButNot pattern

    /// Includes a single pattern and scans the files - !! x = AllFilesMatching x
    let inline (!!) x = GlobbingPattern.create x

[<RequireQualifiedAccess>]
[<System.Obsolete "use Fake.Core.Process and the ProcessUtils helpers instead.">]
module Tools =
    open Operators

    let private (@@) path1 (path2:string) = Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])

    /// Looks for a tool first in its default path, if not found the in ./packages/ and then
    /// in all subfolders of the root folder - returns the tool file name.
    [<System.Obsolete "use Fake.Core.Process and the ProcessUtils helpers instead. Example: `tryFindLocalTool \"TOOL\" \"tool\" [ \".\"; defaultPath ]`">]
    let findToolInSubPath (toolname:string) (defaultPath:string) =
        try
            let tools = !! (defaultPath @@ "/**/" @@ toolname)
            if  Seq.isEmpty tools then 
                let packages = !! ("./packages/**/" @@ toolname)
                if Seq.isEmpty packages then
                    let root = !! ("./**/" @@ toolname)
                    Seq.head root
                else
                    Seq.head packages
            else
                Seq.head tools
        with
        | _ -> defaultPath @@ toolname

    /// Looks for a tool in all subfolders - returns the folder where the tool was found
    /// or None if not found.
    [<System.Obsolete "use Fake.Core.Process and the ProcessUtils helpers instead. Example: `tryFindLocalTool \"TOOL\" \"tool\" [ \".\"; defaultPath ]`">]
    let tryFindToolFolderInSubPath toolname =
        !! ("./**/" @@ toolname)
        |> Seq.tryHead
        |> Option.map Path.GetDirectoryName

    /// Looks for a tool in all subfolders - returns the folder where the tool was found.
    [<System.Obsolete "use Fake.Core.Process and the ProcessUtils helpers instead. Example: `tryFindLocalTool \"TOOL\" \"tool\" [ \".\"; defaultPath ]`">]
    let findToolFolderInSubPath toolname defaultPath =
        toolname
        |> tryFindToolFolderInSubPath 
        |> Option.defaultValue defaultPath
