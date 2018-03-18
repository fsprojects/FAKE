[<AutoOpen>]
/// Contains functions which allow FAKE to interact with the [TeamCity REST API](http://confluence.jetbrains.com/display/TCD8/REST+API).
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
module Fake.TeamCityRESTHelper

/// [omit]
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
let prepareURL restURL (serverURL : string) = serverURL.Trim '/' + restURL

/// Returns the REST version of the TeamCity server
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
let getRESTVersion serverURL username password =
    serverURL
    |> prepareURL "/httpAuth/app/rest/version"
    |> REST.ExecuteGetCommand username password

/// Record type which stores VCSRoot properties
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
type VCSRoot =
    { URL : string
      Properties : Map<string, string>
      VCSName : string
      Name : string }

/// Record type which stores Build properties
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
type Build =
    { ID : string
      Number : string
      Status : string
      WebURL : string }

/// Record type which stores Build configuration properties
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
type BuildConfiguration =
    { ID : string
      Name : string
      WebURL : string
      ProjectID : string
      Paused : bool
      Description : string
      Builds : Build seq }

/// Record type which stores TeamCity project properties
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
type Project =
    { ID : string
      Name : string
      Description : string
      WebURL : string
      Archived : bool
      BuildConfigs : string seq }

/// [omit]
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
let getFirstNode serverURL username password url =
    serverURL
    |> prepareURL url
    |> REST.ExecuteGetCommand username password
    |> XMLDoc
    |> DocElement

let private parseBooleanOrFalse s =
    let ok, parsed = System.Boolean.TryParse s
    if ok then parsed else false

/// Gets information about a build configuration from the TeamCity server.
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
let getBuildConfig serverURL username password id =
    sprintf "/httpAuth/app/rest/buildTypes/id:%s" id
    |> getFirstNode serverURL username password
    |> parse "buildType" (fun n ->
           { ID = getAttribute "id" n
             Name = getAttribute "name" n
             Description = getAttribute "description" n
             WebURL = getAttribute "webUrl" n
             Paused = getAttribute "paused" n |> parseBooleanOrFalse
             ProjectID = parseSubNode "project" (getAttribute "id") n
             Builds = [] })

/// Gets informnation about a project from the TeamCity server.
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
let getProject serverURL username password id =
    sprintf "/httpAuth/app/rest/projects/id:%s" id
    |> getFirstNode serverURL username password
    |> parse "project" (fun n ->
           { ID = getAttribute "id" n
             Name = getAttribute "name" n
             Description = getAttribute "description" n
             WebURL = getAttribute "webUrl" n
             Archived = getAttribute "archived" n |> parseBooleanOrFalse
             BuildConfigs = parseSubNode "buildTypes" getChilds n |> Seq.map (getAttribute "id") })

/// Gets all projects on the TeamCity server.
[<System.Obsolete("please check the Fake.BuildServer.TeamCity module for a replacement and send a PR to include this into FAKE 5 if needed.")>]
let getProjects serverURL username password =
    getFirstNode serverURL username password "/httpAuth/app/rest/projects"
    |> parse "projects" getChilds
    |> Seq.map (getAttribute "id")
