[<AutoOpen>]
/// This module contains functions which allow to read and write environment variables and build parameters
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
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
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
type EnvironTarget = EnvironmentVariableTarget

/// Retrieves the environment variable with the given name
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let environVar name = Environment.GetEnvironmentVariable name

/// Combines two path strings using Path.Combine after removing leading slashes from the second path
[<System.Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem)")>]
let inline combinePaths path1 (path2 : string) = Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])
/// Combines two path strings using Path.Combine
[<System.Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem)")>]
let inline combinePathsNoTrim path1 path2 = Path.Combine(path1, path2)

/// Combines two path strings using Path.Combine after removing leading slashes from the second path
[<System.Obsolete("Use Fake.IO.FileSystemOperators instead (FAKE0001 - package: Fake.IO.FileSystem)")>]
let inline (@@) path1 path2 = combinePaths path1 path2
/// Combines two path strings using Path.Combine
[<System.Obsolete("Use Fake.IO.FileSystemOperators instead (FAKE0001 - package: Fake.IO.FileSystem)")>]
let inline (</>) path1 path2 = combinePathsNoTrim path1 path2

// Normalizes path for different OS
[<System.Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem)")>]
let inline normalizePath (path : string) =
    path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)

/// Retrieves all environment variables from the given target
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let environVars target =
    [ for e in Environment.GetEnvironmentVariables target ->
          let e1 = e :?> Collections.DictionaryEntry
          e1.Key, e1.Value ]

/// Sets the environment variable with the given name
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let setEnvironVar name value = Environment.SetEnvironmentVariable(name, value)

/// Sets the environment variable with the given name for the current user.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let setUserEnvironVar name value = Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User)

/// Sets the environment variable with the given name for the current machine.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let setMachineEnvironVar name value = Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Machine)

/// Sets the environment variable with the given name for the current process.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let setProcessEnvironVar name value = Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process)

/// Clears the environment variable with the given name for the current process.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let clearProcessEnvironVar name = Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.Process)

/// Sets the build parameter with the given name for the current process.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let setBuildParam name value = setProcessEnvironVar name value

/// Retrieves the environment variable with the given name or returns the default if no value was set
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let environVarOrDefault name defaultValue =
    let var = environVar name
    if String.IsNullOrEmpty var then defaultValue
    else var

/// Retrieves the environment variable with the given name or fails if not found
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let environVarOrFail name =
    let var = environVar name
    if String.IsNullOrEmpty var then failwith <| sprintf "Environment variable '%s' not found" name
    else var

/// Retrieves the environment variable with the given name or returns the default value if no value was set
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let getEnvironmentVarAsBoolOrDefault varName defaultValue =
    try
        (environVar varName).ToUpper() = "TRUE"
    with
    | _ ->  defaultValue

/// Retrieves the environment variable with the given name or returns false if no value was set
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let getEnvironmentVarAsBool varName = getEnvironmentVarAsBoolOrDefault varName false

/// Retrieves the environment variable or None if no value was set
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let environVarOrNone name =
    let var = environVar name
    if String.IsNullOrEmpty var then None
    else Some var

/// Splits the entries of an environment variable and removes the empty ones.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let splitEnvironVar name =
    let var = environVarOrNone name
    if var = None then [ ]
    else var.Value.Split([| Path.PathSeparator |]) |> Array.toList

/// Retrieves the application settings variable with the given name
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let appSetting (name : string) = ConfigurationManager.AppSettings.[name]

/// Returns if the build parameter with the given name was set
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let inline hasBuildParam name = environVar name <> null

/// Returns the value of the build parameter with the given name if it was set and otherwise the given default value
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let inline getBuildParamOrDefault name defaultParam =
    if hasBuildParam name then environVar name
    else defaultParam

/// Returns the value of the build parameter with the given name if it was set and otherwise an empty string
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let inline getBuildParam name = getBuildParamOrDefault name String.Empty

