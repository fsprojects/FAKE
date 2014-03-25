module Fake.Deploy.Web.Module.ApiModels
open System


[<CLIMutable>]
type RollbackRequest = {
    agentUrl : string
    version : string
    appName : string
}

[<CLIMutable>]
type DeployResponse = {
    Agent : string
    Messages : seq<string>
    Success : bool
    Error : string
}

[<CLIMutable>]
type TeamcityBuild = {
    Id : string
    Number : string
    ProjectId : string
    ProjectName : string
    BuildConfigId : string
    Date : DateTime
    Branch: string
}

[<CLIMutable>]
type TeamcityArtifact = { 
    Name : string
    Server : Uri
    href : string
    Build : TeamcityBuild
}
