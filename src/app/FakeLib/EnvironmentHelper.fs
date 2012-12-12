[<AutoOpen>]
module Fake.EnvironmentHelper

open System
open System.IO
open System.Configuration
open System.Diagnostics

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

/// Retrieves the environment variable or None
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
/// I think this covers all cases where PROCESSOR_ARCHITECTURE may misreport and the case where the other variable 
/// PROCESSOR_ARCHITEW6432 can be null
let ProgramFilesX86 =
    let wow64 = (environVar "PROCESSOR_ARCHITEW6432")
    let globalArch = (environVar "PROCESSOR_ARCHITECTURE")
    match wow64, globalArch with
    | "AMD64", "AMD64" | null, "AMD64" | "x86", "AMD64" ->
        environVar "ProgramFiles(x86)"
    | _ ->
        environVar "ProgramFiles"

/// System root environment variable. Typically "C:\Windows"
let SystemRoot = environVar "SystemRoot"

let isUnix = System.Environment.OSVersion.Platform = System.PlatformID.Unix

let platformInfoAction (psi:ProcessStartInfo) =
    if isUnix && psi.FileName.EndsWith ".exe" then
      psi.Arguments <- psi.FileName + " " + psi.Arguments
      psi.FileName <- "mono"  

let mutable TargetPlatformPrefix = 
    let (<|>) a b = match a with None -> b | _ -> a
    environVarOrNone "FrameworkDir32"
    <|> 
        if (isNullOrEmpty SystemRoot) then None
        else Some (SystemRoot @@ @"Microsoft.NET\Framework")
    <|> 
        if (isUnix) then Some "/usr/lib/mono"
        else Some @"C:\Windows\Microsoft.NET\Framework"
    |> Option.get
    

/// Gets the local directory for the given target platform
let getTargetPlatformDir platformVersion = 
    if Directory.Exists(TargetPlatformPrefix + "64") then 
        Path.Combine(TargetPlatformPrefix + "64",platformVersion) 
    else 
        Path.Combine(TargetPlatformPrefix,platformVersion)

/// The path to the personal documents
let documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)

/// Convert the given windows path to a path in the current system
let convertWindowsToCurrentPath (w:string) = 
    if (w.Length > 2 && w.[1] = ':' && w.[2] = '\\') then
        w
    else
        replace @"\" directorySeparator w        