/// The path of the "Program Files" folder - might be x64 on x64 machine
/// It seems this covers all cases where PROCESSOR\_ARCHITECTURE may misreport and the case where the other variable
/// PROCESSOR\_ARCHITEW6432 can be null
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let ProgramFiles =
    let wow64 = environVar "PROCESSOR_ARCHITEW6432"
    let globalArch = environVar "PROCESSOR_ARCHITECTURE"
    match wow64, globalArch with
    | "AMD64", "AMD64"
    | null, "AMD64"
    | "x86", "AMD64"
    | "AMD64", "x86" ->
        RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion")
            .GetValue("ProgramFilesDir")
            .ToString()
    | _ -> environVar "ProgramFiles"
    |> fun detected -> if detected = null then @"C:\Program Files\" else detected

/// The path of Program Files (x86)
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let ProgramFilesX86 = Environment.GetFolderPath Environment.SpecialFolder.ProgramFilesX86

/// The system root environment variable. Typically "C:\Windows"
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let SystemRoot = environVar "SystemRoot"

/// Determines if the current system is a Windows system
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isWindows = Environment.OSVersion.Platform = PlatformID.Win32NT

/// Determines if the current system is an Unix system
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isUnix = Environment.OSVersion.Platform = PlatformID.Unix

/// Determines if the current system is a MacOs system
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isMacOS =
    (Environment.OSVersion.Platform = PlatformID.MacOSX) ||
      // Running on OSX with mono, Environment.OSVersion.Platform returns Unix
      // rather than MacOSX, so check for osascript (the AppleScript
      // interpreter). Checking for osascript for other platforms can cause a
      // problem on Windows if the current-directory is on a mapped-drive
      // pointed to a Mac's root partition; e.g., Parallels does this to give
      // Windows virtual machines access to files on the host.
      (Environment.OSVersion.Platform = PlatformID.Unix && (File.Exists "/usr/bin/osascript"))

/// Determines if the current system is a Linux system
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isLinux = (not isMacOS) && (int System.Environment.OSVersion.Platform |> fun p -> (p = 4) || (p = 6) || (p = 128))

/// Determines if the current system is a mono system
/// Todo: Detect mono on windows
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isMono = isLinux || isUnix || isMacOS

