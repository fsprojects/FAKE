(*
    this module helps android developers to publish automatically their apks

    useful links:

    https://developers.google.com/accounts/docs/OAuth2ServiceAccount#formingheader
    https://developers.google.com/android-publisher/api-ref/edits/insert

    note:
        I would like to use a JsonProvider to parse json but i don't know if it causes a problem with dependency "FSharp.Data.DesignTime.dll"
        So i used Newtonsoft.Json
*)

module Fake.AndroidPublisher

open System
open System.Security.Cryptography
open System.Security.Cryptography.X509Certificates
open System.Text
open System.Net
open System.Collections.Specialized
open System.Threading
open System.IO
open System.Diagnostics
open Newtonsoft.Json
open ICSharpCode.SharpZipLib.Zip
open ICSharpCode.SharpZipLib.Core
open System.Xml.Linq

type AndroidPublishConfig = {
    Certificate: X509Certificate2;
    PackageName: string;
    AccountId: string;
    Apk: string;
}

type AndroidPublishParams = {
    Track: string;
    Config: AndroidPublishConfig;
}

let ProductionSettings = { Track = "production"; Config = { Certificate = null; PackageName = null; AccountId = null; Apk = null; } }
let AlphaSettings = { ProductionSettings with Track = "alpha"; }
let BetaSettings = { ProductionSettings with Track = "beta"; }
let RolloutSettings = { ProductionSettings with Track = "rollout"; }

type private ServiceCredentials = { 
    Certificate: X509Certificate2;
    AccountId: string; //"xxxxxxx@developer.gserviceaccount.com"
}

type private ServiceSession = {
    Token: String;
    TokenType: String;
    Expiry: DateTime;
}

[<CLIMutable>]
type private JwtHeader = {
    [<JsonProperty("alg")>]
    Algo:string;
    [<JsonProperty("typ")>]
    Type: string;
}

[<CLIMutable>]
type private JwtClaimSet = {
    [<JsonProperty("iss")>]
    AccountId: string;
    [<JsonProperty("scope")>]
    Scope: string;
    [<JsonProperty("aud")>]
    Audience: string;
    [<JsonProperty("exp")>]
    Expiry: int64;
    [<JsonProperty("iat")>]
    Issue: int64;
}

[<CLIMutable>]
type private AuthReply = {
    [<JsonProperty("access_token")>]
    AccessToken: String;
    [<JsonProperty("token_type")>]
    TokenType: String;
    [<JsonProperty("expires_in")>]
    ExpiresIn: int32;
}

[<CLIMutable>]
type EditResourceModel = { 
    [<JsonProperty("id")>]
    Id: string;
    [<JsonProperty("expiryTimeSeconds")>]
    Expiry: int64;
}

[<CLIMutable>]
type ApkVersion = {
    [<JsonProperty("versionCode")>]
    Code: int;
}

[<CLIMutable>]
type AppEditListApksResult = {
    [<JsonProperty("apks")>]
    Apks: ApkVersion list
}

[<CLIMutable>]
type TrackModel = {
    [<JsonProperty("versionCodes")>]
    VersionCodes: int list;
    [<JsonProperty("track")>]
    Track: string;
//    [<JsonProperty("userFraction")>]
//    UserFraction: float;
}

type private Result<'t> = {
    Content: 't option;
    Error: string option;
}

type private HttpClient() =
    inherit WebClient()

    override __.GetWebRequest (address:Uri) =
        let rq = base.GetWebRequest address
        rq.Timeout <- int32 <| TimeSpan.FromHours(1.).TotalMilliseconds
        rq

    member x.CreateRequest (address:Uri) =
        x.GetWebRequest (address)

let mutable public AndroidPublisherScope = "https://www.googleapis.com/auth/androidpublisher"
let mutable public TokenServerUrl = "https://www.googleapis.com/oauth2/v3/token"
let mutable public AndroidPublisherBaseUrl = "https://www.googleapis.com/androidpublisher/v2/applications"
let mutable public AndroidUploadApkBaseUrl = "https://www.googleapis.com/upload/androidpublisher/v2/applications"

