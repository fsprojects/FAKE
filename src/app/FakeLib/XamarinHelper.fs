/// Contains tasks for building Xamarin.iOS and Xamarin.Android apps
module Fake.XamarinHelper

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open System.Xml
open System.Text

let private executeCommand command args =
    ExecProcessAndReturnMessages (fun p ->
        p.FileName <- command
        p.Arguments <- args
    ) TimeSpan.MaxValue
    |>  fun result ->
             let output = String.Join (Environment.NewLine, result.Messages)
             tracefn "Process output: \r\n%A" output
             if result.ExitCode <> 0 then failwithf "%s exited with error %d" command result.ExitCode

/// The package restore paramater type
type XamarinComponentRestoreParams = {
    /// Path to xamarin-component.exe, defaults to checking tools/xpkg
    ToolPath: string
}

/// The default package restore parameters
let XamarinComponentRestoreDefaults = {
    ToolPath = findToolInSubPath "xamarin-component.exe" (currentDirectory @@ "tools" @@ "xpkg")
}

/// Restores NuGet packages and Xamarin Components for a project or solution
/// ## Parameters
///  - `setParams` - Function used to override the default package restore parameters
let RestoreComponents setParams projectFile =
    let restoreComponents project param =
        executeCommand param.ToolPath ("restore " + project)

    XamarinComponentRestoreDefaults
    |> setParams
    |> restoreComponents projectFile

/// The iOS build paramater type
type iOSBuildParams = {
    /// (Required) Path to solution or project file
    ProjectPath: string
    /// Build target, defaults to Build
    Target: string
    /// Build configuration, defaults to 'Debug'
    Configuration: string
    /// Build platform, defaults to 'iPhoneSimulator'
    Platform: string
    /// Output path for build, defaults to project settings
    OutputPath: string
    /// Indicates if an IPA file should be generated
    BuildIpa: bool
    /// Additional MSBuild properties, defaults to empty list
    Properties: (string * string) list
}

/// The default iOS build parameters
let iOSBuildDefaults = {
    ProjectPath = ""
    Target = "Build"
    Configuration = "Debug"
    Platform = "iPhoneSimulator"
    OutputPath = ""
    BuildIpa = false
    Properties = []
}


type AndroidAbiTargetConfig = {
    SuffixAndExtension: string
}

type AndroidAbiTarget = 
    | X86 of AndroidAbiTargetConfig
    | ArmEabi of AndroidAbiTargetConfig
    | ArmEabiV7a of AndroidAbiTargetConfig
    | Arm64V8a of AndroidAbiTargetConfig
    | X86And64 of AndroidAbiTargetConfig
    | AllAbi

type AndroidPackageAbiParam = 
    | OneApkForAll
    | SpecificAbis of AndroidAbiTarget list

let AllAndroidAbiTargets = 
    AndroidPackageAbiParam.SpecificAbis
        ( [ AndroidAbiTarget.X86({ SuffixAndExtension="-x86.apk"; })
            AndroidAbiTarget.ArmEabi({ SuffixAndExtension="-armeabi.apk"; })
            AndroidAbiTarget.ArmEabiV7a({ SuffixAndExtension="-armeabi-v7a.apk"; })
            AndroidAbiTarget.Arm64V8a({ SuffixAndExtension="-arm64-v8a.apk"; })
            AndroidAbiTarget.X86And64({ SuffixAndExtension="-x86_64.apk"; })
          ] )

type IncrementerVersion = int32 -> AndroidAbiTarget -> int32

