﻿namespace Fake.DotNet

/// <summary>
/// Contains tasks for building Xamarin.iOS and Xamarin.Android apps
/// </summary>
module Xamarin =

    open System
    open System.IO
    open System.Text.RegularExpressions
    open System.Xml
    open System.Xml.Linq
    open System.Text
    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators
    open Fake.DotNet

    let private executeCommand command args =
        let results = System.Collections.Generic.List<ConsoleMessage>()

        let errorF msg =
            Trace.traceError msg
            results.Add(ConsoleMessage.CreateError msg)

        let messageF msg =
            Trace.trace msg
            results.Add(ConsoleMessage.CreateOut msg)

        let processResult =
            CreateProcess.fromRawCommandLine command args
            |> CreateProcess.withTimeout TimeSpan.MaxValue
            |> CreateProcess.redirectOutput
            |> CreateProcess.withOutputEventsNotNull messageF errorF
            |> Proc.run

        results
        |> Seq.iter (fun cm -> Trace.logVerbosefn "%O: %s" cm.Timestamp cm.Message)

        if processResult.ExitCode <> 0 then
            failwithf "%s exited with error %d" command processResult.ExitCode

    /// <summary>
    /// The package restore parameter type
    /// </summary>
    type XamarinComponentRestoreParams =
        {
            /// Path to xamarin-component.exe, defaults to checking tools/xpkg
            ToolPath: string
        }

    let internal toolPath =
        let toolPath =
            ProcessUtils.tryFindLocalTool
                "TOOL"
                "xamarin-component.exe"
                [ ((Path.GetFullPath ".") @@ "tools" @@ "xpkg") ]

        match toolPath with
        | Some path -> path
        | None -> "xamarin-component.exe"

    /// <summary>
    /// The default package restore parameters
    /// </summary>
    let XamarinComponentRestoreDefaults =
        {
          // Xamarin component tool path
          ToolPath = toolPath }

    /// <summary>
    /// Restores NuGet packages and Xamarin Components for a project or solution
    /// </summary>
    ///
    /// <param name="setParams">Function used to override the default package restore parameters</param>
    /// <param name="projectFile">The project file to use</param>
    let RestoreComponents setParams projectFile =
        let restoreComponents project param =
            executeCommand param.ToolPath ("restore " + project)

        XamarinComponentRestoreDefaults |> setParams |> restoreComponents projectFile

    /// <summary>
    /// The iOS build parameter type
    /// </summary>
    type iOSBuildParams =
        {
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

            /// Max CPU count to use
            MaxCpuCount: int option option

            /// Disable/enable logging, default is false
            NoLogo: bool

            /// Disable/enable node re-use, default is false
            NodeReuse: bool

            /// Disable/enable package restoring, default is false
            RestorePackagesFlag: bool

            /// Tools version
            ToolsVersion: string option

            /// Set verbosity level
            Verbosity: MSBuildVerbosity option

            /// Disable/enable console logging, default is false
            NoConsoleLogger: bool

            /// The MSBuild file logger configurations
            FileLoggers: MSBuildFileLoggerConfig list option

            /// The binary logger configurations
            BinaryLoggers: string list option

            /// The distributed logger configurations
            DistributedLoggers: (MSBuildDistributedLoggerConfig * MSBuildDistributedLoggerConfig option) list option
        }

    /// The default iOS build parameters
    let iOSBuildDefaults =
        { ProjectPath = ""
          Target = "Build"
          Configuration = "Debug"
          Platform = "iPhoneSimulator"
          OutputPath = ""
          BuildIpa = false
          Properties = []
          MaxCpuCount = Some None
          NoLogo = false
          NodeReuse = false
          ToolsVersion = None
          Verbosity = None
          NoConsoleLogger = false
          RestorePackagesFlag = false
          FileLoggers = None
          BinaryLoggers = None
          DistributedLoggers = None }


    /// <summary>
    /// The target config values for Android Abi
    /// </summary>
    type AndroidAbiTargetConfig = { SuffixAndExtension: string }

    /// The different types of Android Abi targets supported
    type AndroidAbiTarget =
        | X86 of AndroidAbiTargetConfig
        | ArmEabi of AndroidAbiTargetConfig
        | ArmEabiV7a of AndroidAbiTargetConfig
        | Arm64V8a of AndroidAbiTargetConfig
        | X86And64 of AndroidAbiTargetConfig
        | AllAbi

    /// The Android package Abi parameters, give option to select one Apk or specific Abis
    type AndroidPackageAbiParam =
        | OneApkForAll
        | SpecificAbis of AndroidAbiTarget list

    /// Select all Android Abi supported
    let AllAndroidAbiTargets =
        AndroidPackageAbiParam.SpecificAbis(
            [ AndroidAbiTarget.X86({ SuffixAndExtension = "-x86.apk" })
              AndroidAbiTarget.ArmEabi({ SuffixAndExtension = "-armeabi.apk" })
              AndroidAbiTarget.ArmEabiV7a({ SuffixAndExtension = "-armeabi-v7a.apk" })
              AndroidAbiTarget.Arm64V8a({ SuffixAndExtension = "-arm64-v8a.apk" })
              AndroidAbiTarget.X86And64({ SuffixAndExtension = "-x86_64.apk" }) ]
        )

    /// The version incrementing type
    type IncrementerVersion = int32 -> AndroidAbiTarget -> int32

    /// <summary>
    /// Builds a project or solution using Xamarin's iOS build tools
    /// </summary>
    ///
    /// <param name="setParams">Function used to override the default build parameters</param>
    let iOSBuild setParams =
        let validateParams param =
            if param.ProjectPath = "" then
                failwith "You must specify a project to package"

            let exists parameter =
                param.Properties
                |> List.exists (fun (key, _) -> key.Equals(parameter, StringComparison.OrdinalIgnoreCase))

            if exists "Configuration" then
                failwith
                    "Cannot specify build configuration via additional parameters. Use Configuration field instead."

            if exists "Platform" then
                failwith "Cannot specify build platform via additional parameters. Use Platform field instead."

            if exists "BuildIpa" then
                failwith "Cannot specify build IPA via additional parameters. Use BuildIpa field instead."

            param

        let applyiOSBuildParamsToMSBuildParams iOSBuildParams buildParams =
            let msBuildParams =
                { buildParams with
                    Targets = [ iOSBuildParams.Target ]
                    Properties =
                        [ "Configuration", iOSBuildParams.Configuration
                          "Platform", iOSBuildParams.Platform
                          "BuildIpa", iOSBuildParams.BuildIpa.ToString() ]
                        @ iOSBuildParams.Properties
                    MaxCpuCount = iOSBuildParams.MaxCpuCount
                    NoLogo = iOSBuildParams.NoLogo
                    NodeReuse = iOSBuildParams.NodeReuse
                    ToolsVersion = iOSBuildParams.ToolsVersion
                    Verbosity = iOSBuildParams.Verbosity
                    NoConsoleLogger = iOSBuildParams.NoConsoleLogger
                    RestorePackagesFlag = iOSBuildParams.RestorePackagesFlag
                    FileLoggers = iOSBuildParams.FileLoggers
                    BinaryLoggers = iOSBuildParams.BinaryLoggers
                    DistributedLoggers = iOSBuildParams.DistributedLoggers }

            let msBuildParams =
                if String.isNullOrEmpty iOSBuildParams.OutputPath then
                    msBuildParams
                else
                    { msBuildParams with
                        Properties =
                            [ "OutputPath", Path.getFullName iOSBuildParams.OutputPath ]
                            @ msBuildParams.Properties }

            msBuildParams

        let buildProject param =
            MSBuild.build (fun msbuildParam -> applyiOSBuildParamsToMSBuildParams param msbuildParam) param.ProjectPath

        iOSBuildDefaults |> setParams |> validateParams |> buildProject

    /// <summary>
    /// The Android packaging parameter type
    /// </summary>
    type AndroidPackageParams =
        {
            /// (Required) Path to the Android project file (not the solution file!)
            ProjectPath: string

            /// Build configuration, defaults to 'Release'
            Configuration: string

            /// Output path for build, defaults to 'bin/Release'
            OutputPath: string

            /// Additional MSBuild properties, defaults to empty list
            Properties: (string * string) list

            /// Build an APK Targeting One ABI (used to reduce the size of the APK and support different CPU architectures)
            PackageAbiTargets: AndroidPackageAbiParam

            /// Used for multiple APK packaging to set different version code par ABI
            VersionStepper: IncrementerVersion option

            /// Max CPU count to use
            MaxCpuCount: int option option

            /// Disable/enable logging, default is false
            NoLogo: bool

            /// Disable/enable node re-use, default is false
            NodeReuse: bool

            /// Disable/enable package restoring, default is false
            RestorePackagesFlag: bool

            /// Tools version
            ToolsVersion: string option

            /// Set verbosity level
            Verbosity: MSBuildVerbosity option

            /// Disable/enable console logging, default is false
            NoConsoleLogger: bool

            /// The MSBuild file logger configurations
            FileLoggers: MSBuildFileLoggerConfig list option

            /// The binary logger configurations
            BinaryLoggers: string list option

            /// The distributed logger configurations
            DistributedLoggers: (MSBuildDistributedLoggerConfig * MSBuildDistributedLoggerConfig option) list option
        }

    /// The default Android packaging parameters
    let AndroidPackageDefaults =
        { ProjectPath = ""
          Configuration = "Release"
          OutputPath = "bin/Release"
          Properties = []
          PackageAbiTargets = AndroidPackageAbiParam.OneApkForAll
          VersionStepper = None
          MaxCpuCount = Some None
          NoLogo = false
          NodeReuse = false
          ToolsVersion = None
          Verbosity = None
          NoConsoleLogger = false
          RestorePackagesFlag = false
          FileLoggers = None
          BinaryLoggers = None
          DistributedLoggers = None }

    /// <summary>
    /// Packages a Xamarin.Android app, returning a multiple FileInfo objects for the unsigned APK files
    /// </summary>
    ///
    /// <param name="setParams">Function used to override the default build parameters</param>
    let AndroidBuildPackages setParams =
        let validateParams param =
            if param.ProjectPath = "" then
                failwith "You must specify a project to package"

            if
                param.Properties
                |> List.exists (fun (key, _) -> key.Equals("Configuration", StringComparison.OrdinalIgnoreCase))
            then
                failwith
                    "Cannot specify build configuration via additional parameters. Use Configuration field instead."

            param

        let applyAndroidBuildParamsToMSBuildParams androidBuildParams buildParams =
            let msBuildParams =
                { buildParams with
                    Targets = [ "PackageForAndroid" ]
                    Properties =
                        [ "Configuration", androidBuildParams.Configuration ]
                        @ androidBuildParams.Properties
                    MaxCpuCount = androidBuildParams.MaxCpuCount
                    NoLogo = androidBuildParams.NoLogo
                    NodeReuse = androidBuildParams.NodeReuse
                    ToolsVersion = androidBuildParams.ToolsVersion
                    Verbosity = androidBuildParams.Verbosity
                    NoConsoleLogger = androidBuildParams.NoConsoleLogger
                    RestorePackagesFlag = androidBuildParams.RestorePackagesFlag
                    FileLoggers = androidBuildParams.FileLoggers
                    BinaryLoggers = androidBuildParams.BinaryLoggers
                    DistributedLoggers = androidBuildParams.DistributedLoggers }

            let msBuildParams =
                if String.isNullOrEmpty androidBuildParams.OutputPath then
                    msBuildParams
                else
                    { msBuildParams with
                        Properties =
                            [ "OutputPath", Path.getFullName androidBuildParams.OutputPath ]
                            @ msBuildParams.Properties }

            msBuildParams

        let buildPackages param (abi: string option) (manifestFile: string option) =
            let applyBuildParams msbuildParam =
                let result = applyAndroidBuildParamsToMSBuildParams param msbuildParam

                let result =
                    { result with
                        Properties =
                            match (abi, manifestFile) with
                            | Some a, Some m ->
                                let manifest = @"Properties" @@ Path.GetFileName(m)
                                [ "AndroidSupportedAbis", a; "AndroidManifest", manifest ] @ result.Properties
                            | Some a, None -> [ "AndroidSupportedAbis", a ] @ result.Properties
                            | _, _ -> result.Properties }

                result

            MSBuild.build (fun msbuildParam -> applyBuildParams msbuildParam) param.ProjectPath

        let rewriteManifestFile (manifestFile: string) outfile (transformVersion: IncrementerVersion) target =
            let manifest = XDocument.Load(manifestFile)

            let vc =
                manifest.Element("manifest" |> XName.Get).Attributes()
                |> Seq.filter (fun a -> a.Name.LocalName = "versionCode")
                |> Seq.exactlyOne

            let v = transformVersion (Convert.ToInt32(vc.Value)) target
            vc.Value <- v.ToString()
            use fs = new FileStream(outfile, FileMode.OpenOrCreate)
            use wr = new XmlTextWriter(fs, Encoding.UTF8)
            wr.Formatting <- Formatting.Indented
            manifest.Save(wr)

        let mostRecentFileInDirMatching path =
            DirectoryInfo.ofPath path
            |> DirectoryInfo.getMatchingFiles "*.apk"
            |> Seq.sortBy (fun file -> file.LastWriteTime)
            |> Seq.last

        let createPackage param =
            MSBuild.build
                (fun msbuildParam -> applyAndroidBuildParamsToMSBuildParams param msbuildParam)
                param.ProjectPath

            [ mostRecentFileInDirMatching param.OutputPath ]

        let buildSpecificApk param (manifestFile: string) name transformVersion target =
            let specificManifest =
                (manifestFile |> Path.GetDirectoryName) @@ ("AndroidManifest-" + name + ".xml")

            rewriteManifestFile manifestFile specificManifest transformVersion target
            // workaround for xamarin bug: https://bugzilla.xamarin.com/show_bug.cgi?id=30571
            let backupFn =
                (manifestFile |> Path.GetDirectoryName) @@ "AndroidManifest-original.xml"

            Shell.copyFile backupFn manifestFile
            Shell.copyFile manifestFile specificManifest

            try
                //buildPackages param (Some name) (Some specificManifest) // to uncomment after xamarin fix there bug
                buildPackages param (Some name) None
            finally
                Shell.copyFile manifestFile backupFn

        let translateAbi =
            function
            | AndroidAbiTarget.X86 _ -> "x86"
            | AndroidAbiTarget.ArmEabi _ -> "armeabi"
            | AndroidAbiTarget.ArmEabiV7a _ -> "armeabi-v7a"
            | AndroidAbiTarget.Arm64V8a _ -> "arm64-v8a"
            | AndroidAbiTarget.X86And64 _ -> "x86_64"
            | _ -> ""

        let createTargetPackage param (manifestFile: string) (target: AndroidAbiTarget) transformVersion =
            let name = target |> translateAbi

            match target with
            | AndroidAbiTarget.X86 _
            | AndroidAbiTarget.ArmEabi _
            | AndroidAbiTarget.ArmEabiV7a _
            | AndroidAbiTarget.Arm64V8a _
            | AndroidAbiTarget.X86And64 _ -> buildSpecificApk param manifestFile name transformVersion target
            | _ -> buildPackages param None None

        let createPackageAbiSpecificApk param (targets: AndroidAbiTarget list) transformVersion =
            let manifestPath =
                (param.ProjectPath |> Path.GetDirectoryName)
                @@ @"Properties"
                @@ "AndroidManifest.xml"

            seq {
                for t in targets do
                    createTargetPackage param manifestPath t transformVersion
                    let apk = mostRecentFileInDirMatching param.OutputPath
                    let name = t |> translateAbi

                    if name.Length > 0 then
                        let apkname = Path.GetFileNameWithoutExtension(apk.Name) + "-" + name + ".apk"
                        yield apk.CopyTo(param.OutputPath @@ apkname)
                    else
                        yield apk
            }
            |> Seq.toList

        let param = AndroidPackageDefaults |> setParams |> validateParams

        let transformVersion =
            match param.VersionStepper with
            | Some f -> f
            | None ->
                (fun v t ->
                    match t with
                    | AndroidAbiTarget.X86 _ -> v + 1
                    | AndroidAbiTarget.X86And64 _ -> v + 2
                    | AndroidAbiTarget.ArmEabi _ -> v + 3
                    | AndroidAbiTarget.ArmEabiV7a _ -> v + 4
                    | AndroidAbiTarget.Arm64V8a _ -> v + 5
                    | _ -> v)

        match param.PackageAbiTargets with
        | AndroidPackageAbiParam.OneApkForAll -> param |> createPackage
        | AndroidPackageAbiParam.SpecificAbis targets -> createPackageAbiSpecificApk param targets transformVersion


    /// <summary>
    /// Packages a Xamarin.Android app, returning a FileInfo object for the unsigned APK file
    /// </summary>
    ///
    /// <param name="setParams">Function used to override the default build parameters</param>
    let AndroidPackage setParams =
        AndroidBuildPackages setParams |> Seq.exactlyOne

    /// <summary>
    /// Parameters for signing and aligning an Android package
    /// </summary>
    type AndroidSignAndAlignParams =
        {
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
    let AndroidSignAndAlignDefaults =
        { KeystorePath = ""
          KeystorePassword = ""
          KeystoreAlias = ""
          SignatureAlgorithm = "SHA1withRSA"
          MessageDigestAlgorithm = "SHA1"
          JarsignerPath = "jarsigner"
          ZipalignPath = "zipalign" }

    /// <summary>
    /// Signs and aligns a Xamarin.Android package, returning a FileInfo object for the signed APK file
    /// </summary>
    ///
    /// <param name="setParams">Function used to override the default build parameters</param>
    /// <param name="apkFile">FileInfo object for an unsigned APK file to sign and align</param>
    let AndroidSignAndAlign setParams apkFile =
        let validateParams param =
            if param.KeystorePath = "" then
                failwith "You must specify a keystore to use"

            if param.KeystorePassword = "" then
                failwith "You must provide the keystore's password"

            if param.KeystoreAlias = "" then
                failwith "You must provide the keystore's alias"

            param

        let quotesSurround (s: string) =
            if Environment.isMono then
                sprintf "'%s'" s
            else
                sprintf "\"%s\"" s

        let signAndAlign (file: FileInfo) (param: AndroidSignAndAlignParams) =
            let fullSignedFilePath = Regex.Replace(file.FullName, ".apk$", "-Signed.apk")

            let jarsignerArgs =
                String.Format(
                    "-sigalg {0} -digestalg {1} -keystore {2} -storepass {3} -signedjar {4} {5} {6}",
                    param.SignatureAlgorithm,
                    param.MessageDigestAlgorithm,
                    quotesSurround param.KeystorePath,
                    param.KeystorePassword,
                    quotesSurround fullSignedFilePath,
                    quotesSurround file.FullName,
                    param.KeystoreAlias
                )

            executeCommand param.JarsignerPath jarsignerArgs

            let fullAlignedFilePath =
                Regex.Replace(fullSignedFilePath, "-Signed.apk$", "-SignedAndAligned.apk")

            let zipalignArgs =
                String.Format("-f -v 4 {0} {1}", quotesSurround fullSignedFilePath, quotesSurround fullAlignedFilePath)

            executeCommand param.ZipalignPath zipalignArgs

            FileInfo.ofPath fullAlignedFilePath

        AndroidSignAndAlignDefaults
        |> setParams
        |> validateParams
        |> signAndAlign apkFile

    /// <summary>
    /// Signs and aligns multiple Xamarin.Android packages, returning multiple FileInfo objects for the signed APK file
    /// </summary>
    ///
    /// <param name="setParams">Function used to override the default build parameters</param>
    /// <param name="apkFiles">FileInfo object for an unsigned APK file to sign and align</param>
    let AndroidSignAndAlignPackages setParams apkFiles =
        apkFiles |> Seq.map (fun f -> AndroidSignAndAlign setParams f)

    /// <summary>
    /// The iOS archive parameter type
    /// </summary>
    type iOSArchiveParams =
        {
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
    let iOSArchiveDefaults =
        { SolutionPath = ""
          ProjectName = ""
          Configuration = "Debug|iPhoneSimulator"
          MDToolPath = "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" }

    /// <summary>
    /// Archive a project using Xamarin's iOS archive tools
    /// </summary>
    ///
    /// <param name="setParams">Function used to override the default archive parameters</param>
    let iOSArchive setParams =
        let archiveProject param =
            let projectNameArg =
                if param.ProjectName <> "" then
                    String.Format("-p:{0} ", param.ProjectName)
                else
                    ""

            let args =
                String.Format(@"-v archive ""-c:{0}"" {1}{2}", param.Configuration, projectNameArg, param.SolutionPath)

            executeCommand param.MDToolPath args

        iOSArchiveDefaults |> setParams |> archiveProject
