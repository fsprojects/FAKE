[<AutoOpen>]
module Fake.FileSystemHelper

open System
open System.IO

/// Creates a DirectoryInfo for the given path
let inline directoryInfo path = new DirectoryInfo(path)

/// Creates a FileInfo for the given path
let inline fileInfo path = new FileInfo(path)

/// Converts a file to it's full file system name
let inline FullName fileName = Path.GetFullPath fileName

/// Gets all subdirectories
let inline subDirectories (dir:DirectoryInfo) = dir.GetDirectories()

/// Gets all files in the directory
let inline filesInDir (dir:DirectoryInfo) = dir.GetFiles()

/// Gets the current directory
let currentDirectory = Path.GetFullPath "."