/// Builds a project or solution using Xamarin's iOS build tools
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
let iOSBuild setParams =
    let validateParams param =
        if param.ProjectPath = "" then failwith "You must specify a project to package"
        let exists parameter = param.Properties |> List.exists (fun (key, _) -> key.Equals(parameter, StringComparison.OrdinalIgnoreCase))

        if exists("Configuration") then failwith "Cannot specify build configuration via additional parameters. Use Configuration field instead."
        if exists("Platform") then failwith "Cannot specify build platform via additional parameters. Use Platform field instead."
        if exists("BuildIpa") then failwith "Cannot specify build IPA via additional parameters. Use BuildIpa field instead."

        param

    let buildProject param =
        let properties = [ "Configuration", param.Configuration; "Platform" , param.Platform; "BuildIpa" , param.BuildIpa.ToString().ToLower(); ]
        let effectiveProperties = properties @ param.Properties

        MSBuild param.OutputPath param.Target effectiveProperties [ param.ProjectPath ] |> ignore

    iOSBuildDefaults
    |> setParams
    |> validateParams
    |> buildProject

/// The Android packaging parameter type
type AndroidPackageParams = {
    /// (Required) Path to the Android project file (not the solution file!)
    ProjectPath: string
    /// Build configuration, defaults to 'Release'
    Configuration: string
    /// Output path for build, defaults to 'bin/Release'
    OutputPath: string
    /// Additional MSBuild properties, defaults to empty list
    Properties: (string * string) list
    /// Build an APK Targetting One ABI (used to reduce the size of the APK and support different CPU architectures)
    PackageAbiTargets: AndroidPackageAbiParam
    /// Used for multiple APK packaging to set different version code par ABI
    VersionStepper:IncrementerVersion option
}

/// The default Android packaging parameters
let AndroidPackageDefaults = {
    ProjectPath = ""
    Configuration = "Release"
    OutputPath = "bin/Release"
    Properties = []
    PackageAbiTargets = AndroidPackageAbiParam.OneApkForAll
    VersionStepper = None
}

