/// The [SignTool](https://docs.microsoft.com/en-gb/windows/win32/seccrypto/signtool) tool is a command-line tool that digitally signs files, verifies signatures in files, or time stamps files.
/// 
/// [Documentation & samples](/fake-tools-signtool.html)
/// 
[<RequireQualifiedAccess>]
module Fake.Tools.SignTool

open System
open System.IO
open System.Text
open Fake.Core
open Fake.IO


/// Verbosity
type Verbosity =
    /// Displays no output on successful execution and minimal output for failed execution. (signtool option: /q)
    | Quiet
    /// Displays verbose output for successful execution, failed execution, and warning messages. (signtool option: /v)
    | Verbose

/// Digest algorithm
type DigestAlgorithm = SHA1 | SHA256

/// Specifies the URL of the time stamp server and the digest algorithm used by the RFC 3161 time stamp server.
type TimeStampOption =
    {
        /// Specifies the URL of the time stamp server. (signtool options: /t URL, /tr URL)
        ServerUrl: string
        /// Used to request a digest algorithm used by the RFC 3161 time stamp server. (signtool option: /td alg)
        Algorithm: DigestAlgorithm option
    }

    /// Options default values.
    static member Create(serverUrl) = {
        ServerUrl = serverUrl
        Algorithm = None
    }

/// Specifies parameters to use when using a certificate from a file.
type CertificateFromFile =
    {
        /// Specifies the signing certificate in a file. Only the Personal Information Exchange (PFX) file format is supported. If the file is in PFX format protected by a password, use the /p option to specify the password. If the file does not contain private keys, use the /csp and /k options to specify the CSP and private key container name, respectively. (signtool option: /f SignCertFile)
        Path: string
        /// Specifies the password to use when opening a PFX file. A PFX file can be specified by using the /f option. (signtool option: /p Password)
        Password: string option
        /// Specifies the cryptographic service provider (CSP) that contains the private key container. (signtool option: /csp CSPName)
        CspName: string option
        /// Specifies the key that contains the name of the private key. (signtool option: /kc Name)
        PrivateKeyKey: string option
    }

    /// Options default values.
    static member Create(path) = {
        Path = path
        Password = None
        CspName = None
        PrivateKeyKey = None
    }

/// Specifies parameters to use when using a certificate from a certificate store.
type CertificateFromStore =
    {
        /// Selects the best signing certificate automatically. If this option is not present, SignTool expects to find only one valid signing certificate. (signtool option: /a)
        AutomaticallySelectCertificate: bool option
        /// Specifies the name of the issuer of the signing certificate. This value can be a substring of the entire issuer name. (signtool option: /i IssuerName)
        IssuerName: string option
        /// Specifies the name of the subject of the signing certificate. This value can be a substring of the entire subject name. (signtool option: /n SubjectName)
        SubjectName: string option
        /// Specifies the name of the subject of the root certificate that the signing certificate must chain to. This value can be a substring of the entire subject name of the root certificate. (signtool option: /r RootSubjectName)
        RootSubjectName: string option
        /// Specifies the store to open when searching for the certificate. If this option is not specified, the My store is opened. If the store does not exist, signtool will wail with a "File not found" error. (signtool option: /s StoreName)
        StoreName: string option
        /// Specifies the SHA1 hash of the signing certificate. When viewing a certificate, this is the value of the Thumbprint field. (signtool option: /sha1 Hash)
        Hash: string option
        /// Specifies that a computer store, instead of a user store, be used. Accessing the computer store requires admin rights. If the process does not have admin rights, no certificates will be found. (signtool option: /sm)
        UseComputerStore: bool option
    }

    /// Options default values.
    static member Create() = {
        AutomaticallySelectCertificate = None
        IssuerName = None
        SubjectName = None
        RootSubjectName = None
        StoreName = None
        Hash = None
        UseComputerStore = None
    }

/// Specifies what type of certificate to use.
type SignCertificate =
    /// Use a certificate stored in a file.
    | File of CertificateFromFile
    /// Use a certificate stored in a certificate store.
    | Store of CertificateFromStore

    /// Use a certificate stored in a file with options
    static member FromFile(path, setOptions) =
        let options = setOptions (CertificateFromFile.Create(path))
        File options
    /// Use a certificate stored in a certificate store with options.
    static member FromStore(setOptions) =
        let options = setOptions (CertificateFromStore.Create())
        Store options

