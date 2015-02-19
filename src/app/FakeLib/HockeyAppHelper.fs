/// Contains tasks to interact with [HockeyApp](http://hockeyapp.com)
module Fake.HockeyAppHelper

open Microsoft.FSharp.Core
open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Text
open Fake
open Newtonsoft.Json

type ReleaseType = Beta = 0 | Store = 1 | Alpha = 2 | Enterprise = 3

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

    /// set the release type of the app
    ReleaseType: ReleaseType
}

/// The default HockeyApp parameters
let HockeyAppUploadDefaults = {
    ApiToken = String.Empty
    File = String.Empty
    Notes = String.Empty
    ReleaseType = ReleaseType.Beta
}

/// [omit]
let validateParams param =
    if param.ApiToken = "" then failwith "You must provide your API token"
    if param.File = "" then failwith "You must provide an app file to upload"
    if not <| File.Exists param.File then
        failwithf "No such file: %s" param.File

    param

/// [omit]
let private toCurlArgs param = seq {
    yield sprintf "-H \"X-HockeyAppToken:%s\"" param.ApiToken
    yield sprintf "-F \"ipa=@%s\"" param.File
    yield sprintf "-F \"notes=%s\"" param.Notes
    yield sprintf "-F \"release_type=%i\"" (int param.ReleaseType)
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
        match response.ExitCode with
            | 0 -> JsonConvert.DeserializeObject<HockeyResponse>(response.Messages.[0])
            | _ -> failwithf "Error while posting to HockeyApp.\r\nMessages: %s\r\nErrors: %s\r\n" (String.concat "; " response.Messages) (String.concat "; " response.Errors)