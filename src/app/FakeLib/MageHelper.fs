[<AutoOpen>]
module Fake.MageHelper

open System.IO

type MageProcessor = MSIL | X86
type MageCall = NewApp | UpdateApp | Sign | Deploy | UpdateDeploy | SignDeploy
type MageTrustLevels = Internet | LocalIntranet | FullTrust
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
    IncludeProvider : bool option
    Install : bool option
    UseManifest : bool option
    Publisher : string option
    CodeBase : string option
    ProviderURL : string 
    SupportURL : string option}

let MageSerializeParams (action: MageCall) (mp : MageParams) =
  let processorStr =
    match mp.Processor with
    | MSIL -> "msil"
    | X86 -> "x86"
  
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
    | Sign -> [certFile; password] 
    | Deploy -> [processor; name; version; manifest; includeProvider; install; useManifest; publisher; providerUrl; supportUrl; codeBase; trustlevel; algorithm]
    | UpdateDeploy -> [processor; name; version; manifest; includeProvider; install; useManifest; publisher; providerUrl; supportUrl; codeBase; trustlevel; algorithm]
    | SignDeploy -> [certFile; password] 

  allParameters
  |> separated " "


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

let MageCreateApp (mp : MageParams) =
  mageCall NewApp mp

let MageUpdateApp (mp : MageParams) =
  mageCall UpdateApp mp

let MageSignManifest (mp : MageParams) =
  mageCall Sign mp

let MageDeployApp (mp : MageParams) =
  mageCall Deploy mp

let MageUpdateDeploy (mp : MageParams) =
  mageCall UpdateDeploy mp

let MageSignDeploy (mp : MageParams) =
  mageCall SignDeploy mp

let MageRun (mp : MageParams) =
  MageCreateApp mp
  MageSignManifest mp
  MageDeployApp mp
  MageSignDeploy mp
