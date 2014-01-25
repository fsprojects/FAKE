[<AutoOpen>]
/// Contains tasks to upload apps to [TestFlight](http://testflightapp.com)
module Fake.TestFlightHelper

open System

/// [omit]
let private shell cmd args =
    let result = Shell.Exec (cmd, args)
    if result <> 0 then
        failwithf "%s exited with error (%d)" cmd result
    
/// The TestFlight parameter type.
type TestFlightParams = {
    ApiToken: string
    TeamToken: string
    File: string
    Notes: string option
    DSym: string option
    DistributionLists: string list
    Notify: bool
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
let private toCurlArgs parameters  = seq {
    yield "http://testflightapp.com/api/builds.json"
    yield sprintf "-F file=@%s" parameters.File
    yield sprintf "-F api_token=%s" parameters.ApiToken
    yield sprintf "-F team_token=%s" parameters.TeamToken
    yield sprintf "-F notes='%s'" (defaultArg parameters.Notes "")
    yield sprintf "-F distribution_lists='%s'" (String.concat "," parameters.DistributionLists)
    yield sprintf "-F notify=%b" parameters.Notify
    yield sprintf "-F replace=%b" parameters.Replace

    match parameters.DSym with
    | None -> ()
    | Some dsym ->
        trace <| sprintf "Zipping %s..." dsym
        let zipped = dsym + ".zip"
        shell "zip" <| sprintf "-r %s %s" zipped dsym
        yield sprintf "-F dsym=@%s" zipped
}

/// Uploads the app build to TestFlight.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default TestFlightParams value.
let TestFlight (setParams: TestFlightParams -> TestFlightParams) =
    TestFlightDefaults
    |> setParams
    |> toCurlArgs
    |> String.concat " "
    |> shell "curl"
