/// This module contains functions which allow to read and write environment variables and build parameters
namespace Fake.SystemHelper

#if DOTNETCORE
module Environment =
    type Environment = System.Environment

    type SpecialFolder =
        | ApplicationData
        | UserProfile
        | LocalApplicationData
        | ProgramFiles
        | ProgramFilesX86
    let GetFolderPath sf =
        let envVar =
            match sf with
            | ApplicationData -> "APPDATA"
            | UserProfile -> "USERPROFILE"
            | LocalApplicationData -> "LocalAppData"
            | ProgramFiles -> "PROGRAMFILES"
            | ProgramFilesX86 -> "PROGRAMFILES(X86)"
        
        let res = Environment.GetEnvironmentVariable(envVar)
        if System.String.IsNullOrEmpty res && sf = UserProfile then
            Environment.GetEnvironmentVariable("HOME")
        else res
#endif

namespace Fake.Core

module Environment =
    type Environment = System.Environment
#if DOTNETCORE
    open Fake.SystemHelper
#endif

    open System
    open System.IO
    open System.Diagnostics
    open System.Collections.Generic
    open System.Text
    open System.Text.RegularExpressions
    open Microsoft.Win32

    /// Type alias for System.EnvironmentVariableTarget
    #if !DOTNETCORE
    type EnvironTarget = EnvironmentVariableTarget
    #endif

    /// Retrieves the environment variable with the given name
    let environVar name = Environment.GetEnvironmentVariable name

    /// Retrieves all environment variables from the given target
    let environVars () = 
        let vars = Environment.GetEnvironmentVariables ()
        [ for e in vars -> 
              let e1 = e :?> Collections.DictionaryEntry
              e1.Key, e1.Value ]

    #if !DOTNETCORE
    [<Obsolete("Will be removed in dotnetcore. Use environVars instead.")>]
    let environVarsWithMode mode = 
        let vars = Environment.GetEnvironmentVariables (mode)
        [ for e in vars -> 
              let e1 = e :?> Collections.DictionaryEntry
              e1.Key, e1.Value ]
    #endif

    /// Sets the environment variable with the given name
    let setEnvironVar name value = Environment.SetEnvironmentVariable(name, value)

    /// Clears the environment variable with the given name for the current process.
    let clearEnvironVar name = Environment.SetEnvironmentVariable(name, null)

    [<Obsolete("Use setEnvironVar instead")>]
    /// Sets the build parameter with the given name for the current process.
    let setBuildParam name value = setEnvironVar name value

    /// Retrieves the environment variable with the given name or returns the default if no value was set
    let environVarOrDefault name defaultValue = 
        let var = environVar name
        if String.IsNullOrEmpty var then defaultValue
        else var

    /// Retrieves the environment variable with the given name or fails if not found
    let environVarOrFail name = 
        let var = environVar name
        if String.IsNullOrEmpty var then failwith <| sprintf "Environment variable '%s' not found" name
        else var

    /// Retrieves the environment variable with the given name or returns the default bool if no value was set
    let environVarAsBoolOrDefault varName defaultValue =
        try  
            (environVar varName).ToUpper() = "TRUE" 
        with
        | _ ->  defaultValue

    /// Retrieves the environment variable with the given name or returns the false if no value was set
    let environVarVarAsBool varName = environVarAsBoolOrDefault varName false

    /// Retrieves the environment variable or None
    let environVarOrNone name = 
        let var = environVar name
        if String.IsNullOrEmpty var then None
        else Some var

    /// Splits the entries of an environment variable and removes the empty ones.
    let splitEnvironVar name =
        let var = environVarOrNone name
        if var = None then [ ]
        else var.Value.Split([| Path.PathSeparator |]) |> Array.toList

    /// Returns if the build parameter with the given name was set
    let inline hasEnvironVar name = not (isNull (environVar name))

    [<Obsolete("Use hasEnvironVar instead")>]
    /// Returns if the build parameter with the given name was set
    let inline hasBuildParam name = hasEnvironVar name

    [<Obsolete("Use environVarOrDefault instead")>]
    /// Returns the value of the build parameter with the given name if it was set and otherwise the given default value
    let inline getBuildParamOrDefault name defaultParam = 
        if hasBuildParam name then environVar name
        else defaultParam

    [<Obsolete("Use 'environVarOrDefault name String.Empty' instead")>]
    /// Returns the value of the build parameter with the given name if it was set and otherwise an empty string
    let inline getBuildParam name = getBuildParamOrDefault name String.Empty

    /// The path of the "Program Files" folder - might be x64 on x64 machine
    let ProgramFiles = Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles

    /// The path of Program Files (x86)
    /// It seems this covers all cases where PROCESSOR\_ARCHITECTURE may misreport and the case where the other variable 
    /// PROCESSOR\_ARCHITEW6432 can be null
    let ProgramFilesX86 = 
        let wow64 = environVar "PROCESSOR_ARCHITEW6432"
        let globalArch = environVar "PROCESSOR_ARCHITECTURE"
        match wow64, globalArch with
        | "AMD64", "AMD64" 
        | null, "AMD64" 
        | "x86", "AMD64" -> environVar "ProgramFiles(x86)"
        | _ -> environVar "ProgramFiles"
        |> fun detected -> if isNull detected then @"C:\Program Files (x86)\" else detected

    /// The system root environment variable. Typically "C:\Windows"
    let SystemRoot = environVar "SystemRoot"

    /// Determines if the current system is an Unix system
    let isUnix = 
    #if NETSTANDARD1_6
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux) || 
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX)
    #else
        int Environment.OSVersion.Platform |> fun p -> (p = 4) || (p = 6) || (p = 128)
    #endif

    /// Determines if the current system is a MacOs system
    let isMacOS =
    #if NETSTANDARD1_6
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX)
    #else
        (Environment.OSVersion.Platform = PlatformID.MacOSX) ||
            // osascript is the AppleScript interpreter on OS X
            File.Exists "/usr/bin/osascript"
    #endif

    /// Determines if the current system is a Linux system
    let isLinux = 
    #if NETSTANDARD1_6
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux)
    #else
        isUnix && not isMacOS
    #endif

    /// Determines if the current system is a Windows system
    let isWindows =
    #if NETSTANDARD1_6
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows)
    #else
        match Environment.OSVersion.Platform with
        | PlatformID.Win32NT | PlatformID.Win32S | PlatformID.Win32Windows | PlatformID.WinCE -> true
        | _ -> false
    #endif

    /// Determines if the current system is a mono system
    /// Todo: Detect mono on windows
    let isMono = 
    #if NETSTANDARD1_6
        false
    #else
        isUnix
    #endif

    let isDotnetCore = 
    #if NETSTANDARD1_6
        true
    #else
        false
    #endif
    

    /// Gets the list of valid directories included in the PATH environment variable.
    let pathDirectories =
        splitEnvironVar "PATH"
        |> Seq.map (fun value -> value.Trim())
        |> Seq.filter (not << String.IsNullOrEmpty)

    let monoPath =
        if isMacOS && File.Exists "/Library/Frameworks/Mono.framework/Commands/mono" then
            "/Library/Frameworks/Mono.framework/Commands/mono"
        else
            "mono"

    /// The path of the current target platform
    let mutable TargetPlatformPrefix = 
        let (<|>) a b = 
            match a with
            | None -> b
            | _ -> a
        environVarOrNone "FrameworkDir32" <|> if (String.IsNullOrEmpty SystemRoot) then None
                                              else Some(Path.Combine(SystemRoot, "Microsoft.NET", "Framework")) 
        <|> if (isUnix) then Some "/usr/lib/mono"
            else Some @"C:\Windows\Microsoft.NET\Framework"
        |> Option.get

    /// Base path for getting tools from windows SDKs
    let sdkBasePath = Path.Combine(ProgramFilesX86, "Microsoft SDKs", "Windows")

    /// Helper function to help find framework or sdk tools from the 
    /// newest toolkit available
    let getNewestTool possibleToolPaths = 
           possibleToolPaths 
           |> Seq.sortBy (fun p -> p) 
           |> Array.ofSeq 
           |> Array.rev 
           |> Seq.ofArray 
           |> Seq.head

    /// Gets the local directory for the given target platform
    let getTargetPlatformDir platformVersion = 
        if Directory.Exists(TargetPlatformPrefix + "64") then Path.Combine(TargetPlatformPrefix + "64", platformVersion)
        else  Path.Combine(TargetPlatformPrefix, platformVersion)

    /// Contains the IO encoding which is given via build parameter "encoding" or the default encoding if no encoding was specified.
    let getDefaultEncoding() = 
        match environVarOrDefault "encoding" "default" with
