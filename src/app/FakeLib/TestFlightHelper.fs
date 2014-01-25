/// Contains tasks to upload apps to [TestFlight](http://testflightapp.com)
module Fake.TestFlightHelper

open System
open System.IO

/// The TestFlight parameter type.
type TestFlightParams = {
    /// (Required) API token from testflightapp.com/account/#api
    ApiToken: string
    /// (Required) Team token from testflightapp.com/dashboard/team/edit
    TeamToken: string
    /// (Required) file data for the build (.ipa or .apk)
    File: string
    /// Release notes for the build
    Notes: string option
    /// iOS ONLY - the .dSYM corresponding to the build
    DSym: string option
    /// Distribution list names which will receive access to the build
    DistributionLists: string list
    /// Notify permitted teammates to install the build
    Notify: bool
    /// Replace binary for an existing build if one is found with the same name/bundle version
    Replace: bool
}

/// The default TestFlight upload parameters.
let TestFlightDefaults = {
    ApiToken = ""
    TeamToken = ""
    File = ""
    Notes = None
    DSym = None
    DistributionLists = []
    Notify = false
    Replace = false
}

/// [omit]
let private validateParams ps =
    if ps.ApiToken = "" then
        failwith "Get your API token at testflightapp.com/account/#api"
    if ps.TeamToken = "" then
        failwith "Get your team token at testflightapp.com/dashboard/team/edit"
    if not <| File.Exists ps.File then
        failwithf "No such file: %s" ps.File
    match ps.DSym with
    | Some dsym when not <| Directory.Exists dsym ->
        failwithf "No such file: %s" dsym
    | _ -> ()
    ps

/// [omit]
let private toCurlArgs ps = seq {
    yield "http://testflightapp.com/api/builds.json"
    yield sprintf "-F api_token=%s" ps.ApiToken
    yield sprintf "-F team_token=%s" ps.TeamToken
    yield sprintf "-F file=@%s" ps.File
    yield sprintf "-F notes='%s'" (defaultArg ps.Notes "")
    yield sprintf "-F distribution_lists='%s'" (String.concat "," ps.DistributionLists)
    yield sprintf "-F notify=%b" ps.Notify
    yield sprintf "-F replace=%b" ps.Replace

    match ps.DSym with
    | None -> ()
    | Some dsym ->
        let zipped = dsym + ".zip"
        ZipHelper.CreateZip (Path.GetDirectoryName dsym) zipped "" ZipHelper.DefaultZipLevel false (!! (dsym @@ "**"))
        yield sprintf "-F dsym=@%s" zipped
}

/// Uploads the app build to TestFlight.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default TestFlightParams value.
let TestFlight (setParams: TestFlightParams -> TestFlightParams) =
    let ps = TestFlightDefaults |> setParams |> validateParams
    let args = ps |> toCurlArgs |> String.concat " "
    let result = Shell.Exec ("curl", args)
    if result <> 0 then
        failwithf "curl exited with error (%d)" result
