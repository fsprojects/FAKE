/// This module contains a file pattern globbing implementation.
/// This module is part of the `Fake.IO.FileSystem` package
[<RequireQualifiedAccess>]
module Fake.IO.Globbing.Glob

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
    if dir.Contains "*" then 
        try Directory.EnumerateDirectories(root, dir, SearchOption.TopDirectoryOnly) |> Seq.toList
        with :? System.IO.DirectoryNotFoundException -> List.empty
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
    | Directory name :: t ->
        let subDirs = List.collect (checkSubDirs false name) acc
        buildPaths subDirs t
    | Drive name :: t ->
        let subDirs = List.collect (checkSubDirs true name) acc
        buildPaths subDirs t
    | Recursive :: [] ->
        let dirs =
            Seq.collect (fun dir -> 
                try Directory.EnumerateFileSystemEntries(dir, "*", SearchOption.AllDirectories)
                with :? System.IO.DirectoryNotFoundException -> Seq.empty) acc
        buildPaths (acc @ Seq.toList dirs) []
    | Recursive :: t ->
        let dirs =
            Seq.collect (fun dir -> 
                try Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories)
                with :? System.IO.DirectoryNotFoundException -> Seq.empty) acc
        buildPaths (acc @ Seq.toList dirs) t
    | FilePattern pattern :: _ ->
        acc |> List.collect (fun dir ->
            if Directory.Exists (Path.Combine (dir, pattern)) then [Path.Combine (dir, pattern)]
            else
                try
                    Directory.EnumerateFiles (dir, pattern) |> Seq.toList
                with
                | :? System.IO.DirectoryNotFoundException
                | :? System.IO.PathTooLongException -> [])

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

let internal search (baseDir : string) (originalInput : string) =
    let baseDir = normalizePath baseDir
    let input = normalizePath originalInput
    let input =
        if String.IsNullOrEmpty baseDir
        then input
        else
            // The final \ (or /) makes sure to only match complete folder names (as one folder name could be a substring of the other)
            let start = baseDir.TrimEnd([|Path.DirectorySeparatorChar|]) + string Path.DirectorySeparatorChar
            // See https://github.com/fsharp/FAKE/issues/1925
            if input.StartsWith start then
                input.Substring start.Length
            else input           

    let filePattern = Path.GetFileName(input)

    let splits = input.Split([| '/'; '\\' |], StringSplitOptions.None)
    let baseItems =
        let start, rest =
            if input.StartsWith "\\\\" && splits.Length >= 4 then
                let serverName = splits.[2]
                let share = splits.[3]
                [ Directory (sprintf "\\\\%s\\%s" serverName share) ], splits |> Seq.skip 4
            elif splits.Length >= 2 && Path.IsPathRooted input && driveRegex.IsMatch splits.[0] then
                [ Directory(splits.[0] + "\\") ], splits |> Seq.skip 1
            elif splits.Length >= 2 && Path.IsPathRooted input && input.StartsWith "/" then
                [ Directory("/") ], splits |> Array.toSeq
            else
                if Path.IsPathRooted input then
                    if input.StartsWith "\\"
                    then // https://github.com/fsharp/FAKE/issues/2073
                         failwithf "Please remove the leading '\\' or '/' and replace them with '.\\' or './' if you want to use a relative path. Leading slashes are considered an absolute path (input was '%s')!" originalInput
                    else failwithf "Unknown globbing input '%s', try to use a relative path and report an issue!" originalInput
                [], splits |> Array.toSeq
        let restList =
            rest    
            |> Seq.filter (String.IsNullOrEmpty >> not)
            |> Seq.map (function 
                   | "**" -> Recursive
                   | a when a = filePattern -> FilePattern(a)
                   | a -> Directory(a))
            |> Seq.toList
        start @ restList
    baseItems    
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
            ] |> List.map(fun (key, (pattern, replace)) ->
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
