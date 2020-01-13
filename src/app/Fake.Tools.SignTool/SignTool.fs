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


/// Tool options
type ToolOptions =
    {
        /// Path to signtool.exe.
        /// If not provided, an attempt will be made to locate it automatically in 'Program Files (x86)\Windows Kits'.
        ToolPath: string option
        /// Timeout.
        /// If not provided, default value is 10 seconds per file.
        Timeout: TimeSpan option
        /// Working directory.
        /// If not provided, current directory will be used.
        WorkingDir: string option
    }

    /// Options default values.
    static member Create() = {
        ToolPath = None
        Timeout = None
        WorkingDir = None
    }

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
        /// Tool options
        ToolOptions: ToolOptions option
        /// Displays debugging information. (signtool option: /debug)
        Debug: bool option
        /// Output verbosity. (signtool options: /q, /v)
        Verbosity: Verbosity option
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
    }

    /// Options default values.
    static member Create(certificate) = {
        ToolOptions = None
        Debug = None
        Verbosity = None
        Certificate = certificate
        DigestAlgorithm = None
        AdditionalCertificate = None
        AppendSignature = None
        CertificateTemplateName = None
        Description = None
        EnhancedKeyUsage = None
        EnhancedKeyUsageW = None
    }

/// Timestamp command options
type TimeStampOptions =
    {
        /// Tool options
        ToolOptions: ToolOptions option
        /// Displays debugging information. (signtool option: /debug)
        Debug: bool option
        /// Output verbosity. (signtool options: /q, /v)
        Verbosity: Verbosity option
        /// Specifies the URL of the time stamp server. (signtool options: /t URL, /tr URL)
        ServerUrl: string
        /// Used to request a digest algorithm used by the RFC 3161 time stamp server. (signtool option: /td alg)
        Algorithm: DigestAlgorithm option
        /// Adds a timestamp to the signature at index. (signtool option: /tp Index)
        TimestampIndex: int option
    }

    /// Options default values.
    static member Create(serverUrl) = {
        ToolOptions = None
        Debug = None
        Verbosity = None
        ServerUrl = serverUrl
        Algorithm = None
        TimestampIndex = None
    }

/// Verify command options
type VerifyOptions =
    {
        /// Tool options
        ToolOptions: ToolOptions option
        /// Displays debugging information. (signtool option: /debug)
        Debug: bool option
        /// Output verbosity. (signtool options: /q, /v)
        Verbosity: Verbosity option
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
    }

    /// Options default values.
    static member Create() = {
        ToolOptions = None
        Debug = None
        Verbosity = None
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
    }


/// run signtool command with options and files
let internal signtool runner (signtoolexeLocator: unit -> string option) command (options: seq<string>) (toolOptions: ToolOptions option) (files: seq<string>) =
    let filesList = files |> List.ofSeq
    let getTimeout = Option.defaultValue (TimeSpan.FromSeconds (10.0 * float (List.length filesList)))
    let getToolPath = function
        | Some p -> p
        | None ->
            match signtoolexeLocator () with
            | Some p -> p
            | None -> failwith "SignTool failed: Could not locate signtool.exe. Make sure you have Windows SDKs installed or provide direct path in the ToolPath option."
    let getWorkingDir = Option.defaultValue (Directory.GetCurrentDirectory())
    let signtoolPath, signtoolTimeout, signtoolWorkingDir =
        match toolOptions with
        | Some o -> getToolPath o.ToolPath, getTimeout o.Timeout, getWorkingDir o.WorkingDir
        | None -> getToolPath None, getTimeout None, getWorkingDir None
    // if there are any options, join them with a space and prepend a space separator, otherwise nothing
    let optionsString = String.Join(" ", options) |> fun o -> if String.isNullOrWhiteSpace o then String.Empty else (" " + o)
    // if there are any files, quote them and join them with a space and prepend a space separator, otherwise nothing
    let filesString = if List.isEmpty filesList then String.Empty else (" \"" + String.Join("\" \"", filesList) + "\"")
    let signtoolArgs = command + optionsString + filesString

    runner signtoolPath signtoolArgs signtoolWorkingDir signtoolTimeout


/// default runner
let internal defaultRunner (signtoolPath: string) (signtoolArgs: string) (signtoolWorkingDir: string) (signtoolTimeout: TimeSpan) =
    let stdOut = StringBuilder()
    let stdErr = StringBuilder()
    let result =
        CreateProcess.fromRawCommandLine signtoolPath signtoolArgs
        |> CreateProcess.withWorkingDirectory signtoolWorkingDir
        |> CreateProcess.withTimeout signtoolTimeout
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


let private timestamping serverUrl algorithm = seq {
    yield match algorithm with
          | Some SHA1 -> sprintf "/t \"%s\"" serverUrl
          | Some SHA256 | None -> sprintf "/tr \"%s\" /td \"sha256\"" serverUrl
}
let private digesting = function
    | Some SHA1 -> "/fd \"sha1\""
    | Some SHA256 | None -> "/fd \"sha256\""
let private verbosing verbosity = seq {
    match verbosity with
    | Some v -> match v with
                | Quiet -> yield "/q"
                | Verbose -> yield "/v"
    | None -> ()
}
let private yieldIfTrue yieldVal value = seq {
    match value with
    | Some _ -> yield sprintf "/%s" yieldVal
    | None -> ()
}
let private yieldIfSome yieldGen value = seq {
    match value with
    | Some v -> yield yieldGen v
    | None -> ()
}
let private sarg = sprintf "/%s \"%s\""
let private iarg = sprintf "/%s %i"


