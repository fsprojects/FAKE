/// This module contains a file pattern globbing implementation.
namespace Fake.Core
open System.Collections.Generic

type IGlobbingPattern =
    inherit IEnumerable<string>
    abstract BaseDirectory : string
    abstract Includes : string list
    abstract Excludes : string list

namespace Fake.Core.Globbing
open Fake.Core
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

namespace Fake.Core
open System.IO
open Fake.Core.Globbing

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
            let fullPath = fullDir path
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
    let Include x = 
        { BaseDirectory = defaultBaseDir
          Includes = [ x ]
          Excludes = [] } :> IGlobbingPattern

    /// Sets a directory as baseDirectory for fileIncludes. 
    let SetBaseDir (dir : string) (fileIncludes : IGlobbingPattern) = fileIncludes.SetBaseDirectory dir



namespace Fake.Core.Globbing

open Fake.Core
open System.IO

// Compat
[<System.Obsolete("Please use IGlobbingPattern instead")>]
type FileIncludes = IGlobbingPattern

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<System.Obsolete("Please use GlobbingPattern instead")>]
module FileIncludes =
    /// Include files
    [<System.Obsolete("Please use GlobbingPattern instead")>]
    let Include x = GlobbingPattern.Include x

    /// Sets a directory as baseDirectory for fileIncludes. 
    [<System.Obsolete("Please use GlobbingPattern instead")>]
    let SetBaseDir (dir : string) (fileIncludes : FileIncludes) = GlobbingPattern.SetBaseDir dir fileIncludes

module Operators =
    /// Add Include operator
    let inline (++) (x : IGlobbingPattern) pattern = x.And pattern

    /// Exclude operator
    let inline (--) (x : IGlobbingPattern) pattern = x.ButNot pattern

    /// Includes a single pattern and scans the files - !! x = AllFilesMatching x
    let inline (!!) x = GlobbingPattern.Include x

module Tools =
    open Operators

    let private (@@) path1 (path2:string) = Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])

    /// Looks for a tool first in its default path, if not found the in ./packages/ and then
    /// in all subfolders of the root folder - returns the tool file name.
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

    /// Looks for a tool in all subfolders - returns the folder where the tool was found.
    let findToolFolderInSubPath toolname defaultPath =
        try
            let tools = !! ("./**/" @@ toolname)
            if Seq.isEmpty tools then defaultPath
            else 
                let fi = FileInfo (Seq.head tools)
                fi.Directory.FullName
        with
        | _ -> defaultPath