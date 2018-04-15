/// Contains tasks to interact with [HockeyApp](http://hockeyapp.com)
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
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
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
type ReleaseType = 
    | Beta = 0 
    | Store = 1 
    | Alpha = 2 
    | Enterprise = 3

/// The notification options
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
type NotifyOption = 
    | None = 0 
    | CanInstallApp = 1 
    | All = 2

/// The note types
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
type NoteType = 
    | Textile = 0 
    | Markdown = 1

/// The mandatory options
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
type MandatoryOption = 
    | NotMandatory = 0 
    | Mandatory = 1

/// The release download status
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
type DownloadStatusOption =
    | NotDownloadable = 1
    | Downloadable = 2

/// HockeyApp's success response
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
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

    AppSize : int64
}

/// HockeyAppVersion's success response
/// https://support.hockeyapp.net/kb/api/api-versions#create-version
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
type HockeyVersionResponse = {
    Title:  string

    Timestamp : int64

    Id : string

    Version : string

    ShortVersion : string

    Status : int

    [<JsonProperty("Config_Url")>]
    ConfigUrl : string
    
    [<JsonProperty("Public_Url")>]
    PublicUrl : string
}    

[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
type BaseHockeyAppParams = 
    /// (Required) API token
    abstract ApiToken: string

    /// Set to your App Id (required for UWP apps targeting windows phone)
    abstract AppId: string

    /// Set maximum upload delay
    abstract UploadTimeout: TimeSpan
    
/// The HockeyAppVersion parameter type
/// Based on https://support.hockeyapp.net/kb/api/api-versions#create-version
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
[<CLIMutable>]
type HockeyAppVersionParams = 
    { 
        /// (Required) API token
        ApiToken: string 

        /// Set to your App Id (required for UWP apps targeting windows phone)
        AppId: string

        /// Human readable version
        Version: string

        /// Set maximum upload delay
        UploadTimeout: TimeSpan 
    }
    interface BaseHockeyAppParams with
        member this.ApiToken = this.ApiToken
        member this.AppId = this.AppId
        member this.UploadTimeout = this.UploadTimeout

/// The HockeyApp parameter type
/// Based on http://support.hockeyapp.net/kb/api/api-apps#upload-app
[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
[<CLIMutable>]
type HockeyAppUploadParams = 
    {
        /// (Required) API token
        ApiToken: string

        /// (Required) file data for the build (.ipa or .apk)
        File: string

        /// file data for dsym (IOS: *.dysm.zip or Android: mapping.txt)
        Dsym: string

        /// Release notes for the build
        Notes: string

        /// Release notes type for the build
        NotesType: NoteType

        /// Set the release type of the app
        ReleaseType: ReleaseType

        /// Set the owner of the app
        OwnerId: string

        /// Set the notify option
        Notify: NotifyOption

        /// Set version as mandatory
        Mandatory: MandatoryOption

        /// Set to true to enable the private download page (default is false)
        Private: bool

        /// Set to the git commit sha for this build
        CommitSHA: string

        /// Set to the URL of the build job on your build server
        BuildServerUrl: string

        /// Set to your source repository
        RepositoryUrl: string

        /// Release download status (can only be set with full-access tokens)
        DownloadStatus: DownloadStatusOption

        /// Restrict download to specific teams
        Teams: string[]

        /// Set maximum upload delay
        UploadTimeout: TimeSpan

        /// Set to your App Id (required for UWP apps targeting windows phone)
        AppId: string

        /// When uploading a build, specify to which version (hockeyapp version id)
        VersionId: string
    }
    interface BaseHockeyAppParams with
        member this.ApiToken: string = this.ApiToken
        member this.AppId: string = this.AppId
        member this.UploadTimeout = this.UploadTimeout

/// The default HockeyApp parameters to upload a build
let HockeyAppUploadDefaults = {
    ApiToken = String.Empty
    File = String.Empty
    Dsym = String.Empty
    Notes = String.Empty
    NotesType = NoteType.Textile
    ReleaseType = ReleaseType.Beta
    OwnerId = String.Empty
    Notify = NotifyOption.None
    Mandatory = MandatoryOption.NotMandatory
    Private = false
    CommitSHA = String.Empty
    BuildServerUrl = String.Empty
    RepositoryUrl = String.Empty
    DownloadStatus = DownloadStatusOption.NotDownloadable
    Teams = Array.empty
    UploadTimeout = TimeSpan.FromMinutes 2.
    AppId = String.Empty
    VersionId = String.Empty
}

/// The default HockeyAppVersion parameters to create a version
let HockeyAppVersionDefaults = {
    ApiToken = String.Empty
    AppId = String.Empty
    Version = String.Empty
    UploadTimeout = TimeSpan.FromMinutes 2.
}

/// [omit]
let private nl = Environment.NewLine

/// [omit]
let private validateParams param:HockeyAppUploadParams =
    if param.ApiToken = "" then failwith "You must provide your API token"
    if param.File = "" then failwith "You must provide an app file to upload"
    if not <| File.Exists param.File then
        failwithf "No such file: %s" param.File

    if not (String.IsNullOrEmpty param.Dsym) then
        if not (param.Dsym.EndsWith(".dsym.zip", StringComparison.InvariantCultureIgnoreCase) 
        || param.Dsym.Equals("mapping.txt", StringComparison.InvariantCultureIgnoreCase)) then
            failwith "DSYM files should only be: IOS: *.dsym.zip  Android: mapping.txt"

        if param.File.EndsWith(".ipa") 
        && not (param.Dsym.EndsWith(".dsym.zip", StringComparison.InvariantCultureIgnoreCase)) then
            failwith "DSYM for an .ipa file can only only be: *.dsym.zip"

        if param.File.EndsWith(".apk") 
        && not (param.Dsym.Equals("mapping.txt", StringComparison.InvariantCultureIgnoreCase)) then
            failwith "DSYM for an .apk file can only only be: mapping.txt"

    param

/// [omit]
let private validateVersionParams (param:HockeyAppVersionParams) =
    if param.ApiToken = "" then failwith "You must provide your API token"
    if param.Version = "" then failwith "You must provide a version"
    if param.AppId = "" then failwith "You must provide an app id"
    param

/// [omit]
let private toCurlArgs (param:HockeyAppUploadParams) = seq {
    yield (String.Format("-sL -w \"{0}%{{http_code}}{0}\"", Regex.Escape(nl)))
    if not (String.IsNullOrEmpty param.VersionId) then yield "-X PUT"
    yield sprintf "-H \"X-HockeyAppToken:%s\"" param.ApiToken
    yield sprintf "-F \"ipa=@%s\"" param.File
    if not (String.IsNullOrEmpty param.Dsym) then yield sprintf "-F \"dsym=@%s\"" param.Dsym
    yield sprintf "-F \"notes=%s\"" param.Notes
    yield sprintf "-F \"notes_type=%i\"" (int param.NotesType)
    yield sprintf "-F \"release_type=%i\"" (int param.ReleaseType)
    yield sprintf "-F \"notify=%i\"" (int param.Notify)
    yield sprintf "-F \"mandatory=%i\"" (int param.Mandatory)
    yield sprintf "-F \"status=%i\"" (int param.DownloadStatus)
    yield sprintf "-F \"private=%b\"" param.Private
    yield sprintf "-F \"teams=%s\"" (param.Teams |> String.concat ",")
    if not (String.IsNullOrEmpty param.OwnerId) then yield sprintf "-F \"owner_id=%s\"" param.OwnerId
    if not (String.IsNullOrEmpty param.CommitSHA) then yield sprintf "-F \"commit_sha=%s\"" param.CommitSHA
    if not (String.IsNullOrEmpty param.BuildServerUrl) then yield sprintf "-F \"build_server_url=%s\"" param.BuildServerUrl
    if not (String.IsNullOrEmpty param.RepositoryUrl) then yield sprintf "-F \"repository_url=%s\"" param.RepositoryUrl

    if not (String.IsNullOrEmpty param.AppId) && (String.IsNullOrEmpty param.VersionId) then 
        yield sprintf "https://rink.hockeyapp.net/api/2/apps/%s/app_versions/upload" param.AppId
    else if not (String.IsNullOrEmpty param.AppId) && not (String.IsNullOrEmpty param.VersionId) then
        yield sprintf "https://rink.hockeyapp.net/api/2/apps/%s/app_versions/%s" param.AppId param.VersionId
    else
        yield "https://rink.hockeyapp.net/api/2/apps/upload"
}

/// [omit]
let private toVersionCurlArgs (param:HockeyAppVersionParams) = seq {
    yield (String.Format("-sL -w \"{0}%{{http_code}}{0}\"", Regex.Escape(nl)))
    yield "-X POST"
    yield "-H \"Content-Type: application/json\""
    yield sprintf "-d '{\"bundle_version\":\"%s\"}'" param.Version
    yield sprintf "-H \"X-HockeyAppToken:%s\"" param.ApiToken
    yield sprintf "https://rink.hockeyapp.net/api/2/apps/%s/app_versions/new" param.AppId
}

/// [omit]
let private processHockeyAppCmd<'TParam, 'TResponse when 'TParam :> BaseHockeyAppParams> defaults
    (setParams: 'TParam -> 'TParam) 
    (validateParam: 'TParam -> 'TParam) 
    (toCurlArgs: 'TParam -> seq<string>) =
    let p = defaults
            |> setParams
            |> validateParam

    p
    |> toCurlArgs
    |> fun args ->
        ExecProcessAndReturnMessages (fun p ->
            p.FileName <- "curl"
            p.Arguments <- (String.concat " " args)
        ) p.UploadTimeout
    |> fun response ->
        let error = sprintf "Error while posting to HockeyApp.%sMessages: %s%sErrors: %s%s" nl (String.concat "; " response.Messages) nl (String.concat "; " response.Errors) nl
        match response.ExitCode with
            | 0 ->
                match Int32.TryParse (response.Messages.[response.Messages.Count - 1].Trim()) with
                    | (false, _) -> failwith error
                    | (true, responseCode) ->
                        match responseCode with
                            | 201 -> JsonConvert.DeserializeObject<'TResponse>(response.Messages.[0])
                            | _ -> failwith error
            | _ -> failwith error


/// Uploads an app to HockeyApp
/// ## Parameters
///  - `setParams` - Function used to override the default parameters
///
/// ## Sample
///
///     HockeyApp (fun defaults ->
///         {defaults with
///             AppId = ...
///             ApiToken = ...
///             ...
///         })

[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
let HockeyApp setParams = processHockeyAppCmd<HockeyAppUploadParams, HockeyResponse> HockeyAppUploadDefaults setParams validateParams toCurlArgs

/// Create a new version of an app on HockeyApp
/// ## Parameters
///  - `setParams` - Function used to override the default parameters
///
/// ## Sample
///
///     HockeyAppVersions (fun defaults ->
///         {defaults with
///             AppId = ...
///             ApiToken = ...
///             Version = ...
///             ...
///         })

[<System.Obsolete("Use Fake.Api.HockeyApp instead (open Fake.Api and use 'HockeyApp.')")>]
let HockeyAppVersion setParams = processHockeyAppCmd<HockeyAppVersionParams, HockeyVersionResponse> HockeyAppVersionDefaults setParams validateVersionParams toVersionCurlArgs
