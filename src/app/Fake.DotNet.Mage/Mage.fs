namespace Fake.DotNet

/// <summary>
/// Contains helper functions which allow FAKE to call the
/// <a href="http://msdn.microsoft.com/en-us/library/acz3y3te.aspx">Manifest Generation and Editing Tool</a>,
/// in short <c>MAGE</c>. The intentional use is the creation of a clickonce application.
/// </summary>
///
/// <remarks>
/// Certificates
/// <br/>The MAGE tool wants to sign the manifest using a certificate. It should be clear, that this file is not
/// under source control. On the other hand - you want to be able to run the compile batch on each developer machine.
/// How can we achieve that? In the parameter structure, we use a CertFile property and a TmpCertFile property.
/// Whenever the CertFile was not found, the manifest is signed with
/// a temporary certificate. And the latter one can be shared in the source control.
/// </remarks>
[<RequireQualifiedAccess>]
module Mage =

    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators

    /// These are the supported processor types of the MAGE tool
    type Processor =
        | MSIL
        | X86
        | IA64
        | AMD64

    /// The supported commands of the MAGE tool
    type internal MageCall =
        | NewApp
        | UpdateApp
        | Sign
        | Deploy
        | UpdateDeploy
        | SignDeploy

    /// The level of trust to grant the application on client computers.
    type TrustLevel =
        | Internet
        | LocalIntranet
        | FullTrust

    /// <summary>
    /// Needed information to call MAGE
    /// </summary>
    type MageParams =
        { ToolsPath: string
          ProjectFiles: seq<string>
          Name: string
          IconPath: string
          IconFile: string
          Processor: Processor
          Version: string
          Manifest: string
          FromDirectory: string
          ApplicationFile: string
          TrustLevel: TrustLevel option
          CertFile: string option
          TmpCertFile: string
          Password: string option
          CertHash: string option
          IncludeProvider: bool option
          Install: bool option
          UseManifest: bool option
          Publisher: string option
          CodeBase: string option
          ProviderURL: string
          SupportURL: string option }

    /// Convert the parameter structure into command line arguments of MAGE
    let private serializeParams (action: MageCall) (mp: MageParams) =
        let processorStr =
            match mp.Processor with
            | MSIL -> "msil"
            | X86 -> "x86"
            | IA64 -> "ia64"
            | AMD64 -> "amd64"

        let processor = "-p " + processorStr

        let name =
            if String.isNullOrEmpty mp.Name then
                ""
            else
                "-n \"" + mp.Name + "\""

        let iconFile =
            if String.isNullOrEmpty mp.IconFile then
                ""
            else
                "-if \"" + mp.IconFile + "\""

        let version =
            if String.isNullOrEmpty mp.Version then
                ""
            else
                "-v " + mp.Version

        let fromDir =
            if String.isNullOrEmpty mp.FromDirectory then
                ""
            else
                "-fd " + mp.FromDirectory

        let manifest =
            if String.isNullOrEmpty mp.Manifest then
                ""
            else
                "-appm " + mp.Manifest

        let certFile =
            match mp.CertFile with
            | None -> ""
            | Some p ->
                if not (File.exists p) then
                    "-cf " + mp.TmpCertFile
                else
                    "-cf " + p

        let password =
            match mp.Password with
            | None -> ""
            | Some p ->
                if String.isNullOrEmpty certFile then ""
                else if not (File.exists p) then ""
                else "-pwd " + File.readLine p

        let certHash =
            match mp.CertHash with
            | None -> ""
            | Some p -> "-ch " + "\"" + p + "\""

        let trustLevelStr =
            match mp.TrustLevel with
            | None -> ""
            | Some p ->
                match p with
                | Internet -> "Internet"
                | LocalIntranet -> "LocalIntranet"
                | FullTrust -> "FullTrust"

        let trustLevel =
            if String.isNullOrEmpty trustLevelStr then
                ""
            else
                "-tr " + trustLevelStr

        let includeProvider =
            match mp.IncludeProvider with
            | None -> ""
            | Some p -> if p then "-ip true" else "ip false"

        let install =
            match mp.Install with
            | None -> ""
            | Some p -> if p then "-i true" else "-i false"

        let useManifest =
            match mp.UseManifest with
            | None -> ""
            | Some p -> if p then "-um true" else "-um false"

        let publisher =
            match mp.Publisher with
            | None -> ""
            | Some p -> "-pub \"" + p + "\""

        let codeBase =
            match mp.CodeBase with
            | None -> ""
            | Some p -> "-appc \"" + p + "\""

        let providerUrl =
            if String.isNullOrEmpty mp.ProviderURL then
                ""
            else
                "-pu \"" + mp.ProviderURL + "\""

        let supportUrl =
            match mp.SupportURL with
            | None -> ""
            | Some p -> "-s \"" + p + "\""

        let algorithm = "-a sha256RSA" // "sha1RSA"

        let allParameters =
            match action with
            | NewApp ->
                [ processor
                  name
                  iconFile
                  version
                  fromDir
                  trustLevel
                  algorithm ]
            | UpdateApp ->
                [ processor
                  name
                  iconFile
                  version
                  fromDir
                  trustLevel
                  algorithm ]
            | Sign -> [ certFile; password; certHash ]
            | Deploy ->
                [ processor
                  name
                  version
                  manifest
                  includeProvider
                  install
                  useManifest
                  publisher
                  providerUrl
                  supportUrl
                  codeBase
                  trustLevel
                  algorithm ]
            | UpdateDeploy ->
                [ processor
                  name
                  version
                  manifest
                  includeProvider
                  install
                  useManifest
                  publisher
                  providerUrl
                  supportUrl
                  codeBase
                  trustLevel
                  algorithm ]
            | SignDeploy -> [ certFile; password; certHash ]

        allParameters |> String.separated " "

    /// <summary>
    /// Execute the MAGE tool. Adds some parameters, dependent on the MAGE command.
    /// </summary>
    let internal call (action: MageCall) (mp: MageParams) =
        let magePath = mp.ToolsPath </> "mage.exe"

        let call =
            match action with
            | NewApp -> "New Application -t \"" + mp.Manifest + "\""
            | UpdateApp -> "Update \"" + mp.Manifest + "\""
            | Sign -> "Sign " + mp.Manifest
            | Deploy -> "New Deployment -t \"" + mp.ApplicationFile + "\""
            | UpdateDeploy -> "Update \"" + mp.ApplicationFile + "\""
            | SignDeploy -> "Sign \"" + mp.ApplicationFile + "\""

        let args = "-" + call + " " + serializeParams action mp

        let processResult =
            CreateProcess.fromRawCommandLine magePath args
            |> CreateProcess.withTimeout System.TimeSpan.MaxValue
            |> Proc.run

        if processResult.ExitCode <> 0 then
            failwithf "Error during mage call with exit code %i." processResult.ExitCode

    /// <summary>
    /// Encapsulates the MAGE call to create a new application's manifest
    /// </summary>
    let createApp = call NewApp

    /// <summary>
    /// Encapsulates the MAGE call to update an existing application's manifest
    /// </summary>
    let updateApp = call UpdateApp

    /// <summary>
    /// Encapsulates the MAGE call to sign an application's manifest
    /// </summary>
    let signManifest = call Sign

    /// <summary>
    /// Encapsulates the MAGE call to deploy an application
    /// </summary>
    let deployApp = call Deploy

    /// <summary>
    /// Encapsulates the MAGE call to update the deployment of an application
    /// </summary>
    let updateDeploy = call UpdateDeploy

    /// <summary>
    /// Encapsulates the MAGE call to sign the deployment of an application
    /// </summary>
    let signDeploy = call SignDeploy

    /// <summary>
    /// Executes a full run of MAGE commands: first, it creates a new manifest file. Then it signs the manifest,
    /// deploys the application and finally signs the deployment.
    /// </summary>
    ///
    /// <param name="mp">Mage parameters</param>
    let run (mp: MageParams) =
        Trace.traceStartTaskUnsafe "Fake.Tools.Mage" mp.ApplicationFile
        createApp mp
        signManifest mp
        deployApp mp
        signDeploy mp
