/// Contains tasks to interact with [HockeyApp](http://hockeyapp.com)
module Fake.HockeyAppHelper

open Microsoft.FSharp.Core
open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Text
open System.Text.RegularExpressions
open Fake
open Newtonsoft.Json

/// The release type of the app
type ReleaseType = Beta = 0 | Store = 1 | Alpha = 2 | Enterprise = 3

/// HockeyApp's success response
type HockeyResponse = {
    Title : string
    
    [<JsonProperty("Bundle_Identifier")>]
    BundleIdentifier: string

    [<JsonProperty("Public_Identifier")>]
    PublicIdentifier: string

    [<JsonProperty("Device_Family")>]
    DeviceFamily: string

    [<JsonProperty("Minimum_OS_Version")>]
    MinimumOSVersion: string

    [<JsonProperty("Release_Type")>]
    ReleaseType : ReleaseType
    
    Platform : string

    Status : int

    [<JsonProperty("Config_Url")>]
    ConfigUrl : string

    [<JsonProperty("Public_Url")>]
    PublicUrl : string
}

/// The HockeyApp parameter type
type HockeyAppUploadParams = {
    /// (Required) API token
    ApiToken: string

    /// (Required) file data for the build (.ipa or .apk)
    File: string

    /// Release notes for the build
    Notes: string    

    /// Set the release type of the app
    ReleaseType: ReleaseType

    /// Set the owner of the app
    OwnerId: string
}

/// The default HockeyApp parameters
let HockeyAppUploadDefaults = {
    ApiToken = String.Empty
    File = String.Empty
    Notes = String.Empty
    ReleaseType = ReleaseType.Beta
    OwnerId = String.Empty
}

/// [omit]
let private nl = Environment.NewLine

/// [omit]
let private validateParams param =
    if param.ApiToken = "" then failwith "You must provide your API token"
    if param.File = "" then failwith "You must provide an app file to upload"
    if not <| File.Exists param.File then
        failwithf "No such file: %s" param.File

    param

/// [omit]
let private toCurlArgs param = seq {
    yield (String.Format("-sL -w \"{0}%{{http_code}}{0}\"", Regex.Escape(nl)))
    yield sprintf "-H \"X-HockeyAppToken:%s\"" param.ApiToken
    yield sprintf "-F \"ipa=@%s\"" param.File
    yield sprintf "-F \"notes=%s\"" param.Notes
    yield sprintf "-F \"release_type=%i\"" (int param.ReleaseType)
    if not (String.IsNullOrEmpty param.OwnerId) then yield sprintf "-F \"owner_id=%s\"" param.OwnerId
    yield "https://rink.hockeyapp.net/api/2/apps/upload"
}

/// Uploads an app to HockeyApp
/// ## Parameters
///  - `setParams` - Function used to override the default parameters
let HockeyApp (setParams: HockeyAppUploadParams -> HockeyAppUploadParams) =
    HockeyAppUploadDefaults
    |> setParams
    |> validateParams
    |> toCurlArgs
    |> fun args ->
        ExecProcessAndReturnMessages (fun p ->
            p.FileName <- "curl"
            p.Arguments <- (String.concat " " args)
        ) (TimeSpan.FromMinutes 2.)
    |> fun response ->
        let error = sprintf "Error while posting to HockeyApp.%sMessages: %s%sErrors: %s%s" nl (String.concat "; " response.Messages) nl (String.concat "; " response.Errors) nl
        match response.ExitCode with
            | 0 ->
                match Int32.TryParse (response.Messages.[response.Messages.Count - 1].Trim()) with
                    | (false, _) -> failwith error
                    | (true, responseCode) ->
                        match responseCode with
                            | 201 -> JsonConvert.DeserializeObject<HockeyResponse>(response.Messages.[0])
                            | _ -> failwith error
            | _ -> failwith error