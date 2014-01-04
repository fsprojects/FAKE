/// This module contains a file pattern globbing implementation.
[<AutoOpen>]
module Fake.FileSystem
    
open System
open System.Collections.Generic
open System.IO
open Fake
open System.Text.RegularExpressions

type private SearchOption = 
| Directory of string
| Recursive
| FilePattern of string
        
let private checkSubDirs (dir:string) root =
    if dir.Contains "*" then
        Directory.EnumerateDirectories(root, dir, SearchOption.TopDirectoryOnly) |> Seq.toList
    else
        let di = new DirectoryInfo(Path.Combine(root, dir))
        if di.Exists then [di.FullName] else []        
        
let rec private buildPaths acc (input : SearchOption list) =
    match input with
    | [] -> acc
    | Directory(name) :: t -> 
        let subDirs = 
            acc
            |> List.map (checkSubDirs name) 
            |> List.concat
        buildPaths subDirs t
    | Recursive :: [] ->
        let dirs = 
            Seq.collect (fun dir -> Directory.EnumerateFileSystemEntries(dir, "*", SearchOption.AllDirectories)) acc
            |> Seq.toList
        buildPaths (acc @ dirs) []
    | Recursive :: t ->
        let dirs = 
            Seq.collect (fun dir -> Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories)) acc
            |> Seq.toList
        buildPaths (acc @ dirs) t
    | FilePattern(pattern) :: t ->
        Seq.collect (fun dir -> Directory.EnumerateFiles(dir, pattern)) acc
        |> Seq.toList
         
let private isDrive =
    let regex = Regex(@"^[A-Za-z]:$", RegexOptions.Compiled)
    fun dir -> regex.IsMatch dir

let inline private normalizePath (p:string) = p.Replace('\\',Path.DirectorySeparatorChar).Replace('/',Path.DirectorySeparatorChar)

let private search (baseDir:string) (input : string) =
    let baseDir = normalizePath baseDir
    let input = normalizePath input    
    let input = input.Replace(baseDir,"")
    let filePattern = Path.GetFileName(input)    
    input.Split([|'/';'\\'|], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map (function
                | "**" -> Recursive
                | a when a = filePattern -> FilePattern(a)
                | a when isDrive a -> Directory (a + directorySeparator)
                | a -> Directory(a))
    |> Seq.toList
    |> buildPaths [baseDir]
    |> List.map normalizePath

/// Internal representation of a file set.
type FileIncludes =
  { BaseDirectory: string
    Includes: string list
    Excludes: string list }

  /// Adds the given pattern to the file includes
  member this.And pattern = { this with Includes = this.Includes @ [pattern]}

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
            seq { for pattern in this.Includes do
                    yield! search this.BaseDirectory pattern }
            |> Seq.filter (fun x -> not(Set.contains x excludes))
            |> Seq.filter (fun x -> hashSet.Add x)
        
        files.GetEnumerator()
                 
    member this.GetEnumerator() = (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

let private defaultBaseDir = Path.GetFullPath "."

/// Logs the given files with the message.
let Log message files = files |> Seq.iter (log << sprintf "%s%s" message)

/// Include files
let Include x = { BaseDirectory = defaultBaseDir; Includes = [x]; Excludes = []}

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

/// Looks for a tool in all subfolders - returns the tool file name.
let findToolInSubPath toolname defaultPath =
    let tools = !! ("./**/" @@ toolname) 
    if Seq.isEmpty tools then defaultPath @@ toolname else Seq.head tools

/// Looks for a tool in all subfolders - returns the folder where the tool was found.
let findToolFolderInSubPath toolname defaultPath =
    let tools = !! ("./**/" @@ toolname) 
    if Seq.isEmpty tools then defaultPath else 
    let fi = fileInfo (Seq.head tools)
    fi.Directory.FullName

/// Includes a single pattern and scans the files - !! x = AllFilesMatching x
[<Obsolete>]
let AllFilesMatching x = Include x

/// Lazy scan for include files.
/// Will be processed at the time when needed.
[<Obsolete("FileIncludes implement IEnumerable<string> so explicit scanning is not needed")>]
let Scan files = files

/// Scans immediately for include files - all matching files will be memoized.
[<Obsolete("FileIncludes implement IEnumerable<string> so explicit scanning is not needed. Just use Seq.toList")>]
let ScanImmediately includes = includes |> Seq.toList