/// run the sign command using a runner
let internal signInternal runner signtoolexeLocator (options: SignOptions) (files: seq<string>) =
    let signtoolOptions = seq {
        yield! yieldIfTrue "debug" options.Debug
        yield! verbosing options.Verbosity
        yield! yieldIfTrue "as" options.AppendSignature

        match options.Certificate with
        | File f ->
            yield sarg "f" f.Path
            yield! yieldIfSome (sarg "p") f.Password
            yield! yieldIfSome (sarg "csp") f.CspName
            yield! yieldIfSome (sarg "kc") f.PrivateKeyKey
        | Store s ->
            yield! yieldIfTrue "a" s.AutomaticallySelectCertificate
            yield! yieldIfSome (sarg "i") s.IssuerName
            yield! yieldIfSome (sarg "n") s.SubjectName
            yield! yieldIfSome (sarg "r") s.RootSubjectName
            yield! yieldIfSome (sarg "s") s.StoreName
            yield! yieldIfSome (sarg "sha1") s.Hash
            yield! yieldIfTrue "sm" s.UseComputerStore

        yield digesting options.DigestAlgorithm
        yield! yieldIfSome (sarg "ac") options.AdditionalCertificate
        yield! yieldIfSome (sarg "c") options.CertificateTemplateName
        yield! yieldIfSome (sarg "d") options.Description
        yield! yieldIfSome (sarg "u") options.EnhancedKeyUsage
        yield! yieldIfTrue "uw" options.EnhancedKeyUsageW
    }
    // hide password in trace output
    match Context.isFakeContext (), options.Certificate with
    | true, File f ->
        if f.Password.IsSome then
            // surround in quotes to lower chances of replacing non-password occurences of password-string
            TraceSecrets.register "\"<PASSWORD>\"" (sprintf "\"%s\"" f.Password.Value)
    | _ -> ()
    signtool runner signtoolexeLocator "sign" signtoolOptions options.ToolOptions files

/// run the sign command with time stamping using a runner
let internal signWithTimeStampInternal runner signtoolexeLocator (signOptions: SignOptions) (timeStampOptions: TimeStampOption) (files: seq<string>) =
    let signtoolOptions = seq {
        yield! yieldIfTrue "debug" signOptions.Debug
        yield! verbosing signOptions.Verbosity
        yield! yieldIfTrue "as" signOptions.AppendSignature

        match signOptions.Certificate with
        | File f ->
            yield sarg "f" f.Path
            yield! yieldIfSome (sarg "p") f.Password
            yield! yieldIfSome (sarg "csp") f.CspName
            yield! yieldIfSome (sarg "kc") f.PrivateKeyKey
        | Store s ->
            yield! yieldIfTrue "a" s.AutomaticallySelectCertificate
            yield! yieldIfSome (sarg "i") s.IssuerName
            yield! yieldIfSome (sarg "n") s.SubjectName
            yield! yieldIfSome (sarg "r") s.RootSubjectName
            yield! yieldIfSome (sarg "s") s.StoreName
            yield! yieldIfSome (sarg "sha1") s.Hash
            yield! yieldIfTrue "sm" s.UseComputerStore

        yield digesting signOptions.DigestAlgorithm
        yield! timestamping timeStampOptions.ServerUrl timeStampOptions.Algorithm
        yield! yieldIfSome (sarg "ac") signOptions.AdditionalCertificate
        yield! yieldIfSome (sarg "c") signOptions.CertificateTemplateName
        yield! yieldIfSome (sarg "d") signOptions.Description
        yield! yieldIfSome (sarg "u") signOptions.EnhancedKeyUsage
        yield! yieldIfTrue "uw" signOptions.EnhancedKeyUsageW
    }
    // hide password in trace output
    match Context.isFakeContext (), signOptions.Certificate with
    | true, File f ->
        if f.Password.IsSome then
            // surround in quotes to lower chances of replacing non-password occurences of password-string
            TraceSecrets.register "\"<PASSWORD>\"" (sprintf "\"%s\"" f.Password.Value)
    | _ -> ()
    signtool runner signtoolexeLocator "sign" signtoolOptions signOptions.ToolOptions files

/// run the timestamp command using a runner
let internal timeStampInternal runner signtoolexeLocator (options: TimeStampOptions) (files: seq<string>) =
    let signtoolOptions = seq {
        yield! yieldIfTrue "debug" options.Debug
        yield! verbosing options.Verbosity
        yield! timestamping options.ServerUrl options.Algorithm
        yield! yieldIfSome (iarg "tp") options.TimestampIndex
    }
    signtool runner signtoolexeLocator "timestamp" signtoolOptions options.ToolOptions files

/// run the verify command using a runner
let internal verifyInternal runner signtoolexeLocator (options: VerifyOptions) (files: seq<string>) =
    let signtoolOptions = seq {
        yield! yieldIfTrue "debug" options.Debug
        yield! verbosing options.Verbosity
        yield! yieldIfTrue "a" options.AllMethods
        yield! yieldIfTrue "all" options.AllSignatures
        yield! yieldIfTrue "d" options.PrintDescription
        yield! yieldIfSome (iarg "ds") options.VerifyIndex
        yield! yieldIfTrue "kp" options.UseX64KernelModeDriverSigningPolicy
        yield! yieldIfTrue "ms" options.UseMultipleVerificationSemantics
        yield! yieldIfSome (sarg "o") options.VerifyByOperatingSystemVersion
        yield! yieldIfTrue "pa" options.UseDefaultAuthenticationVerificationPolicy
        yield! yieldIfSome (sarg "r") options.RootSubjectName
        yield! yieldIfTrue "tw" options.WarnIfNotTimeStamped
    }
    signtool runner signtoolexeLocator "verify" signtoolOptions options.ToolOptions files


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
