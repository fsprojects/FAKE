[<AutoOpen>]
module Fake.EnvironmentHelper

open System
open System.IO
open System.Configuration

type EnvironTarget = EnvironmentVariableTarget

/// Retrieves the EnvironmentVariable
let environVar = Environment.GetEnvironmentVariable

/// Gets the current directory
let currentDirectory = Path.GetFullPath "."

/// Combines to path strings
let inline (@@) path1 path2 = Path.Combine(path1,path2)

/// Retrieves the EnvironmentVariable
let environVars target = 
  [for e in Environment.GetEnvironmentVariables target ->
     let e1 = e :?> Collections.DictionaryEntry
     e1.Key,e1.Value]

/// Retrieves a ApplicationSettings variable
let appSetting (name:string) = ConfigurationManager.AppSettings.[name]

/// Returns true if the buildParam is set and otherwise false
let inline hasBuildParam name = environVar name <> null

/// Returns the value of the buildParam if it is set and otherwise "" 
let inline getBuildParam name = if hasBuildParam name then environVar name else String.Empty

/// Returns the value of the buildParam if it is set and otherwise the default
let inline getBuildParamOrDefault name defaultParam = if hasBuildParam name then getBuildParam name else defaultParam

/// The path of Program Files - might be x64 on x64 machine
let ProgramFiles = Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles

/// The path of Program Files (x86)
let ProgramFilesX86 =
    let a = environVar "PROCESSOR_ARCHITEW6432"
    if 8 = IntPtr.Size || (a <> null && a <> "") then
        environVar "ProgramFiles(x86)"
    else
        environVar "ProgramFiles"

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