/// Sign command options
type SignOptions =
    {
        /// Specifies the certificate to use for signing. (signtool options: /a, /f, /p, /csp, /kc, /i, /n, /r, /s, /sha1, /sm)
        Certificate: SignCertificate
        /// Specifies the file digest algorithm to use to create file signatures. The default algorithm is Secure Hash Algorithm (SHA-1). (signtool option: /fd)
        DigestAlgorithm: DigestAlgorithm option
        /// Specifies a file that contains an additional certificate to add to the signature block. (signtool option: /ac FileName)
        AdditionalCertificate: string option
        /// Appends this signature. If no primary signature is present, this signature is made the primary signature. (signtool option: /as)
        AppendSignature: bool option
        /// Specifies the Certificate Template Name (a Microsoft extension) for the signing certificate. (signtool option: /c CertTemplateName)
        CertificateTemplateName: string option
        /// Specifies a description of the signed content. (signtool option: /d Desc)
        Description: string option
        /// Specifies the enhanced key usage (EKU) that must be present in the signing certificate. The usage value can be specified by OID or string. The default usage is "Code Signing" (1.3.6.1.5.5.7.3.3). (signtool option: /u Usage)
        EnhancedKeyUsage: string option
        /// Specifies using "Windows System Component Verification" (1.3.6.1.4.1.311.10.3.6). (signtool option: /uw)
        EnhancedKeyUsageW: bool option
        /// Displays debugging information. (signtool option: /debug)
        Debug: bool option
        /// Output verbosity. (signtool options: /q, /v)
        Verbosity: Verbosity option
        /// Path to signtool.exe.
        /// If not provided, an attempt will be made to locate it automatically in 'Program Files (x86)\Windows Kits'.
        ToolPath: string option
        /// Timeout.
        Timeout: TimeSpan option
        /// Working directory.
        /// If not provided, current directory will be used.
        WorkingDir: string option
    }

    /// Options default values.
    static member Create(certificate) = {
        Certificate = certificate
        DigestAlgorithm = None
        AdditionalCertificate = None
        AppendSignature = None
        CertificateTemplateName = None
        Description = None
        EnhancedKeyUsage = None
        EnhancedKeyUsageW = None
        Debug = None
        Verbosity = None
        ToolPath = None
        Timeout = None
        WorkingDir = None
    }

/// Timestamp command options
type TimeStampOptions =
    {
        /// Specifies the URL of the time stamp server. (signtool options: /t URL, /tr URL)
        ServerUrl: string
        /// Used to request a digest algorithm used by the RFC 3161 time stamp server. (signtool option: /td alg)
        Algorithm: DigestAlgorithm option
        /// Adds a timestamp to the signature at index. (signtool option: /tp Index)
        TimestampIndex: int option
        /// Displays debugging information. (signtool option: /debug)
        Debug: bool option
        /// Output verbosity. (signtool options: /q, /v)
        Verbosity: Verbosity option
        /// Path to signtool.exe.
        /// If not provided, an attempt will be made to locate it automatically in 'Program Files (x86)\Windows Kits'.
        ToolPath: string option
        /// Timeout.
        Timeout: TimeSpan option
        /// Working directory.
        /// If not provided, current directory will be used.
        WorkingDir: string option
    }

    /// Options default values.
    static member Create(serverUrl) = {
        ServerUrl = serverUrl
        Algorithm = None
        TimestampIndex = None
        Debug = None
        Verbosity = None
        ToolPath = None
        Timeout = None
        WorkingDir = None
    }

