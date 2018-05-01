[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.Deploy.Web.Module.ApiModels
open System

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<CLIMutable>]
type RollbackRequest = {
    agentUrl : string
    version : string
    appName : string
}

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<CLIMutable>]
type DeployResponse = {
    Agent : string
    Messages : seq<string>
    Success : bool
    Error : string
}

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
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

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<CLIMutable>]
type TeamcityArtifact = { 
    Name : string
    Server : Uri
    href : string
    Build : TeamcityBuild
}
