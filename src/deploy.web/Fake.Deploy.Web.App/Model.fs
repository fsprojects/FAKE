namespace Fake.Deploy.Web

module Model = 

    open System
    open System.IO
    open System.Web
    open System.Runtime.Serialization
    open Raven.Imports.Newtonsoft.Json
    open Raven.Client.Embedded
    open Raven.Client.Indexes
    open Microsoft.FSharp.Reflection
    open System.ComponentModel.DataAnnotations
    open System.ComponentModel.Composition

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

    let private documentStore = 
        let ds = new EmbeddableDocumentStore(ConnectionStringName = "RavenDB", UseEmbeddedHttpServer = true)
        ds.Conventions.IdentityPartsSeparator <- "-"
        ds.Configuration.Port <- 8082
        ds.Conventions.CustomizeJsonSerializer <- new Action<_>(fun s -> s.Converters.Add(new Helpers.RavenUnionTypeConverter()))
        ds.Initialize()
    
    let private createIndexes (assems : seq<Reflection.Assembly>) =
        assems |> Seq.iter (fun ass -> IndexCreation.CreateIndexes(ass, documentStore)) 

//    let private createData() = 
//        [
//            { Id = "environments-1";
//              Name = "Development";
//              Description = "Development Environment";
//              Agents = []}
//            { Id = "environments-2";
//              Name = "Integration";
//              Description = "Integration Environment";
//              Agents = [] }
//            { Id = "environments-3";
//              Name = "Staging";
//              Description = "User Acceptance and pre-Production environment";
//              Agents = [] }
//            {
//              Id = "environments-4"; 
//              Name = "Production";
//              Description = "User Acceptance and pre-Production environment";
//              Agents = [] }
//        ]
//
//    let Save (instances : seq<_>) =
//        use session = documentStore.OpenSession()
//        let count = ref 0
//        for inst in instances do
//            session.Store(inst)
//            incr(count)
//            if !count > 29 then session.SaveChanges()
//        session.SaveChanges()

    let Init() = 
        createIndexes [Reflection.Assembly.GetExecutingAssembly()]
//        createData() |> Save

    let getEnvironment (id : string) = 
        match id with
        | null -> { Id = id; Name = null; Description = null; Agents = Seq.empty }
        | _ ->
            use session = documentStore.OpenSession()
            session.Load<Environment>(id)

    let saveEnvironment (env : Environment) = 
        use session = documentStore.OpenSession()
        session.Store(env)
        session.SaveChanges()
       
    let deleteEnvironment (id : string) =
        use session = documentStore.OpenSession()
        let x = session.Load<Environment>(id)        
        session.Delete(x)
        session.SaveChanges()

    let getEnvironments() = 
        use session = documentStore.OpenSession()
        query {
            for env in session.Query<Environment>() do
            select env
        } 
        |> Seq.sortBy (fun e -> e.Id)
        |> Seq.toArray
    
    let saveAgent (environmentId : string) (agent : Agent) = 
        use session = documentStore.OpenSession()
        let env = session.Load<Environment>(environmentId)
        env.AddAgents([agent])
        session.Store(env)
        session.Store(agent)
        session.SaveChanges()

    let getAgents() = 
        use session = documentStore.OpenSession()
        query {
            for env in session.Query<Agent>() do
            select env
        } 
        |> Seq.sortBy (fun e -> e.Id)
        |> Seq.toArray

    let deleteAgent (id : string) =
        use session = documentStore.OpenSession()
        let x = session.Load<Agent>(id)        
        session.Delete(x)
        session.SaveChanges()

    let getAgent (id : string) =
        match id with
        | null -> { Id = id; Name = null; Address = null }
        | _ ->
            use session = documentStore.OpenSession()
            session.Load<Agent>(id)
     




