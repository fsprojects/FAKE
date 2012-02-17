[<AutoOpen>]
module Fake.FileSystemHelper

open System
open System.IO

/// Creates a DirectoryInfo for the given path
let inline directoryInfo path = new DirectoryInfo(path)

/// Creates a FileInfo for the given path
let inline fileInfo path = new FileInfo(path)

/// Creates a FileInfo or a DirectoryInfo for the given path
let inline fileSystemInfo path : FileSystemInfo =
    if Directory.Exists path
        then upcast directoryInfo path
        else upcast fileInfo path

/// Converts a file to it's full file system name
let inline FullName fileName = Path.GetFullPath fileName

/// Gets the directory part of a filename
let inline DirectoryName fileName = Path.GetDirectoryName fileName

/// Gets all subdirectories
let inline subDirectories (dir:DirectoryInfo) = dir.GetDirectories()

/// Gets all files in the directory
let inline filesInDir (dir:DirectoryInfo) = dir.GetFiles()

/// Finds all the files in the directory matching the search pattern 
let filesInDirMatching pattern (dir:DirectoryInfo) =
    if dir.Exists then dir.GetFiles pattern else [||]

/// Gets the first file in the directory matching the search pattern or throws if nothing was found
let FindFirstMatchingFile pattern dir =
    let files = filesInDirMatching pattern dir
    if Seq.isEmpty files then
        new FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir.FullName)
          |> raise
    (Seq.head files).FullName

/// Gets the current directory
let currentDirectory = Path.GetFullPath "."

/// Checks if the file exists on disk.
let checkFileExists fileName =
    if not <| File.Exists fileName then failwithf "File %s does not exist." fileName

/// Checks if all given files exists
let allFilesExist files = Seq.forall File.Exists files

/// Checks if all given directory exists. If not then this functions creates the directory
let ensureDirectory dir = 
    if not <| Directory.Exists(dir) then 
      Directory.CreateDirectory(dir) |> ignore
        