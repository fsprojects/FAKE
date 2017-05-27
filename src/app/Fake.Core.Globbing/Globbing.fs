/// This module contains a file pattern globbing implementation.
module Fake.Core.Globbing.Glob

open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions


// Normalizes path for different OS
let inline normalizePath (path : string) = 
    path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)

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
                            else 
                                try
                                    Directory.EnumerateFiles(dir, pattern)
                                with
                                    | :? System.IO.PathTooLongException as ex ->
                                        Array.toSeq [| |]
                            ) acc |> Seq.toList

let private driveRegex = Regex(@"^[A-Za-z]:$", RegexOptions.Compiled)

let inline private normalizeOutputPath (p : string) = 
    p.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)
     .TrimEnd(Path.DirectorySeparatorChar)

let internal getRoot (baseDirectory : string) (pattern : string) =
    let baseDirectory = normalizePath baseDirectory
    let normPattern = normalizePath pattern

    let patternParts = normPattern.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
    let patternPathParts = 
        patternParts
        |> Seq.takeWhile(fun p -> not (p.Contains("*")))
        |> Seq.toArray

    let globRoot = 
        // If we did not find any "*", then drop the last bit (it is a file name, not a pattern)
        ( if patternPathParts.Length = patternParts.Length then
              patternPathParts.[0 .. patternPathParts.Length-2]     
          else patternPathParts )
        |> String.concat (Path.DirectorySeparatorChar.ToString())

    let globRoot = 
        // If we dropped "/" from the beginning of the path in the 'Split' call, put it back!
        if normPattern.StartsWith("/") then "/" + globRoot
        else globRoot

    if Path.IsPathRooted globRoot then globRoot
    else Path.Combine(baseDirectory, globRoot)

let internal search (baseDir : string) (input : string) = 
    let baseDir = normalizePath baseDir
    let input = normalizePath input
    let input = input.Replace(baseDir, "")

    let filePattern = Path.GetFileName(input)
    input.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map (function 
           | "**" -> Recursive
           | a when a = filePattern -> FilePattern(a)
           | a when driveRegex.IsMatch a -> Directory(a + "\\")
           | a -> Directory(a))
    |> Seq.toList
    |> buildPaths [ baseDir ]
    |> List.map normalizeOutputPath

let internal compileGlobToRegex pattern =
    let pattern = normalizePath pattern

    let escapedPattern = (Regex.Escape pattern)
    let regexPattern = 
        let xTOy = 
            [
                "dirwildcard", (@"\\\*\\\*(/|\\\\)", @"(.*(/|\\))?")
                "stardotstar", (@"\\\*\\.\\\*", @"([^\\/]*)")
                "wildcard", (@"\\\*", @"([^\\/]*)")
            ] |> List.map(fun (key, reg) ->
                let pattern, replace = reg
                let pattern = sprintf "(?<%s>%s)" key pattern
                key, (pattern, replace)
            )
        let xTOyMap = xTOy |> Map.ofList
        let replacePattern = xTOy |> List.map(fun x -> x |> snd |> fst) |> String.concat("|")
        let replaced = Regex(replacePattern).Replace(escapedPattern, fun m -> 
            let matched = xTOy |> Seq.map(fst) |> Seq.find(fun n -> 
                m.Groups.Item(n).Success
            )
            (xTOyMap |> Map.tryFind matched).Value |> snd
        )
        "^" + replaced + "$"

    Regex(regexPattern)

let private globRegexCache = System.Collections.Concurrent.ConcurrentDictionary<string, Regex>()

let isMatch pattern path : bool = 
    let path = normalizePath path

    let regex = 
        let outRegex : ref<Regex> = ref null
        if globRegexCache.TryGetValue(pattern, outRegex) then
            !outRegex
        else
            let compiled = compileGlobToRegex pattern
            globRegexCache.TryAdd(pattern, compiled) |> ignore
            compiled

    regex.IsMatch(path)