#if !DOTNETCORE
        | "default" -> Text.Encoding.Default
#else
        | "default" -> Text.Encoding.UTF8
#endif
        | enc -> Text.Encoding.GetEncoding(enc)

#if !DOTNETCORE
    [<Obsolete("Will no longer be available in dotnetcore, target package is currently unknown")>]
    /// Returns a sequence with all installed .NET framework versions
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

    [<Obsolete("Will no longer be available in dotnetcore, target package is currently unknown")>]
    /// A record which allows to display lots of machine specific information like machine name, processor count etc.
    type MachineDetails = 
        { ProcessorCount : int
          Is64bit : bool
          OperatingSystem : string
          MachineName : string
          NETFrameworks : seq<string>
          UserDomainName : string
          AgentVersion : string
          DriveInfo : seq<string> }

    [<Obsolete("Will no longer be available in dotnetcore, target package is currently unknown")>]
    /// Retrieves information about the hard drives
    let getDrivesInfo() = 
        Environment.GetLogicalDrives()
        |> Seq.map (fun d -> IO.DriveInfo(d))
        |> Seq.filter (fun d -> d.IsReady)
        |> Seq.map 
               (fun d -> 
               sprintf "%s has %0.1fGB free of %0.1fGB" (d.Name.Replace(":\\", "")) 
                   (Convert.ToDouble(d.TotalFreeSpace) / (1024. * 1024. * 1024.)) 
                   (Convert.ToDouble(d.TotalSize) / (1024. * 1024. * 1024.)))

    [<Obsolete("Will no longer be available in dotnetcore, target package is currently unknown")>]
    /// Retrieves lots of machine specific information like machine name, processor count etc.
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
#endif