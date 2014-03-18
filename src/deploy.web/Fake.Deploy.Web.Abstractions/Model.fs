namespace Fake.Deploy.Web

open System
open System.Collections.Generic
open System.Runtime.Serialization
open System.ComponentModel.Composition
open System.Web.Security

[<CLIMutable>]
[<DataContract>]
type AgentRef = {
    [<DataMember>]Id : string
    [<DataMember>]Name : string
}

[<CLIMutable>]
[<DataContract>]
type Agent = {
    [<DataMember>]Id : string
    [<DataMember>]Name : string
    [<DataMember>]Address : Uri
    [<DataMember>]EnvironmentId : string
    }
    with
        static member Create url environmentId name =
            let url = Uri(url)
            {
                Id = url.Host + "-" + (url.Port.ToString())
                Name = if String.IsNullOrEmpty name then url.Host else name
                Address = url
                EnvironmentId = environmentId
            }
        member x.Ref with get() : AgentRef = { Id = x.Id; Name = x.Name }


[<CLIMutable>]
[<DataContract>]
type Environment = {
        [<DataMember>]Id : string
        [<DataMember>]Name : string
        [<DataMember>]Description : string
        [<DataMember>]Agents : seq<AgentRef>
    }
    with
        static member CreateWithId id  name desc agents =
            { Id = id; Name = name; Description = desc; Agents = agents }

        static member Create name desc agents  =
            { Id = Guid.NewGuid().ToString(); Name = name; Description = desc; Agents = agents }

        member x.AddAgents(agents : seq<Agent>) = 
            { x with Agents = Seq.append (agents |> Seq.map (fun a -> a.Ref)) x.Agents |> Seq.distinct }

        member x.RemoveAgents(agents : seq<Agent>) =
            { x with 
                Agents = Seq.filter (fun a -> 
                                          agents 
                                          |> Seq.map (fun a -> a.Ref) 
                                          |> Seq.exists (fun b -> a = b)
                                          |> not
                                      ) x.Agents
            }

[<CLIMutable>]
type ParameterDescription = { 
    ParameterName : string
    Description : string
}

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

[<InheritedExport>]
type IDataProvider = 
    inherit IDisposable
    abstract member Id : string with get
    abstract member ParameterDescriptions : seq<ParameterDescription> with get
    abstract member Initialize : IDictionary<string, string> -> unit
    abstract member GetEnvironments : seq<string> -> Environment[]
    abstract member SaveEnvironments : seq<Environment> -> unit
    abstract member DeleteEnvironment : string -> unit
    abstract member GetAgents : seq<string> -> Agent[]
    abstract member SaveAgents : seq<Agent> -> unit
    abstract member DeleteAgent : string -> unit

[<InheritedExport>]
type IMembershipProvider = 
    inherit IDisposable
    abstract member Id : string with get
    abstract member ParameterDescriptions : seq<ParameterDescription> with get
    abstract member Initialize : IDictionary<string, string> -> unit
    abstract member Login : string * string * bool -> bool
    abstract member Logout : unit -> unit
    abstract member GetUser : string -> User option
    abstract member GetUsers : unit -> User[]
    abstract member CreateUser : string * string * string -> MembershipCreateStatus * User
    abstract member DeleteUser : string -> bool
    abstract member CreateRole : string -> unit
    abstract member AddUserToRoles : string * seq<string> -> unit
    abstract member RemoveUserFromRoles : string * seq<string> -> unit
    abstract member GetAllRoles : unit -> string[]