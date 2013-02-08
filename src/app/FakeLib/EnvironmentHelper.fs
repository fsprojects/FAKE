[<AutoOpen>]
module Fake.EnvironmentHelper

open System
open System.IO
open System.Configuration
open System.Diagnostics
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions
open Microsoft.Win32

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

/// Detemernines if the current system is Unix system
let isUnix = Environment.OSVersion.Platform = PlatformID.Unix

let platformInfoAction (psi:ProcessStartInfo) =
    if isUnix && psi.FileName.EndsWith ".exe" then
        psi.Arguments <- psi.FileName + " " + psi.Arguments
        psi.FileName <- "mono"  

/// The path of the current target platform
let mutable TargetPlatformPrefix =
    match environVarOrNone "FrameworkDir32" with
    | Some path -> path
    | _ ->
        if not (isNullOrEmpty SystemRoot) then SystemRoot @@ @"Microsoft.NET\Framework" else
        if isUnix then "/usr/lib/mono" else 
        @"C:\Windows\Microsoft.NET\Framework" 

/// Gets the local directory for the given target platform
let getTargetPlatformDir platformVersion = 
    if Directory.Exists(TargetPlatformPrefix + "64") then 
        (TargetPlatformPrefix + "64") @@ platformVersion
    else 
        TargetPlatformPrefix @@ platformVersion

/// The path to the personal documents
let documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)

/// Convert the given windows path to a path in the current system
let convertWindowsToCurrentPath (w:string) = 
    if (w.Length > 2 && w.[1] = ':' && w.[2] = '\\') then
        w
    else
        replace @"\" directorySeparator w


let getInstalledDotNetFrameworks() = 
    let frameworks = new ResizeArray<_>()
    try
        let matches = 
            Registry.LocalMachine
                    .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP")
                    .GetSubKeyNames()
            |> Seq.filter (fun keyname -> Regex.IsMatch(keyname, @"^v\d"))

        for item in matches do
            match item with
            | "v4.0" -> ()
            | "v4" ->
                let key = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\" + item
                Registry.LocalMachine.OpenSubKey(key).GetSubKeyNames()
                |> Seq.iter (fun subkey -> 
                                let key = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\" + item + @"\" + subkey
                                let version = Registry.LocalMachine.OpenSubKey(key).GetValue("Version").ToString();
                                frameworks.Add(String.Format("{0} ({1})", version, subkey)))
            | "v1.1.4322" -> frameworks.Add item
            | _ ->
                    let key = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\" + item;
                    frameworks.Add(Registry.LocalMachine.OpenSubKey(key).GetValue("Version").ToString());
        frameworks :> seq<_>
    with e ->
        frameworks :> seq<_> //Probably a new unrecognisable version

type MachineDetails = {
    ProcessorCount : int
    Is64bit : bool
    OperatingSystem : string
    MachineName : string
    NETFrameworks : seq<string>
    UserDomainName : string

}

let getMachineEnvironment() = 
     {
        ProcessorCount = Environment.ProcessorCount
        Is64bit = Environment.Is64BitOperatingSystem
        OperatingSystem = Environment.OSVersion.ToString()
        MachineName = Environment.MachineName
        NETFrameworks = getInstalledDotNetFrameworks()
        UserDomainName = Environment.UserDomainName
     }  
     
