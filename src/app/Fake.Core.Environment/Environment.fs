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

[<RequireQualifiedAccess>]
module Environment =
//    type Environment = System.Environment
#if DOTNETCORE
    open Fake.SystemHelper
#endif

    open System
    open System.IO
    open System.Diagnostics
    open System.Collections.Generic
    open System.Text
    open System.Text.RegularExpressions
    open System.Reflection
    open Microsoft.Win32

    /// Type alias for System.EnvironmentVariableTarget
    #if !DOTNETCORE
    type EnvironTarget = EnvironmentVariableTarget
    #endif

    /// Retrieves the environment variable with the given name
    let environVar name = System.Environment.GetEnvironmentVariable name

    /// Retrieves all environment variables from the given target
    let environVars () = 
        let vars = System.Environment.GetEnvironmentVariables ()
        [ for e in vars -> 
              let e1 = e :?> Collections.DictionaryEntry
              e1.Key.ToString(), e1.Value.ToString() ]

    #if !DOTNETCORE
    [<Obsolete("Will be removed in dotnetcore. Use environVars instead.")>]
    let environVarsWithMode mode = 
        let vars = System.Environment.GetEnvironmentVariables (mode)
        [ for e in vars -> 
              let e1 = e :?> Collections.DictionaryEntry
              e1.Key, e1.Value ]
    #endif

    /// Sets the environment variable with the given name
    let setEnvironVar name value = System.Environment.SetEnvironmentVariable(name, value)

    /// Clears the environment variable with the given name for the current process.
    let clearEnvironVar name = System.Environment.SetEnvironmentVariable(name, null)

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
    let environVarAsBool varName = environVarAsBoolOrDefault varName false

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
        if hasEnvironVar name then environVar name
        else defaultParam

    [<Obsolete("Use 'environVarOrDefault name String.Empty' instead")>]
    /// Returns the value of the build parameter with the given name if it was set and otherwise an empty string
    let inline getBuildParam name = environVarOrDefault name String.Empty

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

    /// Determines if the current system is an Unix system.
    /// See http://www.mono-project.com/docs/faq/technical/#how-to-detect-the-execution-platform
    let isUnix = 
    #if !FX_NO_RUNTIME_INFORMATION
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux) || 
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX)
    #else
        int System.Environment.OSVersion.Platform |> fun p -> (p = 4) || (p = 6) || (p = 128)
    #endif

    /// Determines if the current system is a MacOs system
    let isMacOS =
    #if !FX_NO_RUNTIME_INFORMATION
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX)
    #else
        (System.Environment.OSVersion.Platform = PlatformID.MacOSX) ||
            // osascript is the AppleScript interpreter on OS X
            File.Exists "/usr/bin/osascript"
    #endif

    /// Determines if the current system is a Linux system
    let isLinux = 
    #if !FX_NO_RUNTIME_INFORMATION
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux)
    #else
        isUnix && not isMacOS
    #endif

    /// Determines if the current system is a Windows system
    let isWindows =
    #if !FX_NO_RUNTIME_INFORMATION
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows)
    #else
        match System.Environment.OSVersion.Platform with
        | PlatformID.Win32NT | PlatformID.Win32S | PlatformID.Win32Windows | PlatformID.WinCE -> true
        | _ -> false
    #endif

    /// Determines if the current FAKE runner is being run via mono.  With the FAKE 5 runner, this will always be false
    /// Todo: Detect mono on windows
    let isMono = 
    #if !FX_NO_RUNTIME_INFORMATION
        not (isNull (Type.GetType("Mono.Runtime")))
    #else
        isUnix
    #endif

    let isDotNetCore = 
    #if !FX_NO_RUNTIME_INFORMATION
        // See https://github.com/dotnet/corefx/blob/master/src/System.Runtime.InteropServices.RuntimeInformation/src/System/Runtime/InteropServices/RuntimeInformation/RuntimeInformation.cs
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Core")
    #else
        false
    #endif
    [<System.Obsolete("Use isDotNetCore instead (different casing of 'N')")>]
    let isDotnetCore = isDotNetCore
    
    module Internal =
        /// Internal, do not use.
        /// We use this internally for parsing the output of mono --version
        let parseMonoDisplayName displayName =
            let pattern = Regex("\d+(\.\d+)+")
            let m = pattern.Match(displayName)
            // NOTE: in System.Version 5.0 >= 5.0.0.0 is false while 5.0.0.0 >= 5.0 is true...
            let minimizeVersion (v:System.Version) =
                match v.Minor = 0, v.Revision = 0 with
                | true, true -> System.Version(v.Major, v.Minor)
                | _, true -> System.Version(v.Major, v.Minor, v.Build)
                | _ -> v

            match System.Version.TryParse m.Value with
            | true, v -> Some (minimizeVersion v)
            | _ -> None

    /// required sometimes to workaround mono crashes
    /// http://stackoverflow.com/a/8414517/1269722
    /// Note: Only given when we are running on mono,
    /// represents the version of the mono runtime we
    /// are currently running on.
    /// In netcore world you can retrieve the mono version in the
    /// environment (PATH) via Fake.Core.Process.Mono.monoVersion
    let monoVersion =
        let t = Type.GetType("Mono.Runtime")
        if (not (isNull t)) then