/// Verify command options
type VerifyOptions =
    {
        /// Specifies that all methods can be used to verify the file. First, the catalog databases are searched to determine whether the file is signed in a catalog. If the file is not signed in any catalog, SignTool attempts to verify the file's embedded signature. This option is recommended when verifying files that may or may not be signed in a catalog. Examples of files that may or may not be signed include Windows files or drivers. (signtool option: /a)
        AllMethods: bool option
        /// Verifies all signatures in a file with multiple signatures. (signtool option: /all)
        AllSignatures: bool option
        /// Print the description and description URL. (signtool option: /d)
        PrintDescription: bool option
        /// Verifies the signature at a certain position. (signtool option: /ds Index)
        VerifyIndex: int option
        /// Performs the verification by using the x64 kernel-mode driver signing policy. (signtool option: /kp)
        UseX64KernelModeDriverSigningPolicy: bool option
        /// Uses multiple verification semantics. This is the default behavior of a WinVerifyTrust call. (signtool option: /ms)
        UseMultipleVerificationSemantics: bool option
        /// Verifies the file by operating system version. The version parameter is of the form: PlatformID&ast;&ast;:VerMajor.VerMinor.&ast;&ast;BuildNumber. The use of the /o switch is recommended. If /o is not specified SignTool may return unexpected results. For example, if you do not include the /o switch, then system catalogs that validate correctly on an older OS may not validate correctly on a newer OS. (signtool option: /o Version)
        VerifyByOperatingSystemVersion: string option
        /// Specifies that the Default Authentication Verification Policy is used. If the /pa option is not specified, SignTool uses the Windows Driver Verification Policy. This option cannot be used with the catdb options. (signtool option: /pa)
        UseDefaultAuthenticationVerificationPolicy: bool option
        /// Specifies the name of the subject of the root certificate that the signing certificate must chain to. This value can be a substring of the entire subject name of the root certificate. (signtool option: /r RootSubjectName)
        RootSubjectName: string option
        /// Specifies that a warning is generated if the signature is not time stamped. (signtool option: /tw)
        WarnIfNotTimeStamped: bool option
        /// Displays debugging information. (signtool option: /debug)
        Debug: bool option
        /// Output verbosity. (signtool options: /q, /v)
        Verbosity: Verbosity option
        /// Path to signtool.exe.
        /// If not provided, an attempt will be made to locate it automatically in 'Program Files (x86)\Windows Kits'.
        ToolPath: string option
        /// Timeout.
        Timeout: TimeSpan option
        /// Working directory.
        /// If not provided, current directory will be used.
        WorkingDir: string option
    }

    /// Options default values.
    static member Create() = {
        AllMethods = None
        AllSignatures = None
        PrintDescription = None
        VerifyIndex = None
        UseX64KernelModeDriverSigningPolicy = None
        UseMultipleVerificationSemantics = None
        VerifyByOperatingSystemVersion = None
        UseDefaultAuthenticationVerificationPolicy = None
        RootSubjectName = None
        WarnIfNotTimeStamped = None
        Debug = None
        Verbosity = None
        ToolPath = None
        Timeout = None
        WorkingDir = None
    }


/// run signtool command with options and files
let internal signtool runner (signtoolexeLocator: unit -> string option) (args: Arguments) toolPath timeout workingDir =
    let signtoolPath =
        match toolPath with
        | Some p -> p
        | None ->
            match signtoolexeLocator () with
            | Some p -> p
            | None -> failwith "SignTool failed: Could not locate signtool.exe. Make sure you have Windows SDKs installed or provide direct path in the ToolPath option."
    let signtoolWorkingDir = workingDir |> Option.defaultValue (Directory.GetCurrentDirectory())
    runner signtoolPath args signtoolWorkingDir timeout


/// default runner
let internal defaultRunner (signtoolPath: string) (signtoolArgs: Arguments) (signtoolWorkingDir: string) (signtoolTimeout: TimeSpan option) =
    let stdOut = StringBuilder()
    let stdErr = StringBuilder()
    let result =
        CreateProcess.fromCommand (RawCommand (signtoolPath, signtoolArgs))
        |> CreateProcess.withWorkingDirectory signtoolWorkingDir
        |> (fun cp ->
            match signtoolTimeout with
            | Some t -> CreateProcess.withTimeout t cp
            | None -> cp)
        |> CreateProcess.redirectOutput
        |> CreateProcess.withOutputEvents (stdOut.AppendLine >> ignore) (stdErr.AppendLine >> ignore)
        |> Proc.run
    Trace.log (stdOut.ToString())
    if result.ExitCode <> 0 then
        sprintf "SignTool failed: %s" (stdErr.ToString())
        |> TraceSecrets.guardMessage
        |> failwith

