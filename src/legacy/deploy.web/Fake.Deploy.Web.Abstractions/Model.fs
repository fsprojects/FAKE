namespace Fake.Deploy.Web

open System
open System.Collections.Generic
open System.Runtime.Serialization
open System.ComponentModel.Composition
open System.Web.Security

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
[<DataContract>]
type AgentRef = {
    [<DataMember>]Id : string
    [<DataMember>]Name : string
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
[<DataContract>]
type Agent = {
    [<DataMember>]Id : string
    [<DataMember>]Name : string
    [<DataMember>]Address : Uri
    [<DataMember>]EnvironmentId : string
    }
    with
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        static member Create url environmentId name =
            let url = Uri(url)
            {
                Id = url.Host + "-" + (url.Port.ToString())
                Name = if String.IsNullOrEmpty name then url.Host else name
                Address = url
                EnvironmentId = environmentId
            }
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]        
        member x.Ref with get() : AgentRef = { Id = x.Id; Name = x.Name }


[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
[<DataContract>]
type Environment = {
        [<DataMember>]Id : string
        [<DataMember>]Name : string
        [<DataMember>]Description : string
        [<DataMember>]Agents : seq<AgentRef>
    }
    with
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        static member CreateWithId id  name desc agents =
            { Id = id; Name = name; Description = desc; Agents = agents }
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        static member Create name desc agents  =
            { Id = Guid.NewGuid().ToString(); Name = name; Description = desc; Agents = agents }

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.AddAgents(agents : seq<Agent>) = 
            { x with Agents = Seq.append (agents |> Seq.map (fun a -> a.Ref)) x.Agents |> Seq.distinct }

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.RemoveAgents(agents : seq<Agent>) =
            { x with 
                Agents = Seq.filter (fun a -> 
                                          agents 
                                          |> Seq.map (fun a -> a.Ref) 
                                          |> Seq.exists (fun b -> a = b)
                                          |> not
                                      ) x.Agents
            }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
type ParameterDescription = { 
    ParameterName : string
    Description : string
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
type SetupInfo = {
    AdministratorUserName : string
    AdministratorEmail : string
    AdministratorPassword : string
    ConfirmAdministratorPassword : string
    DataProvider : string
    DataProviderParameters : string
    AvailableDataProviders: string array
    DataProviderParametersDescription : IDictionary<string, seq<ParameterDescription>>
    
    MembershipProvider : string
    MembershipProviderParameters : string
    AvailableMembershipProviders: string array
    MembershipProviderParametersDescription : IDictionary<string, seq<ParameterDescription>>

    UseFileUpload : bool
    UseNuGetFeedUpload : bool
    NuGetFeeds : Uri[]
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<InheritedExport>]
type IDataProvider = 
    inherit IDisposable
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
    abstract member Id : string with get
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member ParameterDescriptions : seq<ParameterDescription> with get
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member Initialize : IDictionary<string, string> -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member GetEnvironments : seq<string> -> Environment[]
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member SaveEnvironments : seq<Environment> -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member DeleteEnvironment : string -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member GetAgents : seq<string> -> Agent[]
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member SaveAgents : seq<Agent> -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member DeleteAgent : string -> unit

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<InheritedExport>]
type IMembershipProvider = 
    inherit IDisposable
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member Id : string with get
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member ParameterDescriptions : seq<ParameterDescription> with get
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member Initialize : IDictionary<string, string> -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member Login : string * string * bool -> bool
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member Logout : unit -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member GetUser : string -> User option
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member GetUsers : unit -> User[]
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member CreateUser : string * string * string -> MembershipCreateStatus * User
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member DeleteUser : string -> bool
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member CreateRole : string -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member AddUserToRoles : string * seq<string> -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member RemoveUserFromRoles : string * seq<string> -> unit
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    abstract member GetAllRoles : unit -> string[]
