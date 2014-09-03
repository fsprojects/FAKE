[<AutoOpen>]
/// Contains a task to sign assemblies using the [SignTool](http://msdn.microsoft.com/en-us/library/windows/desktop/aa387764(v=vs.85).aspx).
///
/// ## Certificates
/// The SignTool needs a certificate to sign assemblies. It is not a good idea to include a certficate in your
/// source control system, but the sign step should be usable on developer machines. Because of this, you can
/// specify a dev certificate that can safely included in your source control system. Whenever the real certificate
/// can not be found, the dev certificate will be used.
module Fake.SignToolHelper

open System
open System.IO

/// Represents a certificate file and an optional password
type SignCert = {
    File : string
    Password : string option
}

/// Parameters used for signing.
type SignParams = {
    /// The assemblies to sign
    FilesToSign : seq<string>
    /// The dev certificate that will be used when the real certificate can not be found
    DevCertificate : SignCert
    /// The optional real certificate that will be used when it is found
    Certificate : SignCert option
    /// The optional url of the timestamp server to use
    TimeStampUrl : Uri option
}

/// Signs assemblies according to the settings specified in the parameters using signtool.exe.
/// This will be looked up using the toolsPath parameter.
let Sign (toolsPath : string) (parameters : SignParams) = 
    traceStartTask "SignTool" "Trying to sign the specified assemblies"
  
    let signPath = toolsPath @@ "signtool.exe"

    let certToUse = match parameters.Certificate with
                        | Some cert -> if File.Exists cert.File then cert else parameters.DevCertificate
                        | None -> parameters.DevCertificate
    
    let baseCall = sprintf "sign /a /f \"%s\"" certToUse.File

    let withTimeStamp = baseCall + match parameters.TimeStampUrl with
                                        | Some url -> sprintf " /t \"%s\"" url.AbsoluteUri
                                        | None -> ""

    let withPassword = withTimeStamp + match certToUse.Password with
                                           | Some pass -> sprintf " /p \"%s\"" (ReadLine pass)
                                           | None -> ""

    parameters.FilesToSign
    |> Seq.iter (fun fileToSign ->  
        let withFileToSign = withPassword + sprintf " \"%s\"" fileToSign

        let result =
            ExecProcess (fun info ->
                info.FileName <- signPath
                info.Arguments <- withFileToSign) System.TimeSpan.MaxValue
        if result <> 0 then failwithf "Error during sign call ")

    traceEndTask "SignTool" "Successfully signed the specified assemblies"