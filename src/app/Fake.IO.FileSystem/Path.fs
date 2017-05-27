/// Contains helper function which allow to deal with files and directories.
module Fake.IO.FileSystem.Path

open Fake.Core
open Fake.Core.String.Operators
open System
open System.IO
open System.Collections.Generic

/// Combines two path strings using Path.Combine
let inline combineTrimEnd path1 (path2 : string) = Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])
/// Combines two path strings using Path.Combine
let inline combine path1 path2 = Path.Combine(path1, path2)

/// Detects whether the given path is a directory.
let isDirectory path = 
    let attr = File.GetAttributes path
    attr &&& FileAttributes.Directory = FileAttributes.Directory
    
/// Detects whether the given path is a file.
let isFile path = isDirectory path |> not

/// Normalizes a filename.
let normalizeFileName (fileName : string) = 
    fileName.Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())
            .TrimEnd(Path.DirectorySeparatorChar).ToLower()


/// Detects whether the given path does not contains invalid characters.
let isValidPath (path:string) =
    Path.GetInvalidPathChars()
    |> Array.exists (fun char -> path.Contains(char.ToString()))
    |> not

/// Change the extension of the file.
/// ## Parameters
///
/// - 'extension' - The new extension containing the leading '.'.
/// - 'fileName' - Name of the file from which the extension is retrieved.
let changeExtension extension fileName = Path.ChangeExtension(fileName, extension)

/// Tests whether the file has specified extensions (containing the leading '.')
/// ## Parameters
///
/// - 'extension' - The extension to fine containing the leading '.'.
/// - 'fileName' - Name of the file from which the extension is retrieved.
let hasExtension extension fileName = System.String.Equals(Path.GetExtension fileName, extension, System.StringComparison.OrdinalIgnoreCase)

/// Get the directory of the specified path
/// ## Parameters
///
/// - 'path' - The path from which the directory is retrieved.
let getDirectory path = Path.GetDirectoryName path

/// The directory separator string. On most systems / or \
let directorySeparator = Path.DirectorySeparatorChar.ToString()

let getFullName p = Path.GetFullPath p

/// Replaces any occurence of the currentDirectory with .
let inline shortenCurrentDirectory path = String.replace (Directory.GetCurrentDirectory()) "." path

/// <summary>Produces relative path when possible to go from baseLocation to targetLocation.</summary>
/// <param name="baseLocation">The root folder</param>
/// <param name="targetLocation">The target folder</param>
/// <returns>The relative path relative to baseLocation</returns>
/// <exception cref="ArgumentNullException">base or target locations are null or empty</exception>
let private ProduceRelativePath baseLocation targetLocation = 
    if String.isNullOrEmpty baseLocation then raise (new ArgumentNullException "baseLocation")
    if String.isNullOrEmpty targetLocation then raise (new ArgumentNullException "targetLocation")
    if not <| Path.IsPathRooted baseLocation then baseLocation
    else if not <| Path.IsPathRooted targetLocation then targetLocation
    else if String.Compare(Path.GetPathRoot baseLocation, Path.GetPathRoot targetLocation, true) <> 0 then 
        targetLocation
    else if String.Compare(baseLocation, targetLocation, true) = 0 then "."
    else 
        let resultPath = ref "."
        
        let targetLocation = 
            if targetLocation |> String.endsWith directorySeparator then targetLocation
            else targetLocation + directorySeparator
        
        let baseLocation = 
            if baseLocation |> String.endsWith directorySeparator then ref (baseLocation.Substring(0, baseLocation.Length - 1))
            else ref baseLocation
        
        while not <| targetLocation.StartsWith(!baseLocation + directorySeparator, StringComparison.OrdinalIgnoreCase) do
            resultPath := !resultPath + directorySeparator + ".."
            baseLocation := Path.GetDirectoryName !baseLocation
            if (!baseLocation) |> String.endsWith directorySeparator then 
                baseLocation := (!baseLocation).Substring(0, (!baseLocation).Length - 1)
        resultPath 
        := (!resultPath + targetLocation.Substring((!baseLocation).Length)) 
            |> String.replace (directorySeparator + directorySeparator) directorySeparator
        // preprocess .\..\ case
        if (sprintf ".%s..%s" directorySeparator directorySeparator) <* (!resultPath) then 
            (!resultPath).Substring(2, (!resultPath).Length - 3)
        else (!resultPath).Substring(0, (!resultPath).Length - 1)

let toRelativeFrom =
    /// A cache of relative path names.
    /// [omit]
    let relativePaths = new Dictionary<_, _>()

    /// Replaces the absolute path to a relative path.
    let inline toRelativePath basePath value =
        let key = (basePath, value)
        match relativePaths.TryGetValue key with
        | true, x -> x
        | _ -> 
            let x = ProduceRelativePath basePath value
            relativePaths.Add(key, x)
            x

    toRelativePath

let toRelativeFromCurrent path =
    let currentDir = normalizeFileName <| Directory.GetCurrentDirectory()
    toRelativeFrom currentDir path

/// Convert the given windows path to a path in the current system
let convertWindowsToCurrentPath (windowsPath : string) = 
    if (windowsPath.Length > 2 && windowsPath.[1] = ':' && windowsPath.[2] = '\\') then windowsPath
    else windowsPath.Replace(@"\", directorySeparator)