/// default signtool.exe locator
let internal defaultSigntoolexeLocator () =
    let winSdksDirs = [
        @"[ProgramFilesX86]\Windows Kits\8.1\bin\x86"
        @"[ProgramFilesX86]\Windows Kits\10\bin\**\x86"
    ]
    ProcessUtils.tryFindFile winSdksDirs "signtool.exe"


/// append common arguments
let commonArguments command debug verbosity arguments =
    arguments
    |> Arguments.withPrefix [command]
    |> Arguments.appendIf (debug |> Option.defaultValue false) "/debug"
    |> fun args ->
        match verbosity with
        | Some v ->
            match v with
            | Quiet ->
                args |> Arguments.append ["/q"]
            | Verbose ->
                args |> Arguments.append ["/v"]
        | None ->
            args

/// append "sign"-specific arguments
let signArguments (options: SignOptions) additionalArguments files =
    let signtoolArgs =
        Arguments.Empty
        |> commonArguments "sign" options.Debug options.Verbosity
        |> Arguments.appendIf (options.AppendSignature |> Option.defaultValue false) "/as"
        |> fun args ->
            match options.Certificate with
            | File f ->
                args
                |> Arguments.append ["/f"; f.Path]
                |> Arguments.appendOption "/p" f.Password
                |> Arguments.appendOption "/csp" f.CspName
                |> Arguments.appendOption "/kc" f.PrivateKeyKey
            | Store s ->
                args
                |> Arguments.appendIf (s.AutomaticallySelectCertificate |> Option.defaultValue false) "/a"
                |> Arguments.appendOption "/i" s.IssuerName
                |> Arguments.appendOption "/n" s.SubjectName
                |> Arguments.appendOption "/r" s.RootSubjectName
                |> Arguments.appendOption "/s" s.StoreName
                |> Arguments.appendOption "/sha1" s.Hash
                |> Arguments.appendIf (s.UseComputerStore |> Option.defaultValue false) "/sm"
        |> fun args ->
            match options.DigestAlgorithm with
            | Some SHA1 ->
                args |> Arguments.append ["/fd"; "sha1"]
            | Some SHA256 | None ->
                args |> Arguments.append ["/fd"; "sha256"]
        |> Arguments.appendOption "/ac" options.AdditionalCertificate
        |> Arguments.appendOption "/c" options.CertificateTemplateName
        |> Arguments.appendOption "/d" options.Description
        |> Arguments.appendOption "/u" options.EnhancedKeyUsage
        |> Arguments.appendIf (options.EnhancedKeyUsageW |> Option.defaultValue false) "/uw"
        |> additionalArguments
        |> Arguments.append files
    signtoolArgs

/// append "timestamp"-specific arguments
let timestampArguments serverUrl algorithm arguments =
    match algorithm with
    | Some SHA1 ->
        arguments |> Arguments.append ["/t"; serverUrl]
    | Some SHA256 | None ->
        // Note from signtool.exe docs:
        // The /td switch must be declared after the /tr switch, not before.
        // If the /td switch is declared before the /tr switch, the timestamp that is returned is from an SHA1 algorithm instead of the intended SHA256 algorithm.
        arguments |> Arguments.append ["/tr"; serverUrl; "/td"; "sha256"]

/// hide password in trace output
let hidePasswordInTrace certificate =
    match Context.isFakeContext (), certificate with
    | true, File f ->
        if f.Password.IsSome then
            TraceSecrets.register "<PASSWORD>" f.Password.Value
    | _ ->
        ()


/// run the sign command using a runner
let internal signInternal runner signtoolexeLocator (options: SignOptions) (files: seq<string>) =
    let signtoolArgs = signArguments options (fun args -> args) files
    hidePasswordInTrace options.Certificate |> ignore
    signtool runner signtoolexeLocator signtoolArgs options.ToolPath options.Timeout options.WorkingDir

