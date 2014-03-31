[<AutoOpen>]
/// Contains functions which allow FAKE to interact with the [TeamCity REST API](http://confluence.jetbrains.com/display/TCD8/REST+API).
module Fake.TeamCityRESTHelper

/// [omit]
let prepareURL restURL (serverURL : string) = serverURL.Trim '/' + restURL

/// Returns the REST version of the TeamCity server
let getRESTVersion serverURL username password = 
    serverURL
    |> prepareURL "/httpAuth/app/rest/version"
    |> REST.ExecuteGetCommand username password

/// Record type which stores VCSRoot properties
type VCSRoot = 
    { URL : string
      Properties : Map<string, string>
      VCSName : string
      Name : string }

/// Record type which stores Build properties
type Build = 
    { ID : string
      Number : string
      Status : string
      WebURL : string }

/// Record type which stores Build configuration properties
type BuildConfiguration = 
    { ID : string
      Name : string
      WebURL : string
      ProjectID : string
      Paused : bool
      Description : string
      Builds : Build seq }

/// Record type which stores TeamCity project properties
type Project = 
    { ID : string
      Name : string
      Description : string
      WebURL : string
      Archived : bool
      BuildConfigs : string seq }

/// [omit]
let getFirstNode serverURL username password url = 
    serverURL
    |> prepareURL url
    |> REST.ExecuteGetCommand username password
    |> XMLDoc
    |> DocElement

/// Gets information about a build configuration from the TeamCity server.
let getBuildConfig serverURL username password id = 
    sprintf "/httpAuth/app/rest/buildTypes/id:%s" id
    |> getFirstNode serverURL username password
    |> parse "buildType" (fun n -> 
           { ID = getAttribute "id" n
             Name = getAttribute "name" n
             Description = getAttribute "description" n
             WebURL = getAttribute "webUrl" n
             Paused = getAttribute "paused" n |> System.Boolean.Parse
             ProjectID = parseSubNode "project" (getAttribute "id") n
             Builds = [] })

/// Gets informnation about a project from the TeamCity server.
let getProject serverURL username password id = 
    sprintf "/httpAuth/app/rest/projects/id:%s" id
    |> getFirstNode serverURL username password
    |> parse "project" (fun n -> 
           { ID = getAttribute "id" n
             Name = getAttribute "name" n
             Description = getAttribute "description" n
             WebURL = getAttribute "webUrl" n
             Archived = getAttribute "archived" n |> System.Boolean.Parse
             BuildConfigs = parseSubNode "buildTypes" getChilds n |> Seq.map (getAttribute "id") })

/// Gets all projects on the TeamCity server.
let getProjects serverURL username password = 
    getFirstNode serverURL username password "/httpAuth/app/rest/projects"
    |> parse "projects" getChilds
    |> Seq.map (getAttribute "id")
