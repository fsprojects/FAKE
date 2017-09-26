/// Contains a task to work with Signtool.exe. SignTool is a command-line tool that digitally signs files, 
/// verifies signatures in files, or timestamps files. SignTool.signFiles calls Signtool.exe with "sign " 
/// followed by the additional specified options. (The other commands are not currently supported).
module SignTool

open System
open System.IO

open Fake

/// Perform the "sign" operation with Signtool.exe on one or more files.
/// ## Parameters
///  - `toolPath` - Path for folder containing Signtool.exe. Example: @"C:\Program Files (x86)\Windows Kits\8.1\bin\x64"
///  - `options` - A string that contains the command line options for Signtool.exe. Do not include "sign". You can build
/// this string in a typed manner by creating a SignOption list and passing it to SignTool.toCommandLineOptions.
///  - `files` - Sequence of files to sign
let signFiles (toolPath: string) (options: string) (files: seq<string>) = 
    use __ = traceStartTaskUsing "Sign" "Trying to execute Signtool.exe"

    let doubleQuote = sprintf "\"%s\""
    let exePath = toolPath @@ "Signtool.exe"

    files
    |> Seq.map doubleQuote
    |> Seq.map (fun file -> sprintf "sign %s %s" options file)
    |> Seq.iter (fun arguments ->  
        let result =
            ExecProcess (fun info ->
                info.FileName <- exePath
                info.Arguments <- arguments) TimeSpan.MaxValue
        if result <> 0 then 
            failwithf "Error during Signtool call ")

/// Typed switches for 'sign' command. Comments adapted from 
/// https://docs.microsoft.com/en-us/dotnet/framework/tools/signtool-exe and
/// https://msdn.microsoft.com/en-us/library/windows/desktop/aa387764(v=vs.85).aspx.
/// Only the most common options are included.
module Switch =
    type SignOption = 
        /// Emits "/v". 
        /// Displays verbose output for successful execution, failed execution, and warning 
        /// messages. Usually the best choice.
        | Verbose

        /// Emits "/q". 
        /// Displays no output on successful execution and minimal output for failed execution.
        | Quiet

        /// Emits "/debug".
        /// Displays debugging information. Very noisy, as it may list all certificates found.
        | Debug

        /// Emits "/a". 
        /// Automatically selects the best signing certificate. Sign Tool will find all valid certificates 
        /// that satisfy all specified conditions and select the one that is valid for the longest time. 
        /// If this option is not present, Sign Tool expects to find only one valid signing certificate.
        | A

        /// Emits "/ac [file]".
        /// Adds an additional certificate from [file] to the signature block.
        | Ac of file: string

        /// Emits "/as". 
        /// Appends this signature. If no primary signature is present, this signature is made the 
        /// primary signature.
        | As
    
        /// Emits "/c [name]". 
        /// Specifies the Certificate Template Name (a Microsoft extension) for the signing certificate.
        | C of name: string
    
        /// Emits "/csp [name]". 
        /// Specifies the cryptographic service provider (CSP) that contains the private key container.
        | Csp of name: string

        /// Emits "/d [description]". 
        /// Specifies a description of the signed content.
        | D of description: string

        /// Emits "/du [url]". 
        /// Specifies a URL for expanded description of the signed content.
        | Du of url: string

        /// Emits "/f [file]". 
        /// Specifies the signing certificate in a file. Only the Personal Information Exchange 
        /// (PFX) file format is supported. If the PFX file is protected by a password, use the /p
        /// option to specify it. If the file does not contain private keys, use the /csp and /kc 
        /// options to specify the CSP and private key container name, respectively. 'devFile' is 
        /// an optional path to a developer's certificate; if supplied, it will be used if the 
        /// primary file is not found. This provides an easy way to build on developers' machines,
        /// without storing the certificate in source control.
        | F of file: string * devFile: string option

        /// <summary>
        /// Emits "/fd [fda]". 
        /// Specifies the file digest algorithm to use to create file signatures. The default algorithm
        /// is Secure Hash Algorithm (SHA-1).
        /// </summary>
        | Fd of fda: string

        /// Emits "/i [name]". 
        /// Specifies the name of the issuer of the signing certificate. This value can be a substring 
        /// of the entire issuer name.
        | I of name: string

        /// Emits "/kc [name]". 
        /// Specifies the private key container name.
        | Kc of name: string

        /// Emits "/n [subject]". 
        /// Specifies the name of the subject of the signing certificate. This value can be a substring 
        /// of the entire subject name.
        | N of subject: string

        /// Emits "/p [password]". 
        /// Specifies the password to use when opening a PFX file. A PFX file can be specified by using 
        /// the /f option.
        | P of password: string

        /// Emits "/r [name]". 
        /// Specifies the name of the subject of the root certificate that the signing certificate must
        /// chain to. This value can be a substring of the entire subject name of the root certificate.
        | R of name: string

        /// Emits "/s [name]". 
        /// Specifies the store to open when searching for the certificate. If this option is not specified,
        /// the My store is opened.
        | S of name: string

        /// Emits "/sha1 [hash]". 
        /// Specifies the SHA1 hash of the signing certificate.
        | Sha1 of hash: string

        /// Emits "/sm". 
        /// Specifies that a machine store, instead of a user store, is used.
        | Sm

        /// Emits "/t [url]". 
        /// Specifies the URL of the time stamp server. If this option (or /tr) is not present, the signed 
        /// file will not be time stamped. A warning is generated if time stamping fails. This option cannot be used
        /// with the /tr option.
        | T of url: string

        /// Emits "/td [alg]". 
        /// Used with the /tr switch to request a digest algorithm used by the RFC 3161 time stamp 
        /// server. Note the /td switch must be declared after the /tr switch, not before. If the /td switch
        /// is declared before the /tr switch, the timestamp that is returned is from an SHA1 algorithm 
        /// instead of the intended SHA256 algorithm.
        | Td of alg: string

        /// Emits "/tr [url]". 
        /// Specifies the URL of the RFC 3161 time stamp server. If this option (or /t) is not present, 
        /// the signed file will not be time stamped. A warning is generated if time stamping fails. This option 
        /// cannot be used with the /t option.
        | Tr of url: string

        /// Emits "/u [usage]". 
        /// Specifies the enhanced key usage (EKU) that must be present in the signing certificate.
        /// The usage value can be specified by OID or string. The default usage is "Code Signing" (1.3.6.1.5.5.7.3.3).
        | U of usage: string

        /// Emits "/uw". 
        /// Specifies using "Windows System Component Verification" (1.3.6.1.4.1.311.10.3.6).
        | Uw

