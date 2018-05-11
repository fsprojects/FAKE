[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.Deploy.Web.Module.ApiModels
open System

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
type RollbackRequest = {
    agentUrl : string
    version : string
    appName : string
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
type DeployResponse = {
    Agent : string
    Messages : seq<string>
    Success : bool
    Error : string
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
type TeamcityArtifact = { 
    Name : string
    Server : Uri
    href : string
    Build : TeamcityBuild
}