/// Packages a Xamarin.Android app, returning a multiple FileInfo objects for the unsigned APK files
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
let AndroidBuildPackages setParams =
    let validateParams param =
        if param.ProjectPath = "" then failwith "You must specify a project to package"
        if param.Properties 
            |> List.exists (fun (key, _) -> key.Equals("Configuration", StringComparison.OrdinalIgnoreCase))
            then failwith "Cannot specify build configuration via additional parameters. Use Configuration field instead."

        param

    let buildPackages param (abi:string option) (manifestFile:string option) = 
        let options = match (abi,manifestFile) with 
                      | Some a, Some m -> let manifest = @"Properties" @@ System.IO.Path.GetFileName(m)
                                          [ "Configuration", param.Configuration
                                            "AndroidSupportedAbis", a
                                            "AndroidManifest", manifest ]
                      | Some a, None   -> [ "Configuration", param.Configuration
                                            "AndroidSupportedAbis", a]
                      | _, _           -> [ "Configuration", param.Configuration ] 
        MSBuild param.OutputPath "PackageForAndroid" options [ param.ProjectPath ] |> ignore

    let rewriteManifestFile (manifestFile:string) outfile (transformVersion:IncrementerVersion) target =
        let manifest = XDocument.Load(manifestFile)
        let vc = manifest.Element("manifest" |> XName.Get).Attributes() |> Seq.filter(fun a -> a.Name.LocalName = "versionCode") |> Seq.exactlyOne
        let v = transformVersion (Convert.ToInt32(vc.Value)) target
        vc.Value <- v.ToString()
        use fs = new FileStream(outfile, FileMode.OpenOrCreate)
        use wr = new XmlTextWriter(fs, Encoding.UTF8)
        wr.Formatting <- Formatting.Indented
        manifest.Save(wr)

    let mostRecentFileInDirMatching path = 
        directoryInfo path
        |> filesInDirMatching "*.apk"
        |> Seq.sortBy (fun file -> file.LastWriteTime)
        |> Seq.last

    let createPackage param =
        let effectiveProperties = [ "Configuration", param.Configuration ] @ param.Properties
        MSBuild param.OutputPath "PackageForAndroid" effectiveProperties [ param.ProjectPath ] |> ignore
        [ mostRecentFileInDirMatching param.OutputPath ]

    let buildSpecificApk param manifestFile name transformVersion target =
        let specificManifest = (manifestFile |> Path.GetDirectoryName) @@ ("AndroidManifest-" + name + ".xml")
        rewriteManifestFile manifestFile specificManifest transformVersion target
        // workaround for xamarin bug: https://bugzilla.xamarin.com/show_bug.cgi?id=30571
        let backupFn = (manifestFile |> Path.GetDirectoryName) @@ ("AndroidManifest-original.xml")
        CopyFile backupFn manifestFile
        CopyFile manifestFile specificManifest
        try
            //buildPackages param (Some name) (Some specificManifest) // to uncomment after xamarin fix there bug
            buildPackages param (Some name) None
        finally
            CopyFile manifestFile backupFn

    let translateAbi = function
                       | AndroidAbiTarget.X86 _ -> "x86"
                       | AndroidAbiTarget.ArmEabi _ -> "armeabi"
                       | AndroidAbiTarget.ArmEabiV7a _ -> "armeabi-v7a"
                       | AndroidAbiTarget.Arm64V8a _ -> "arm64-v8a"
                       | AndroidAbiTarget.X86And64 _ -> "X86_64"
                       | _ -> ""

    let createTargetPackage param (manifestFile:string) (target:AndroidAbiTarget) transformVersion = 
        let name = target |> translateAbi
        match target with
        | AndroidAbiTarget.X86 c
        | AndroidAbiTarget.ArmEabi c
        | AndroidAbiTarget.ArmEabiV7a c
        | AndroidAbiTarget.Arm64V8a c
        | AndroidAbiTarget.X86And64 c -> buildSpecificApk param manifestFile name transformVersion target
        | _ -> buildPackages param None None

    let createPackageAbiSpecificApk param (targets:AndroidAbiTarget list) transformVersion =
        let manifestPath = (param.ProjectPath |> Path.GetDirectoryName) @@ @"Properties" @@ "AndroidManifest.xml"
        seq { for t in targets do
                createTargetPackage param manifestPath t transformVersion
                let apk = mostRecentFileInDirMatching param.OutputPath
                let name = t |> translateAbi
                if name.Length > 0 then 
                    let apkname = Path.GetFileNameWithoutExtension(apk.Name) + "-" + name + ".apk"
                    yield apk.CopyTo (param.OutputPath @@ apkname)
                else
                    yield apk
            } |> Seq.toList

    let param = AndroidPackageDefaults |> setParams |> validateParams

    let transformVersion = match param.VersionStepper with
                           | Some f -> f
                           | None -> (fun v t -> match t with
                                                 | AndroidAbiTarget.X86 c -> v + 1
                                                 | AndroidAbiTarget.X86And64 c -> v + 2
                                                 | AndroidAbiTarget.ArmEabi c -> v + 3
                                                 | AndroidAbiTarget.ArmEabiV7a c -> v + 4
                                                 | AndroidAbiTarget.Arm64V8a c -> v + 5
                                                 | _ -> v)

    match param.PackageAbiTargets with
    | AndroidPackageAbiParam.OneApkForAll -> param |> createPackage
    | AndroidPackageAbiParam.SpecificAbis targets -> createPackageAbiSpecificApk param targets transformVersion


/// Packages a Xamarin.Android app, returning a FileInfo object for the unsigned APK file
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
let AndroidPackage setParams =
    AndroidBuildPackages setParams |> Seq.exactlyOne

// Parameters for signing and aligning an Android package
type AndroidSignAndAlignParams = {
    /// (Required) Path to keystore used to sign the app
    KeystorePath: string
    /// (Required) Password for keystore
    KeystorePassword: string
    /// (Required) Alias for keystore
    KeystoreAlias: string
    /// Specifies the name of the signature algorithm to use to sign the JAR file.
    SignatureAlgorithm: string
    /// Specifies the name of the message digest algorithm to use when digesting the entries of a JAR file. 
    MessageDigestAlgorithm: string
    /// Path to jarsigner tool, defaults to assuming it is in your path
    JarsignerPath: string
    /// Path to zipalign tool, defaults to assuming it is in your path
    ZipalignPath: string
}

