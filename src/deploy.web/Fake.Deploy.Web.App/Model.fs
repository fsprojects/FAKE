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
    type Agent = {
        [<DataMember>]Id : string
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
    
    [<CLIMutable>]
    [<DataContract>]
    type Environment = {
            [<DataMember>]Id : string
            [<DataMember>]Name : string
            [<DataMember>]Description : string
            [<DataMember>]Agents : seq<Agent>
        }
        with
            static member Create(name : string, desc : string, agents : seq<_>) =
                   { Id = null; Name = name; Description = desc; Agents = agents }
            member x.AddAgents(agents : seq<Agent>) = 
                   { x with Agents = Seq.append agents x.Agents }

    let private documentStore = 
        let ds = new EmbeddableDocumentStore(ConnectionStringName = "RavenDB", UseEmbeddedHttpServer = true)
        ds.Conventions.IdentityPartsSeparator <- "-"
        ds.Configuration.Port <- 8082
        ds.Conventions.CustomizeJsonSerializer <- new Action<_>(fun s -> s.Converters.Add(new Helpers.RavenUnionTypeConverter()))
        ds.Initialize()
    
    let private createIndexes (assems : seq<Reflection.Assembly>) =
        assems |> Seq.iter (fun ass -> IndexCreation.CreateIndexes(ass, documentStore)) 

    let private createData() = 
        [
            { Id = "environments-1";
              Name = "Development";
              Description = "Development Environment";
              Agents = [
                         Agent.Create("http://localhost:8080"); Agent.Create("http://dev-2:8080");
                         Agent.Create("http://dev-1:8080"); Agent.Create("http://dev-2:8080");
                         Agent.Create("http://dev-1:8080"); Agent.Create("http://dev-2:8080");
                         Agent.Create("http://dev-1:8080"); Agent.Create("http://dev-2:8080");
                       ]}
            { Id = "environments-2";
              Name = "Integration";
              Description = "Integration Environment";
              Agents = [Agent.Create("http://int-1:8080"); Agent.Create("http://int-2:8080")] }
            { Id = "environments-3";
              Name = "Staging";
              Description = "User Acceptance and pre-Production environment";
              Agents = [Agent.Create("http://uat-1:8080"); Agent.Create("http://uat-2:8080")] }
            {
              Id = "environments-4"; 
              Name = "Production";
              Description = "User Acceptance and pre-Production environment";
              Agents = [Agent.Create("http://prod-1:8080"); Agent.Create("http://prod-2:8080")] }
        ]



    let Save (instances : seq<_>) =
        use session = documentStore.OpenSession()
        let count = ref 0
        for inst in instances do
            session.Store(inst)
            incr(count)
            if !count > 29 then session.SaveChanges()
        session.SaveChanges()

    let Init() = 
        createIndexes [Reflection.Assembly.GetExecutingAssembly()]
        createData() |> Save

    let getEnvironment (id : string) = 
        match id with
        | null -> { Id = id; Name = null; Description = null; Agents = Seq.empty }
        | _ ->
            use session = documentStore.OpenSession()
            session.Load<Environment>(id)
       
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

    let getAgents() = 
        getEnvironments()
        |> Seq.collect (fun env -> env.Agents)

    let getAgent (id : string) =
        getAgents()
        |> Seq.filter (fun a -> a.Id = id)
        |> Seq.head
     




