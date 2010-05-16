[<AutoOpen>]
module Fake.TeamCityRESTHelper

let prepareURL restURL (serverURL:string) = serverURL.Trim '/' + restURL

/// Returns the REST version of the TeamCity server
let getRESTVersion serverURL username password =      
   serverURL
     |> prepareURL "/httpAuth/app/rest/version"
     |> REST.ExecuteGetCommand username password 

type VCSRoot =
    { URL: string;
      Properties: Map<string,string>;
      VCSName: string;
      Name: string}

type Build =
    { ID:string; 
      Number:string;
      Status:string;
      WebURL:string }

type BuildConfiguration =
    { ID: string;
      Name: string; 
      WebURL: string;
      ProjectID: string; 
      Paused: bool;
      Description: string;
      Builds: Build seq }

let getFirstNode serverURL username password url =
      serverURL
        |> prepareURL url
        |> REST.ExecuteGetCommand username password 
        |> XMLDoc
        |> DocElement
 
/// Gets a projects from the TeamCity server
let getBuildConfig serverURL username password id =
    sprintf "/httpAuth/app/rest/buildTypes/id:%s" id
      |> getFirstNode serverURL username password 
      |> parse "buildType" (fun n ->             
            { ID = getAttribute "id" n;
              Name = getAttribute "name" n;
              Description = getAttribute "description" n;
              WebURL = getAttribute "webUrl" n;
              Paused = getAttribute "paused" n |> System.Boolean.Parse;
              ProjectID = parseSubNode "project" (getAttribute "id") n;
              Builds =[] })

type Project =
    { ID: string;
      Name: string;
      Description: string;
      WebURL: string;
      Archived: bool;
      BuildConfigs: string seq}

/// Gets a projects from the TeamCity server
let getProject serverURL username password id =
    sprintf "/httpAuth/app/rest/projects/id:%s" id
      |> getFirstNode serverURL username password 
      |> parse "project" (fun n ->             
             { ID = getAttribute "id" n;
               Name = getAttribute "name" n;
               Description = getAttribute "description" n;
               WebURL = getAttribute "webUrl" n;
               Archived = getAttribute "archived" n |> System.Boolean.Parse;
               BuildConfigs = 
                   parseSubNode "buildTypes" getChilds n
                     |> Seq.map (getAttribute "id") })

/// Gets all projects on the TeamCity 
let getProjects serverURL username password =  
    getFirstNode serverURL username password "/httpAuth/app/rest/projects"  
      |> parse "projects" getChilds
      |> Seq.map (getAttribute "id")