[<AutoOpen>]
/// This module contains functions which allow to read and write environment variables
module Fake.EnvironmentHelper

open System
open System.IO
open System.Configuration
open System.Diagnostics
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions
open Microsoft.Win32

/// Type alias for System.EnvironmentVariableTarget
type EnvironTarget = EnvironmentVariableTarget

/// Retrieves the environment variable with the given name
let environVar name = Environment.GetEnvironmentVariable name

/// Combines two path strings using Path.Combine
let inline combinePaths path1 (path2:string) = Path.Combine(path1,path2.TrimStart [|'\\'|])

/// Combines two path strings using Path.Combine
let inline (@@) path1 path2 = combinePaths path1 path2

/// Retrieves all environment variables from the given target
let environVars target = 
  [for e in Environment.GetEnvironmentVariables target ->
     let e1 = e :?> Collections.DictionaryEntry
     e1.Key,e1.Value]

/// Sets the environment variable with the given name
let setEnvironVar name value = Environment.SetEnvironmentVariable(name,value) 

/// Retrieves the environment variable with the given name or returns the default if no value was set
let environVarOrDefault name defaultValue =
    let var = environVar name
    if String.IsNullOrEmpty var then defaultValue else var

/// Retrieves the environment variable or None
let environVarOrNone name =
    let var = environVar name
    if String.IsNullOrEmpty var  then None else Some var

/// Retrieves the application settings variable with the given name
let appSetting (name:string) = ConfigurationManager.AppSettings.[name]

/// Returns if the build parameter with the given name was set
let inline hasBuildParam name = environVar name <> null

/// Returns the value of the build parameter with the given name was set if it was set and otherwise the given default value
let inline getBuildParamOrDefault name defaultParam = if hasBuildParam name then environVar name else defaultParam

/// Returns the value of the build parameter with the given name if it was set and otherwise an empty string
let inline getBuildParam name = getBuildParamOrDefault name String.Empty

/// The path of the "Program Files" folder - might be x64 on x64 machine
let ProgramFiles = Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles

/// The path of Program Files (x86)
/// It seems this covers all cases where PROCESSOR_ARCHITECTURE may misreport and the case where the other variable 
/// PROCESSOR_ARCHITEW6432 can be null
let ProgramFilesX86 =
    let wow64 = (environVar "PROCESSOR_ARCHITEW6432")
    let globalArch = (environVar "PROCESSOR_ARCHITECTURE")
    match wow64, globalArch with
    | "AMD64", "AMD64" | null, "AMD64" | "x86", "AMD64" ->
        environVar "ProgramFiles(x86)"
    | _ ->
        environVar "ProgramFiles"

/// The system root environment variable. Typically "C:\Windows"
let SystemRoot = environVar "SystemRoot"

/// Detemernines if the current system is an Unix system
let isUnix = Environment.OSVersion.Platform = PlatformID.Unix

/// Modifies the ProcessStartInfo according to the platform semantics
let platformInfoAction (psi:ProcessStartInfo) =
    if isUnix && psi.FileName.EndsWith ".exe" then
        psi.Arguments <- psi.FileName + " " + psi.Arguments
        psi.FileName <- "mono"  

/// The path of the current target platform
let mutable TargetPlatformPrefix = 
    let (<|>) a b = match a with None -> b | _ -> a
    environVarOrNone "FrameworkDir32"
    <|> 
        if (String.IsNullOrEmpty SystemRoot) then None
        else Some (SystemRoot @@ @"Microsoft.NET\Framework")
    <|> 
        if (isUnix) then Some "/usr/lib/mono"
        else Some @"C:\Windows\Microsoft.NET\Framework"
    |> Option.get
    

/// Gets the local directory for the given target platform
let getTargetPlatformDir platformVersion = 
    if Directory.Exists(TargetPlatformPrefix + "64") then 
        (TargetPlatformPrefix + "64") @@ platformVersion
    else 
        TargetPlatformPrefix @@ platformVersion

/// The path to the personal documents
let documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)

/// The directory separator string. On most systems / or \
let directorySeparator = Path.DirectorySeparatorChar.ToString()

/// Convert the given windows path to a path in the current system
let convertWindowsToCurrentPath (w:string) = 
    if (w.Length > 2 && w.[1] = ':' && w.[2] = '\\') then
        w
    else
        w.Replace(@"\",directorySeparator)

/// The IO encoding from build parameter
let encoding =
    match getBuildParamOrDefault "encoding" "default" with
    | "default" -> Text.Encoding.Default
    | enc -> Text.Encoding.GetEncoding(enc)

/// Rteurns a sequence with all installed .NET framework versions
let getInstalledDotNetFrameworks() = 
    let frameworks = new ResizeArray<_>()
    try
        let matches = 
            Registry
              .LocalMachine
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

/// A record which allows to display lots of machine specific information
type MachineDetails = {
    ProcessorCount : int
    Is64bit : bool
    OperatingSystem : string
    MachineName : string
    NETFrameworks : seq<string>
    UserDomainName : string
}

/// Retrieves lots of machine specific information
let getMachineEnvironment() = 
     {
        ProcessorCount = Environment.ProcessorCount
        Is64bit = Environment.Is64BitOperatingSystem
        OperatingSystem = Environment.OSVersion.ToString()
        MachineName = Environment.MachineName
        NETFrameworks = getInstalledDotNetFrameworks()
        UserDomainName = Environment.UserDomainName
     }  
     