/// required sometimes to workaround mono crashes
/// http://stackoverflow.com/a/8414517/1269722
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let monoVersion =
    let t = Type.GetType("Mono.Runtime")
    if (not (isNull t)) then
        let displayNameMeth = t.GetMethod("GetDisplayName", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
        let displayName = displayNameMeth.Invoke(null, null).ToString()
        let pattern = new Regex("\d+(\.\d+)+")
        let m = pattern.Match(displayName)
        // NOTE: in System.Version 5.0 >= 5.0.0.0 is false while 5.0.0.0 >= 5.0 is true...
        let minimizeVersion (v:System.Version) =
            match v.Minor = 0, v.Revision = 0 with
            | true, true -> System.Version(v.Major, v.Minor)
            | _, true -> System.Version(v.Major, v.Minor, v.Build)
            | _ -> v
        let version =
            match System.Version.TryParse m.Value with
            | true, v -> Some (minimizeVersion v)
            | _ -> None
        Some (displayName, version)
    else None

[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let monoPath =
    if isMacOS && File.Exists "/Library/Frameworks/Mono.framework/Commands/mono" then
        "/Library/Frameworks/Mono.framework/Commands/mono"
    else
        "mono"

/// Arguments on the Mono executable
[<System.Obsolete("Use Fake.Core.Process instead (FAKE0001 - package: Fake.Core.Process)")>]
let mutable monoArguments = ""

/// Modifies the ProcessStartInfo according to the platform semantics
[<System.Obsolete("Use Fake.Core.Process instead (FAKE0001 - package: Fake.Core.Process)")>]
let platformInfoAction (psi : ProcessStartInfo) =
    if isMono && psi.FileName.EndsWith ".exe" then
        psi.Arguments <- monoArguments + " \"" + psi.FileName + "\" " + psi.Arguments
        psi.FileName <- monoPath

/// The path of the current target platform
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let mutable TargetPlatformPrefix =
    let (<|>) a b =
        match a with
        | None -> b
        | _ -> a
    environVarOrNone "FrameworkDir32" <|> if (String.IsNullOrEmpty SystemRoot) then None
                                          else Some(SystemRoot @@ @"Microsoft.NET\Framework")
    <|> if (isUnix) then Some "/usr/lib/mono"
        else Some @"C:\Windows\Microsoft.NET\Framework"
    |> Option.get

/// Base path for getting tools from Microsoft SDKs
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let msSdkBasePath = ProgramFilesX86 @@ "Microsoft SDKs"

/// Base path for getting tools from Windows SDKs
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let sdkBasePath = msSdkBasePath @@ "Windows"

/// Helper function to help find framework or sdk tools from the
/// newest toolkit available
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let getNewestTool possibleToolPaths =
       possibleToolPaths
       |> Seq.sortBy (fun p -> p)
       |> Array.ofSeq
       |> Array.rev
       |> Seq.ofArray
       |> Seq.head

/// Gets the local directory for the given target platform
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let getTargetPlatformDir platformVersion =
    if Directory.Exists(TargetPlatformPrefix + "64") then (TargetPlatformPrefix + "64") @@ platformVersion
    else TargetPlatformPrefix @@ platformVersion

/// The path to the personal documents
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)

/// The directory separator string. On most systems / or \
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let directorySeparator = Path.DirectorySeparatorChar.ToString()

/// Convert the given windows path to a path in the current system
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let convertWindowsToCurrentPath (windowsPath : string) =
    if (windowsPath.Length > 2 && windowsPath.[1] = ':' && windowsPath.[2] = '\\') then windowsPath
    else windowsPath.Replace(@"\", directorySeparator)

/// Contains the IO encoding which is given via build parameter "encoding" or the default encoding if no encoding was specified.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let encoding =
    match getBuildParamOrDefault "encoding" "default" with
    | "default" -> Text.Encoding.Default
    | enc -> Text.Encoding.GetEncoding(enc)

/// Returns a sequence with all installed .NET framework versions
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let getInstalledDotNetFrameworks() =
    let frameworks = new ResizeArray<_>()
    try
        let matches =
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP").GetSubKeyNames()
            |> Seq.filter (fun keyname -> Regex.IsMatch(keyname, @"^v\d"))
        for item in matches do
            match item with
            | "v4.0" -> ()
            | "v4" ->
                let key = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\" + item
                Registry.LocalMachine.OpenSubKey(key).GetSubKeyNames()
                |> Seq.iter (fun subkey ->
                       let key = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\" + item + @"\" + subkey
                       let version = Registry.LocalMachine.OpenSubKey(key).GetValue("Version").ToString()
                       frameworks.Add(String.Format("{0} ({1})", version, subkey)))
            | "v1.1.4322" -> frameworks.Add item
            | _ ->
                let key = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\" + item
                frameworks.Add(Registry.LocalMachine.OpenSubKey(key).GetValue("Version").ToString())
        frameworks :> seq<_>
    with e -> frameworks :> seq<_> //Probably a new unrecognisable version

/// A record which allows to display lots of machine specific information like machine name, processor count etc.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
type MachineDetails =
    { ProcessorCount : int
      Is64bit : bool
      OperatingSystem : string
      MachineName : string
      NETFrameworks : seq<string>
      UserDomainName : string
      AgentVersion : string
      DriveInfo : seq<string> }

/// Retrieves information about the hard drives
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let getDrivesInfo() =
    Environment.GetLogicalDrives()
    |> Seq.map (fun d -> IO.DriveInfo(d))
    |> Seq.filter (fun d -> d.IsReady)
    |> Seq.map
           (fun d ->
           sprintf "%s has %0.1fGB free of %0.1fGB" (d.Name.Replace(":\\", ""))
               (Convert.ToDouble(d.TotalFreeSpace) / (1024. * 1024. * 1024.))
               (Convert.ToDouble(d.TotalSize) / (1024. * 1024. * 1024.)))

/// Retrieves lots of machine specific information like machine name, processor count etc.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let getMachineEnvironment() =
    { ProcessorCount = Environment.ProcessorCount
      Is64bit = Environment.Is64BitOperatingSystem
      OperatingSystem = Environment.OSVersion.ToString()
      MachineName = Environment.MachineName
      NETFrameworks = getInstalledDotNetFrameworks()
      UserDomainName = Environment.UserDomainName
      AgentVersion =
          sprintf "%A" ((System.Reflection.Assembly.GetAssembly(typedefof<MachineDetails>)).GetName().Version)
      DriveInfo = getDrivesInfo() }
