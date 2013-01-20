namespace Fake.Deploy.Web

open System
open System.Runtime.Serialization

[<CLIMutable>]
[<DataContract>]
type AgentRef = {
    [<DataMember>]Id : string
    [<DataMember>]Name : string
}

[<CLIMutable>]
[<DataContract>]
type Agent = {
    [<DataMember>] mutable Id : string
    [<DataMember>]Name : string
    [<DataMember>]Address : Uri
    }
    with
        static member Create(url : string, ?name : string) =
            let url = Uri(url)
            {
                Id = url.Host + "-" + (url.Port.ToString())
                Name = defaultArg name url.Host
                Address = url
            }
        member x.Ref with get() : AgentRef = { Id = x.Id; Name = x.Name }


[<CLIMutable>]
[<DataContract>]
type Environment = {
        [<DataMember>]mutable Id : string
        [<DataMember>]Name : string
        [<DataMember>]Description : string
        [<DataMember>]mutable Agents : seq<AgentRef>
    }
    with
        static member Create(name : string, desc : string, agents : seq<_>) =
               { Id = null; Name = name; Description = desc; Agents = agents }
        member x.AddAgents(agents : seq<Agent>) = 
                x.Agents <- Seq.append (agents |> Seq.map (fun a -> a.Ref)) x.Agents

type IDataProvider = 
    inherit IDisposable
    abstract member Initialize : string -> unit
    abstract member GetEnvironments : seq<string> -> Environment[]
    abstract member SaveEnvironments : seq<Environment> -> unit
    abstract member DeleteEnvironment : string -> unit
    abstract member GetAgents : seq<string> -> Agent[]
    abstract member SaveAgents : seq<Agent> -> unit
    abstract member DeleteAgent : string -> unit