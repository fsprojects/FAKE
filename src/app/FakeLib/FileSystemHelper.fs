[<AutoOpen>]
module Fake.FileSystemHelper

open System
open System.IO

/// <summary>Creates a DirectoryInfo for the given path</summary>
/// <user/>
let inline directoryInfo path = new DirectoryInfo(path)

/// <summary>Creates a FileInfo for the given path</summary>
/// <user/>
let inline fileInfo path = new FileInfo(path)

/// <summary>Creates a FileInfo or a DirectoryInfo for the given path</summary>
/// <user/>
let inline fileSystemInfo path : FileSystemInfo =
    if Directory.Exists path
        then upcast directoryInfo path
        else upcast fileInfo path

/// <summary>Converts a filename to it's full file system name</summary>
/// <user/>
let inline FullName fileName = Path.GetFullPath fileName

/// <summary>Gets the directory part of a filename</summary>
/// <user/>
let inline DirectoryName fileName = Path.GetDirectoryName fileName

/// Gets all subdirectories
let inline subDirectories (dir:DirectoryInfo) = dir.GetDirectories()

/// Gets all files in the directory
let inline filesInDir (dir:DirectoryInfo) = dir.GetFiles()

/// Finds all the files in the directory matching the search pattern 
let filesInDirMatching pattern (dir:DirectoryInfo) =
    if dir.Exists then dir.GetFiles pattern else [||]

/// Gets the first file in the directory matching the search pattern or None
let TryFindFirstMatchingFile pattern dir =
    dir 
    |> directoryInfo
    |> filesInDirMatching pattern
    |> fun files -> if Seq.isEmpty files then None else (Seq.head files).FullName |> Some

/// Gets the first file in the directory matching the search pattern or throws if nothing was found
let FindFirstMatchingFile pattern dir =
    match TryFindFirstMatchingFile pattern dir with
    | Some x -> x
    | None -> 
        new FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir)
          |> raise

/// <summary>Gets the current directory</summary>
/// <user/>
let currentDirectory = Path.GetFullPath "."

/// <summary>Checks if the file exists on disk.</summary>
/// <user/>
let fileExists = File.Exists

/// <summary>Raises an exception if the file doesn't exist on disk.</summary>
/// <user/>
let checkFileExists fileName =
    if not <| fileExists fileName then 
        failwithf "File %s does not exist." fileName

/// Checks if all given files exist
let allFilesExist files = Seq.forall fileExists files

/// <summary>Checks if the directory exists on disk.</summary>
/// <user/>
let directoryExists = Directory.Exists

/// Checks if the given directory exists. If not then this functions creates the directory
let ensureDirectory dir = 
    if not <| directoryExists dir then 
      Directory.CreateDirectory dir |> ignore
        