open Switch

// format a switch with an additional parameter
let private pair arg arg2 = 
    sprintf "%s %s" arg arg2

let private join delimiter (strings: seq<string>) =
    String.Join (delimiter, strings)

/// Convert list of SignOption switches to a string. For example:
/// ## Parameters
///  - `signOptions` - The sequence of SignOption to convert into a string
///
/// ## Sample usage
///     [
///         SignTool.SignSwitches.Verbose
///         SignTool.SignSwitches.F ("\"mycertificate\"", None)
///         SignTool.SignSwitches.T "www.myurl.com"
///     ]
///     |> SignTool.toCommandLineOptions
///
/// becomes
///
///     /f "mycertificate" /t www.myurl.com
let toCommandLineOptions (signOptions: seq<SignOption>) =
    signOptions
    |> Seq.map (fun o ->
        match o with
        | Verbose ->          "/v"
        | Quiet ->            "/q"
        | Debug ->            "/debug"
        | A ->                "/a"
        | Ac s ->   s |> pair "/ac"
        | As ->               "/as"
        | C s ->    s |> pair "/c"
        | Csp s ->  s |> pair "/csp"
        | D s ->    s |> pair "/d"
        | Du s ->   s |> pair "/du"
        | F (certFile, devCertFile) ->
            // substitute path to developer's certificate if supplied and certFile doesn't exist
            match File.Exists certFile with
            | true -> certFile
            | false -> 
                match devCertFile with
                | Some path -> path
                | None -> certFile
            |> pair "/f"
        | Fd s ->   s |> pair "/fd"
        | I s ->    s |> pair "/i"
        | Kc s ->   s |> pair "/kc"
        | N s ->    s |> pair "/n"
        | P s ->    s |> pair "/p"
        | R s ->    s |> pair "/r"
        | S s ->    s |> pair "/s"
        | Sha1 s -> s |> pair "/sha1"
        | Sm ->               "/sm"
        | T s ->    s |> pair "/t"
        | Td s ->   s |> pair "/td"
        | Tr s ->   s |> pair "/tr"
        | U s ->    s |> pair "/u"
        | Uw ->               "/uw")
    |> join " "
