[<AutoOpen>]
/// Contains helper functions which allow FAKE to call the [Manifest Generation and Editing Tool](http://msdn.microsoft.com/en-us/library/acz3y3te.aspx), in short 'MAGE'.
/// The intentional use is the creation of a clickonce application.
///
/// ## Certificates
/// The MAGE tool wants to sign the manifest using a certificate. It should be clear, that this file is not under source control.
/// On the other hand - you want to be able to run the compile batch on each developer machine. How can we achieve that? 
/// In the parameter structure, we use a CertFile property and a TmpCertFile property. Whenever the CertFile was not found, the manifest is signed with
/// a temporary certificate. And the latter one can be shared in the source control.
module Fake.MageHelper

open System.IO

/// These are the supported processor types of the MAGE tool
type MageProcessor = MSIL | X86 | IA64 | AMD64
/// The supported commands of the MAGE tool
type MageCall = NewApp | UpdateApp | Sign | Deploy | UpdateDeploy | SignDeploy
/// The level of trust to grant the application on client computers.
type MageTrustLevels = Internet | LocalIntranet | FullTrust

/// Needed information to call MAGE
type MageParams =
  { ToolsPath : string 
    ProjectFiles : seq<string>
    Name : string
    IconPath : string 
    IconFile : string 
    Processor : MageProcessor
    Version : string
    Manifest : string
    FromDirectory : string
    ApplicationFile : string
    TrustLevel : MageTrustLevels option
    CertFile : string option
    TmpCertFile : string
    Password : string option
    CertHash : string option
    IncludeProvider : bool option
    Install : bool option
    UseManifest : bool option
    Publisher : string option
    CodeBase : string option
    ProviderURL : string 
    SupportURL : string option}

/// Convert the parameter structure into command line arguments of MAGE
let MageSerializeParams (action: MageCall) (mp : MageParams) =
  let processorStr =
    match mp.Processor with
    | MSIL -> "msil"
    | X86 -> "x86"
    | IA64 -> "ia64"
    | AMD64 -> "amd64"
  
  let processor = "-p " + processorStr
  let name = if isNullOrEmpty mp.Name then "" else "-n \"" + mp.Name + "\""
  let iconFile = if isNullOrEmpty mp.IconFile then "" else "-if \"" + mp.IconFile + "\""
  let version = if isNullOrEmpty mp.Version then "" else "-v " + mp.Version
  let fromDir = if isNullOrEmpty mp.FromDirectory then "" else "-fd " + mp.FromDirectory
  let manifest = if isNullOrEmpty mp.Manifest then "" else "-appm " + mp.Manifest
  let certFile = 
    match mp.CertFile with
    | None -> "" 
    | Some (p) -> 
        if not (File.Exists p) then "-cf " + mp.TmpCertFile else "-cf " + p
  let password = 
    match mp.Password with
    | None -> ""
    | Some (p) -> if isNullOrEmpty certFile then "" else if not (File.Exists p) then "" else "-pwd " + ReadLine p
  let certHash =
    match mp.CertHash with
    | None -> ""
    | Some (p) -> "-ch " + "\"" + p + "\""
  let trustlevelStr = 
    match mp.TrustLevel with
    | None -> ""
    | Some (p) ->
      match p with
      | Internet -> "Internet"
      | LocalIntranet -> "LocalIntranet"
      | FullTrust -> "FullTrust"
  let trustlevel = if isNullOrEmpty trustlevelStr then "" else "-tr " + trustlevelStr
  let includeProvider =
    match mp.IncludeProvider with
    | None -> ""
    | Some (p) -> if p then "-ip true" else "ip false"
  let install =
    match mp.Install with
    | None -> ""
    | Some (p) -> if p then "-i true" else "-i false"
  let useManifest =
    match mp.UseManifest with
    | None -> ""
    | Some (p) -> if p then "-um true" else "-um false"
  let publisher =
    match mp.Publisher with
    | None -> ""
    | Some (p) -> "-pub \"" + p + "\""
  let codeBase =
    match mp.CodeBase with
    | None -> ""
    | Some (p) -> "-appc \"" + p + "\""
  let providerUrl = if isNullOrEmpty mp.ProviderURL then "" else "-pu \"" + mp.ProviderURL + "\""
  let supportUrl =
    match mp.SupportURL with
    | None -> ""
    | Some (p) -> "-s \"" + p + "\""
  let algorithm = "-a sha256RSA" // "sha1RSA"

  let allParameters =
    match action with
    | NewApp -> [processor; name; iconFile; version; fromDir; trustlevel; algorithm]
    | UpdateApp -> [processor; name; iconFile; version; fromDir; trustlevel; algorithm]
    | Sign -> [certFile; password; certHash] 
    | Deploy -> [processor; name; version; manifest; includeProvider; install; useManifest; publisher; providerUrl; supportUrl; codeBase; trustlevel; algorithm]
    | UpdateDeploy -> [processor; name; version; manifest; includeProvider; install; useManifest; publisher; providerUrl; supportUrl; codeBase; trustlevel; algorithm]
    | SignDeploy -> [certFile; password; certHash] 

  allParameters
  |> separated " "

/// Execute the MAGE tool. Adds some parameters, dependent on the MAGE command.
let mageCall (action : MageCall) (mp : MageParams) =
  let magePath = mp.ToolsPath @@ "mage.exe"
  let call =
    match action with
    | NewApp -> "New Application -t \"" + mp.Manifest + "\""
    | UpdateApp -> "Update \"" + mp.Manifest + "\""
    | Sign -> "Sign " + mp.Manifest
    | Deploy -> "New Deployment -t \"" + mp.ApplicationFile + "\""
    | UpdateDeploy -> "Update \"" + mp.ApplicationFile + "\""
    | SignDeploy -> "Sign \"" + mp.ApplicationFile + "\""
  let args = "-" + call + " " + MageSerializeParams  action mp
  let result =
    ExecProcess (fun info ->
      info.FileName <- magePath
      info.Arguments <- args) System.TimeSpan.MaxValue
  if result <> 0 then failwithf "Error during mage call "

/// Encapsulates the MAGE call to create a new application's manifest
let MageCreateApp (mp : MageParams) =
  mageCall NewApp mp

/// Encapsulates the MAGE call to update an existing application's manifest
let MageUpdateApp (mp : MageParams) =
  mageCall UpdateApp mp

/// Encapsulates the MAGE call to sign an application's manifest
let MageSignManifest (mp : MageParams) =
  mageCall Sign mp

/// Encapsulates the MAGE call to deploy an application
let MageDeployApp (mp : MageParams) =
  mageCall Deploy mp

/// Encapsulates the MAGE call to update the deployment of an application
let MageUpdateDeploy (mp : MageParams) =
  mageCall UpdateDeploy mp

/// Encapsulates the MAGE call to sign the deployment of an application
let MageSignDeploy (mp : MageParams) =
  mageCall SignDeploy mp

/// Executes a full run of MAGE commands: first, it creates a new manifest file. Then it signs the manifest, deploys the application and finally signs the deployment.
let MageRun (mp : MageParams) =
  traceStartTask "Mage-Tool" mp.ApplicationFile
  MageCreateApp mp
  MageSignManifest mp
  MageDeployApp mp
  MageSignDeploy mp
  traceEndTask "Mage-Tool" mp.ApplicationFile