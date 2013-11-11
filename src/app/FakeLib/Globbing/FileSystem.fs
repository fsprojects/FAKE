// Copied from https://github.com/colinbull/FSharp.Enterprise/blob/master/src/FSharp.Enterprise/FileSystem.fs
/// Experimental Globbing - be careful we will break this.
module Fake.FileSystem
    
open System
open System.Collections.Generic
open System.IO
open Fake

type internal SearchOption = 
| Directory of string
| Recursive
| FilePattern of string
        
let internal exists dir root = 
    let di = new DirectoryInfo(Path.Combine(root, dir))
    if di.Exists
    then Some di.FullName
    else None
        
let rec internal buildPaths acc (input : SearchOption list) =
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
        
let getBaseDir = lazy (
    let assembly = Reflection.Assembly.GetEntryAssembly()
    if assembly = null then FullName "." else Path.GetDirectoryName assembly.Location)
         
let search baseDir (input : string) =
    let filePattern = Path.GetFileName(input)
    input.Split([|'/';'\\'|], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map (function
                | "**" -> Recursive
                | a when a = filePattern -> FilePattern(a)
                | a -> Directory(a))
    |> Seq.toList
    |> buildPaths [baseDir]

let find pattern = search (getBaseDir.Force()) pattern

/// Internal representation of a file set
type FileIncludes =
  { BaseDirectory: string
    Includes: string list
    Excludes: string list }

  interface IEnumerable<string> with 
    member this.GetEnumerator() =
        let files = 
            seq { for pattern in this.Includes do
                    yield! search this.BaseDirectory pattern }
        files.GetEnumerator()
                 
    member this.GetEnumerator() = (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

/// Include files
let Include x = { BaseDirectory = DefaultBaseDir; Includes = [x]; Excludes = []}

/// Sets a directory as baseDirectory for fileIncludes. 
let SetBaseDir (dir:string) (fileIncludes:FileIncludes)  = {fileIncludes with BaseDirectory = dir.TrimEnd(directorySeparator.[0])}
  
/// Add Include operator
let inline (++) (x:FileIncludes) pattern = { x with Includes = pattern :: x.Includes }

/// Exclude operator
let inline (--) (x:FileIncludes) pattern = { x with Excludes = pattern :: x.Excludes }

/// Includes a single pattern and scans the files - !! x = AllFilesMatching x
let inline (!!) x = Include x