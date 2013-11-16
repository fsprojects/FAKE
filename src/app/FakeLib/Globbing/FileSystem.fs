// Copied from https://github.com/colinbull/FSharp.Enterprise/blob/master/src/FSharp.Enterprise/FileSystem.fs
/// Experimental Globbing - be careful we will break this.
module Fake.FileSystem
    
open System
open System.Collections.Generic
open System.IO
open Fake

type private SearchOption = 
| Directory of string
| Recursive
| FilePattern of string
        
let inline private exists dir root = 
    let di = new DirectoryInfo(Path.Combine(root, dir))
    if di.Exists then Some di.FullName else None
        
let rec private buildPaths acc (input : SearchOption list) =
    match input with
    | [] -> acc
    | Directory(name) :: t -> 
        match List.tryPick (exists name) acc with
        | Some(dir) -> buildPaths [dir] t 
        | None -> [] 
    | Recursive :: t ->
        let dirs = 
            Seq.collect (fun dir -> Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories)) acc
            |> Seq.toList
        buildPaths (acc @ dirs) t
    | FilePattern(pattern) :: t ->
        Seq.collect (fun dir -> Directory.EnumerateFiles(dir, pattern)) acc
        |> Seq.toList
         
let private search (baseDir:string) (input : string) =
    let input = input.Replace(baseDir,"")
    let filePattern = Path.GetFileName(input)    
    input.Split([|'/';'\\'|], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map (function
                | "**" -> Recursive
                | a when a = filePattern -> FilePattern(a)
                | a -> Directory(a))
    |> Seq.toList
    |> buildPaths [baseDir]

/// Internal representation of a file set
type FileIncludes =
  { BaseDirectory: string
    Includes: string list
    Excludes: string list }

  /// Adds the given pattern to the file includes
  member this.And pattern = { this with Includes = pattern::this.Includes}

  /// Ignores files with the given pattern
  member this.ButNot pattern = { this with Excludes = pattern::this.Excludes}

  /// Sets a directory as BaseDirectory.
  member this.SetBaseDirectory(dir:string) = {this with BaseDirectory = dir.TrimEnd(directorySeparator.[0])}

  interface IEnumerable<string> with 
    member this.GetEnumerator() =
        let hashSet = HashSet()
        let excludes = 
            seq { for pattern in this.Excludes do
                    yield! search this.BaseDirectory pattern }
            |> Set.ofSeq

        let files = 
            seq { for pattern in List.rev this.Includes do
                    yield! search this.BaseDirectory pattern }
            |> Seq.filter (fun x -> not(Set.contains x excludes))
            |> Seq.filter (fun x -> hashSet.Add x)
        
        files.GetEnumerator()
                 
    member this.GetEnumerator() = (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

/// Include files
let Include x = { BaseDirectory = DefaultBaseDir; Includes = [x]; Excludes = []}

/// Sets a directory as baseDirectory for fileIncludes. 
let SetBaseDir (dir:string) (fileIncludes:FileIncludes) = fileIncludes.SetBaseDirectory dir
/// Add Include operator
let inline (++) (x:FileIncludes) pattern = x.And pattern

/// Exclude operator
let inline (--) (x:FileIncludes) pattern = x.ButNot pattern

/// Includes a single pattern and scans the files - !! x = AllFilesMatching x
let inline (!!) x = Include x

/// Include prefix operator
[<Obsolete("!+ is obsolete - use !! instead")>]
let inline (!+) x = Include x