#if NETSTANDARD
            let t = t.GetTypeInfo()
#endif
            let displayNameMeth = t.GetMethod("GetDisplayName", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
            let displayName = displayNameMeth.Invoke(null, null).ToString()
            Some (displayName, Internal.parseMonoDisplayName displayName)
        else None


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
    let internal TargetPlatformPrefix = 
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
#if !NETSTANDARD
        | "default" -> Text.Encoding.Default
#else
        | "default" -> Text.Encoding.UTF8
#endif
        | enc -> Text.Encoding.GetEncoding(enc)

    let private getEnvDir specialPath =
        let dir = Environment.GetFolderPath specialPath 
        if String.IsNullOrEmpty dir then None else Some dir
    let private localRootForTempData() =
        getEnvDir Environment.SpecialFolder.UserProfile
        |> Option.orElse (getEnvDir Environment.SpecialFolder.LocalApplicationData)
        |> Option.defaultWith (fun _ ->
            let fallback = Path.GetFullPath ".paket"
            //Logging.traceWarnfn "Could not detect a root for our (user specific) temporary files. Try to set the 'HOME' or 'LocalAppData' environment variable!. Using '%s' instead." fallback
            if not (Directory.Exists fallback) then
                Directory.CreateDirectory fallback |> ignore
            fallback
        )
    let [<Literal>] private globalPackagesFolderEnvironmentKey = "NUGET_PACKAGES"
    let private nugetPackagesFolder =
        lazy
            environVarOrNone globalPackagesFolderEnvironmentKey 
            |> Option.map (fun path ->
                path.Replace (Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            ) |> Option.defaultWith (fun _ ->
                Path.Combine (localRootForTempData(),".nuget","packages")
            )
    /// Returns the path to the user-specific nuget packages folder
    let getNuGetPackagesCacheFolder() = nugetPackagesFolder.Value



#if !NETSTANDARD
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
        System.Environment.GetLogicalDrives()
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
        { ProcessorCount = System.Environment.ProcessorCount
          Is64bit = System.Environment.Is64BitOperatingSystem
          OperatingSystem = System.Environment.OSVersion.ToString()
          MachineName = System.Environment.MachineName
          NETFrameworks = getInstalledDotNetFrameworks()
          UserDomainName = System.Environment.UserDomainName
          AgentVersion = 
              sprintf "%A" ((System.Reflection.Assembly.GetAssembly(typedefof<MachineDetails>)).GetName().Version)
          DriveInfo = getDrivesInfo() }
#endif