/// The default Android signing and aligning parameters
let AndroidSignAndAlignDefaults = {
    KeystorePath = ""
    KeystorePassword = ""
    KeystoreAlias = ""
    SignatureAlgorithm = "SHA1withRSA"
    MessageDigestAlgorithm = "SHA1"
    JarsignerPath = "jarsigner"
    ZipalignPath = "zipalign"
}

/// Signs and aligns a Xamarin.Android package, returning a FileInfo object for the signed APK file
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
///  - `apkFile` - FileInfo object for an unsigned APK file to sign and align
let AndroidSignAndAlign setParams apkFile =
    let validateParams param =
        if param.KeystorePath = "" then failwith "You must specify a keystore to use"
        if param.KeystorePassword = "" then failwith "You must provide the keystore's password"
        if param.KeystoreAlias = "" then failwith "You must provide the keystore's alias"

        param
    
    let quotesSurround (s:string) = if EnvironmentHelper.isMono then sprintf "'%s'" s else sprintf "\"%s\"" s
    
    let signAndAlign (file:FileInfo) (param:AndroidSignAndAlignParams) =
        let fullSignedFilePath = Regex.Replace(file.FullName, ".apk$", "-Signed.apk")
        let jarsignerArgs = String.Format("-sigalg {0} -digestalg {1} -keystore {2} -storepass {3} -signedjar {4} {5} {6}", 
                                param.SignatureAlgorithm, param.MessageDigestAlgorithm, quotesSurround(param.KeystorePath), param.KeystorePassword, quotesSurround(fullSignedFilePath), quotesSurround(file.FullName), param.KeystoreAlias)
        
        executeCommand param.JarsignerPath jarsignerArgs

        let fullAlignedFilePath = Regex.Replace(fullSignedFilePath, "-Signed.apk$", "-SignedAndAligned.apk")
        let zipalignArgs = String.Format("-f -v 4 {0} {1}", quotesSurround(fullSignedFilePath), quotesSurround(fullAlignedFilePath))
        executeCommand param.ZipalignPath zipalignArgs

        fileInfo fullAlignedFilePath

    AndroidSignAndAlignDefaults
    |> setParams
    |> validateParams
    |> signAndAlign apkFile  

/// Signs and aligns multiple Xamarin.Android packages, returning multiple FileInfo objects for the signed APK file
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
///  - `apkFiles` - FileInfo object for an unsigned APK file to sign and align
let AndroidSignAndAlignPackages setParams apkFiles =
    apkFiles |> Seq.map (fun f -> AndroidSignAndAlign setParams f)

/// The iOS archive paramater type
type iOSArchiveParams = {
    /// Path to desired solution file. If not provided, mdtool finds the first solution in the current directory.
    /// Although mdtool can take a project file, the archiving seems to fail to work without a solution.
    SolutionPath: string
    /// Project name within a solution file
    ProjectName: string
    /// Build configuration, defaults to 'Debug|iPhoneSimulator'
    Configuration: string
    /// Path to mdtool, defaults to Xamarin Studio's usual path
    MDToolPath: string
}

/// The default iOS archive parameters
let iOSArchiveDefaults = {
    SolutionPath = ""
    ProjectName = ""
    Configuration = "Debug|iPhoneSimulator"
    MDToolPath = "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool"
}
    
/// Archive a project using Xamarin's iOS archive tools
/// ## Parameters
///  - `setParams` - Function used to override the default archive parameters
let iOSArchive setParams =
    let archiveProject param =
        let projectNameArg = if param.ProjectName <> "" then String.Format("-p:{0} ", param.ProjectName) else ""
        let args = String.Format(@"-v archive ""-c:{0}"" {1}{2}", param.Configuration, projectNameArg, param.SolutionPath)
        executeCommand param.MDToolPath args

    iOSArchiveDefaults
        |> setParams
        |> archiveProject
