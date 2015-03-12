/// This module contains a file pattern globbing implementation.
module Fake.Globbing

open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions

type private SearchOption = 
    | Directory of string
    | Drive of string
    | Recursive
    | FilePattern of string

let private checkSubDirs absolute (dir : string) root = 
    if dir.Contains "*" then Directory.EnumerateDirectories(root, dir, SearchOption.TopDirectoryOnly) |> Seq.toList
    else 
        let path = Path.Combine(root, dir)
        
        let di = 
            if absolute then new DirectoryInfo(dir)
            else new DirectoryInfo(path)
        if di.Exists then [ di.FullName ]
        else []

let rec private buildPaths acc (input : SearchOption list) = 
    match input with
    | [] -> acc
    | Directory(name) :: t -> 
        let subDirs = 
            acc
            |> List.map (checkSubDirs false name)
            |> List.concat
        buildPaths subDirs t
    | Drive(name) :: t -> 
        let subDirs = 
            acc
            |> List.map (checkSubDirs true name)
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
         Seq.collect (fun dir -> 
                            if Directory.Exists(Path.Combine(dir, pattern))
                            then seq { yield Path.Combine(dir, pattern) }
                            else Directory.EnumerateFiles(dir, pattern)) acc |> Seq.toList

let private isDrive = 
    let regex = Regex(@"^[A-Za-z]:$", RegexOptions.Compiled)
    fun dir -> regex.IsMatch dir

let inline private normalizePath (p : string) = 
    p.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)
let inline private normalizeOutputPath (p : string) = 
    p.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)
     .TrimEnd(Path.DirectorySeparatorChar)

let internal search (baseDir : string) (input : string) = 
    let baseDir = normalizePath baseDir
    let input = normalizePath input
    let input = input.Replace(baseDir, "")

    let filePattern = Path.GetFileName(input)
    input.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map (function 
           | "**" -> Recursive
           | a when a = filePattern -> FilePattern(a)
           | a when isDrive a -> Directory(a + "\\")
           | a -> Directory(a))
    |> Seq.toList
    |> buildPaths [ baseDir ]
    |> List.map normalizeOutputPath