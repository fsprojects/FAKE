[<AutoOpen>]
module Fake.EnvironmentHelper

open System
open System.IO
open System.Configuration

type EnvironTarget = EnvironmentVariableTarget

/// Retrieves the EnvironmentVariable
let environVar = Environment.GetEnvironmentVariable

/// Combines two path strings
let inline combinePaths path1 (path2:string) = Path.Combine(path1,path2.TrimStart [|'\\'|])

/// Combines two path strings
let inline (@@) path1 path2 = combinePaths path1 path2

/// Retrieves the EnvironmentVariable
let environVars target = 
  [for e in Environment.GetEnvironmentVariables target ->
     let e1 = e :?> Collections.DictionaryEntry
     e1.Key,e1.Value]

/// Sets the Environment variable
let setEnvironVar environVar value = Environment.SetEnvironmentVariable(environVar,value) 

/// Retrieves the EnvironmentVariable or a default
let environVarOrDefault name defaultValue =
    let var = environVar name
    if isNullOrEmpty var  then defaultValue else var

let environVarOrNone name =
    let var = environVar name
    if isNullOrEmpty var  then None else Some var

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

let SystemRoot = environVar "SystemRoot"

let mutable TargetPlatformPrefix = 
    let (<|>) a b = match a with None -> b | _ -> a
    environVarOrNone "FrameworkDir32"
    <|> Some (SystemRoot @@ @"Microsoft.NET\Framework")
    <|> Some @"C:\Windows\Microsoft.NET\Framework"
    |> Option.get
    

/// Gets the local directory for the given target platform
let getTargetPlatformDir platformVersion = 
    if Directory.Exists(TargetPlatformPrefix + "64") then 
        Path.Combine(TargetPlatformPrefix + "64",platformVersion) 
    else 
        Path.Combine(TargetPlatformPrefix,platformVersion)

/// The path to the personal documents
let documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)

let isUnix = System.Environment.OSVersion.Platform = System.PlatformID.Unix
