namespace Test.Fake.Deploy.Web
open System
open System.Text
open Fake.Deploy.Web
open Fake.Deploy.Web.Data
open System.Collections.Generic

type InMemoryDataProvider () =
                
    member private x.getFrom<'T> (d:Dictionary<string, 'T>) ids f =
        match (ids |> List.ofSeq) with
        | [] -> d.Values |> Array.ofSeq
        | l -> d.Values |> Seq.filter(fun a -> ids |> Seq.exists(fun e -> e = f(a))) |> Array.ofSeq

    interface IDataProvider with
        member x.Id with get() = "InMem"

        member x.ParameterDescriptions with get() = Seq.empty
        member x.Initialize(settings) = ()
        member x.GetEnvironments(ids) = x.getFrom InMemoryProvider.environments ids (fun x -> x.Id)
        member x.SaveEnvironments(envs) = envs |> Seq.iter(fun e -> InMemoryProvider.environments.[e.Id] <- e)
        member x.DeleteEnvironment(id) = InMemoryProvider.environments.Remove id |> ignore
        member x.GetAgents(ids) = x.getFrom InMemoryProvider.agents ids (fun x -> x.Id)
        member x.SaveAgents(agents) = agents |> Seq.iter(fun a -> InMemoryProvider.agents.[a.Id] <- a)
        member x.DeleteAgent(id) = InMemoryProvider.agents.Remove id |> ignore
        member x.Dispose () = ()
