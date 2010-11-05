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

/// Gets all subdirectories
let inline subDirectories (dir:DirectoryInfo) = dir.GetDirectories()

/// Gets all files in the directory
let inline filesInDir (dir:DirectoryInfo) = dir.GetFiles()

/// Gets the current directory
let currentDirectory = Path.GetFullPath "."

/// Checks if the file exists on disk.
let checkFileExists fileName =
    if not <| File.Exists fileName then failwithf "File %s does not exist." fileName

/// Checks if all given files exists
let allFilesExist files = Seq.forall File.Exists files