let private ServiceAccountHeader = {Algo = "RS256"; Type = "JWT"}

let private toJson = JsonConvert.SerializeObject
let private fromJson<'t> v = JsonConvert.DeserializeObject<'t>(v)

let PublishApk (param:AndroidPublishParams)= 

    let getZipEntry (filepath, path) =
        let clean (name:string) = if not(name.StartsWith("/")) then ("/" + name) else name
        let rec searchEntry (zip:ZipInputStream, current:ZipEntry) =
            match current.Name |> clean  with
                | name when name = path -> 
                    use memory = new MemoryStream()
                    zip.CopyTo(memory)
                    zip.Flush()
                    memory.Flush()
                    Some(memory.ToArray())
                | _ -> 
                    match zip.GetNextEntry() with 
                        | null -> None
                        | next -> searchEntry(zip, next)

        use fs = File.OpenRead(filepath)
        let s = new ZipInputStream(fs)
        searchEntry(s, s.GetNextEntry())

    let manifestDecode (data: byte array) = 
        // inpired by http://stackoverflow.com/a/19063830/2554459
        let endDocTag = 0x00100101
        let startTag = 0x00100102
        let endTag = 0x00100103

        let lew (arr:byte array, off:int) = 
            let p1 = uint32 (arr.[off + 3]) <<< 24 &&& 0xff000000u
            let p2 = uint32 (arr.[off + 2]) <<< 16 &&& 0xff0000u
            let p3 = uint32 (arr.[off + 1]) <<< 8 &&& 0xff00u
            let p4 = uint32 (arr.[off]) &&& 0xFFu
            p1 ||| p2 ||| p3 ||| p4 |> int32

        let firstTagOffset = 
            let rec scanStartOfFirstTag offset = 
                match lew(data, offset) with
                    | v when v = startTag -> Some (offset)
                    | _ when offset < data.Length - 4 -> scanStartOfFirstTag (offset + 4)
                    | _ -> None
            let s = lew(data, 3 * 4)
            match s |> scanStartOfFirstTag with
                | Some v -> v
                | None -> s

        let readStringAt offset =   
            let len = data.[offset + 1] <<< 8 &&& byte(0xff00) ||| data.[offset] &&& byte(0xff) |> int
            [|for i in 0..len-1 -> data.[offset + 2 + i * 2] |] |> System.Text.Encoding.UTF8.GetString

        let readString (sitOff:int, stOff:int, strInd:int) =
            if strInd < 0 then null else readStringAt (stOff + lew(data, sitOff + strInd * 4))

        let numbStrings = lew(data, 4 * 4)
        let sitOff = 0x24 // Offset of start of StringIndexTable
        let stOff = sitOff + numbStrings * 4 // StringTable follows StrIndexTable
        let xmlTagOff = firstTagOffset // Start from the offset in the 3rd word.
        let rec readNode acc off =
            let tag0 = lew(data, off)
            let lineNo = lew(data, off + 2 * 4)
            let nameSi = lew(data, off + 5 * 4)
            
            match tag0 with
                | t when t = startTag -> 
                    let numbAttrs = lew(data, off + 7 * 4)
                    let name = readString(sitOff, stOff, nameSi)
                    let mutable attrOff = off + 9 * 4
                    let sb = new StringBuilder(acc + "<" + name)
                    for i in 0..numbAttrs-1 do
                        let attrNameNsSi = lew(data, attrOff)
                        let attrNameSi = lew(data, attrOff + 1 * 4)
                        let attrValueSi = lew(data, attrOff + 2 * 4)
                        let attrFlags = lew(data, attrOff + 3 * 4)
                        let attrResId = lew(data, attrOff + 4 * 4)
                        let attrName = readString(sitOff, stOff, attrNameSi)
                        let attrValue = if not(attrValueSi = -1) then readString (sitOff, stOff, attrValueSi) else attrResId.ToString()
                        (" " + attrName + "=\"" + attrValue + "\"") |> sb.Append |> ignore
                        attrOff <- attrOff + 5 * 4
                    sb.Append(">") |> ignore
                    readNode (sb.ToString()) attrOff
                | t when t = endTag -> 
                    let name = readString(sitOff, stOff, nameSi)
                    readNode (acc + "</" + name + ">\r\n") (off + 6 * 4)
                | t when t = endDocTag -> acc
                | _ -> failwith "Invalid manifest format";

        readNode "" xmlTagOff

    let getManifest (filepath) =
        match getZipEntry (filepath, "/AndroidManifest.xml") with
            | Some bytes -> Some (bytes |> manifestDecode |> System.Xml.Linq.XDocument.Parse)
            | None -> None

    let header (s:ServiceSession) = sprintf "%s %s" s.TokenType s.Token
    let (<<) (headers:WebHeaderCollection) session = headers.Add("Authorization", header session)

    let createRsaCrypto (credentials:ServiceCredentials) = 
        let pkey = credentials.Certificate.PrivateKey :?> RSACryptoServiceProvider
        let blob = pkey.ExportCspBlob(true)
        let rsaCrypto = new RSACryptoServiceProvider()
        rsaCrypto.ImportCspBlob(blob)
        rsaCrypto

    let base64EncodeUrlBytes (b:byte[]) = b |> Convert.ToBase64String |> fun b64 -> b64.Replace("=", "").Replace('+', '-').Replace('/', '_')
    let base64EncodeUrl (s:string) = s |> Encoding.UTF8.GetBytes |> base64EncodeUrlBytes

    let toUnixTime d = (d - new DateTime(1970, 1, 1, 0, 0, 0, d.Kind)).TotalSeconds |> int64

    let postUrl (url:string, data:NameValueCollection) = 
        let client = new WebClient()
        client.UploadValues(url, data)

    let googleAuthenticate (credentials:ServiceCredentials) = 
        let header = ServiceAccountHeader |> toJson |> base64EncodeUrl
        let now = DateTime.UtcNow |> toUnixTime
        let claimSet = { AccountId = credentials.AccountId; Scope = AndroidPublisherScope; Audience = TokenServerUrl; Issue = now; Expiry = now + 3600L  }
        let payload = claimSet |> toJson |> base64EncodeUrl
        let rsa = createRsaCrypto credentials
        let assertion = header + "." + payload
        let bb = rsa.SignData(Encoding.ASCII.GetBytes(assertion), "SHA256")
        let signature = bb |> base64EncodeUrlBytes
        let signedAssertion = assertion + "." + signature
        let p = new NameValueCollection();
        p.Add("assertion", signedAssertion)
        p.Add("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer")
        let auth = postUrl(TokenServerUrl, p) |> Encoding.UTF8.GetString |> fromJson<AuthReply>
        { Token = auth.AccessToken; TokenType = auth.TokenType; Expiry = DateTime.UtcNow.AddSeconds(auth.ExpiresIn |> float) }

    let appEditInsert (session:ServiceSession, packageName:string) =
        let client = new WebClient()
        client.Headers << session
        client.UploadString(AndroidPublisherBaseUrl + "/" + packageName + "/edits", "") |> fromJson<EditResourceModel>

    let appListApks (session:ServiceSession, packageName:string, editId:string) =
        let client = new WebClient()
        client.Headers << session
        client.DownloadString(AndroidPublisherBaseUrl + "/" + packageName + "/edits/" + editId + "/apks") |> fromJson<AppEditListApksResult>
    
    let validateAppEdit (session:ServiceSession, packageName:string, editId:string) =
        let client = new WebClient()
        client.Headers << session
        client.UploadString(AndroidPublisherBaseUrl + "/" + packageName + "/edits/" + editId + ":validate", "") |> fromJson<EditResourceModel>

    let commitAppEdit (session:ServiceSession, packageName:string, editId:string) =
        let client = new WebClient()
        client.Headers << session
        client.UploadString(AndroidPublisherBaseUrl + "/" + packageName + "/edits/" + editId + ":commit", "") |> fromJson<EditResourceModel>

    let setAppTrack (session:ServiceSession, packageName:string, editId:string, track:string, versionCode:int) =
        let client = new WebClient()
        client.Headers << session
        client.Headers.Add ("Content-Type", "application/json")
        let m = { Track = track; VersionCodes = [versionCode];(* UserFraction = 1.*) }
        let data = JsonConvert.SerializeObject(m)
        client.UploadString(AndroidPublisherBaseUrl + "/" + packageName + "/edits/" + editId + "/tracks/" + track, "PUT", data) |> fromJson<EditResourceModel>
    
    let fopen fn = File.Open(fn, FileMode.Open)

    let uploadApk (session:ServiceSession, packageName:string, editId:string, apkPath:string) =
        let url = new Uri(AndroidUploadApkBaseUrl + "/" + packageName + "/edits/" + editId + "/apks?uploadType=media")
        let client = new HttpClient()
        let watch = Stopwatch.StartNew()
        let rq = client.CreateRequest (url)
        rq.Method <- "POST"
        rq.ContentType <- "application/octet-stream"
        rq.Headers << session
        use file = fopen apkPath
        use binary = new BinaryReader(file)
        rq.ContentLength <- file.Length
        use stream = rq.GetRequestStream()
        use out = new BinaryWriter(stream)
        let mutable lastProgress = 0L

        while file.Position < file.Length do
            let uploaded = (file.Position/1024L/1024L)
            let bytes = binary.ReadBytes(512)
            out.Write(bytes)
            out.Flush()
            stream.Flush()
            let percent = 100L * file.Position / file.Length
            if lastProgress = uploaded |> not then
                let elapsed = watch.ElapsedMilliseconds
                let estimated = 100L*elapsed/percent
                tracefn "Estimated remaining time: %s" (TimeSpan.FromMilliseconds(float(estimated - elapsed)).ToString())
                tracefn @"Uploaded progress %d MB %d %%  ..." uploaded percent
            lastProgress <- uploaded
        stream.Flush()
        stream.Close()
        watch.Stop()
        tracefn "APK file sent"
        try
            let rs = rq.GetResponse()
            use reader = new StreamReader(rs.GetResponseStream())
            { Content=Some(fromJson<ApkVersion>(reader.ReadToEnd())); Error=None }
        with 
            | :? WebException as e -> 
                use reader = new StreamReader(e.Response.GetResponseStream())
                { Content=None; Error=Some(reader.ReadToEnd()) }
            | e -> { Content=None; Error=Some(e.Message) }


    let credentials = { Certificate = param.Config.Certificate; AccountId = param.Config.AccountId; }
    let session = credentials |> googleAuthenticate
    let resource = appEditInsert (session, param.Config.PackageName)
    let apkList = appListApks (session, param.Config.PackageName, resource.Id)
    let manifest = match param.Config.Apk |> getManifest with | Some xml -> xml | None -> failwithf "cannot parse apk AndroidManifest"
    let versionCode = (manifest.Element("manifest" |> XName.Get).Attributes() |> Seq.filter(fun a -> a.Name.LocalName = "versionCode") |> Seq.exactlyOne).Value |> Convert.ToInt32
    
    if apkList.Apks.Length > 0 && (apkList.Apks |> Seq.maxBy (fun a -> a.Code)).Code >= versionCode then
        failwithf "You must increase versionCode"                         

    let upResult = uploadApk (session, param.Config.PackageName, resource.Id, param.Config.Apk)
    match (upResult.Content, upResult.Error) with 
        | Some content, None -> 
            tracefn "upload success: version code %d \n" content.Code
            tracefn "setting track %s \n" param.Track
            setAppTrack (session, param.Config.PackageName, resource.Id, param.Track, content.Code) |> ignore
            tracefn "validating \n"
            validateAppEdit (session, param.Config.PackageName, resource.Id) |> ignore
            tracefn "committing app \n"
            commitAppEdit (session, param.Config.PackageName, resource.Id) |> ignore
        | None, Some error -> failwith error
        | _, _ -> failwith "upload failed"

    tracefn "app published"