/// run the sign command with time stamping using a runner
let internal signWithTimeStampInternal runner signtoolexeLocator (signOptions: SignOptions) (timeStampOptions: TimeStampOption) (files: seq<string>) =
    let signtoolArgs = signArguments signOptions (timestampArguments timeStampOptions.ServerUrl timeStampOptions.Algorithm) files
    hidePasswordInTrace signOptions.Certificate |> ignore
    signtool runner signtoolexeLocator signtoolArgs signOptions.ToolPath signOptions.Timeout signOptions.WorkingDir

/// run the timestamp command using a runner
let internal timeStampInternal runner signtoolexeLocator (options: TimeStampOptions) (files: seq<string>) =
    let signtoolArgs =
        Arguments.Empty
        |> commonArguments "timestamp" options.Debug options.Verbosity
        |> timestampArguments options.ServerUrl options.Algorithm
        |> Arguments.appendOption "/tp" (options.TimestampIndex |> Option.map (fun i -> i.ToString()))
        |> Arguments.append files
    signtool runner signtoolexeLocator signtoolArgs options.ToolPath options.Timeout options.WorkingDir

/// run the verify command using a runner
let internal verifyInternal runner signtoolexeLocator (options: VerifyOptions) (files: seq<string>) =
    let signtoolArgs =
        Arguments.Empty
        |> commonArguments "verify" options.Debug options.Verbosity
        |> Arguments.appendIf (options.AllMethods |> Option.defaultValue false) "/a"
        |> Arguments.appendIf (options.AllSignatures |> Option.defaultValue false) "/all"
        |> Arguments.appendIf (options.PrintDescription |> Option.defaultValue false) "/d"
        |> Arguments.appendOption "/ds" (options.VerifyIndex |> Option.map (fun i -> i.ToString()))
        |> Arguments.appendIf (options.UseX64KernelModeDriverSigningPolicy |> Option.defaultValue false) "/kp"
        |> Arguments.appendIf (options.UseMultipleVerificationSemantics |> Option.defaultValue false) "/ms"
        |> Arguments.appendOption "/o" options.VerifyByOperatingSystemVersion
        |> Arguments.appendIf (options.UseDefaultAuthenticationVerificationPolicy |> Option.defaultValue false) "/pa"
        |> Arguments.appendOption "/r" options.RootSubjectName
        |> Arguments.appendIf (options.WarnIfNotTimeStamped |> Option.defaultValue false) "/tw"
        |> Arguments.append files
    signtool runner signtoolexeLocator signtoolArgs options.ToolPath options.Timeout options.WorkingDir


/// Signs files according to the options specified.
let sign (certificate: SignCertificate) (setOptions: SignOptions -> SignOptions) (files: seq<string>) =
    let options = setOptions (SignOptions.Create(certificate))
    signInternal defaultRunner defaultSigntoolexeLocator options files

/// Signs and time stamps files according to the options specified.
let signWithTimeStamp (certificate: SignCertificate) (setSignOptions: SignOptions -> SignOptions) (serverUrl: string) (setTimeStampOptions: TimeStampOption -> TimeStampOption) (files: seq<string>) =
    let signOptions = setSignOptions (SignOptions.Create(certificate))
    let timeStampOptions = setTimeStampOptions (TimeStampOption.Create(serverUrl))
    signWithTimeStampInternal defaultRunner defaultSigntoolexeLocator signOptions timeStampOptions files

/// Time stamps files according to the options specified. The files being time stamped must have previously been signed.
let timeStamp (serverUrl: string) (setOptions: TimeStampOptions -> TimeStampOptions) (files: seq<string>) =
    let options = setOptions (TimeStampOptions.Create(serverUrl))
    timeStampInternal defaultRunner defaultSigntoolexeLocator options files

/// Verifies files according to the options specified.
/// The SignTool verify command determines whether the signing certificate was issued by a trusted authority, whether the signing certificate has been revoked, and, optionally, whether the signing certificate is valid for a specific policy.
let verify (setOptions: VerifyOptions -> VerifyOptions) (files: seq<string>) =
    let options = setOptions (VerifyOptions.Create())
    verifyInternal defaultRunner defaultSigntoolexeLocator options files
