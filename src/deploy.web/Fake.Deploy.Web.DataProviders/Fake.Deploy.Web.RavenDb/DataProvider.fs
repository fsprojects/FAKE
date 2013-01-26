namespace Fake.Deploy.Web.RavenDb

open System
open Fake.Deploy.Web
open Raven.Imports.Newtonsoft.Json
open Raven.Client
open Raven.Client.Document

module internal Provider = 
        
    let load<'a>(ids : string seq) (documentStore : IDocumentStore) =
        use session = documentStore.OpenSession()   
        session.Load<'a>(ids)
        |> Array.map box
        |> Array.choose (function
                       | null -> None
                       | a -> Some(a |> unbox<'a>))

    let save (instances : seq<_>) (documentStore : IDocumentStore)  =
        use session = documentStore.OpenSession()
        let count = ref 0
        for inst in instances do
            session.Store(inst)
            incr(count)
            if !count > 29 then session.SaveChanges()
        session.SaveChanges()

    let delete<'a> (ids : seq<string>) (documentStore : IDocumentStore) =
        use session = documentStore.OpenSession()
        let count = ref 0
        for id in ids do
            let x = session.Load<'a>(id)        
            session.Delete(x)
            if !count > 29 then session.SaveChanges()
        session.SaveChanges()

    let getEnvironment (id : seq<string>) = 
        load<Environment> id

    let saveEnvironment (env : Environment)  = 
        save [env]
       
    let deleteEnvironment (id : string) =
        delete<Environment> [id]

    let getEnvironments (documentStore : IDocumentStore) = 
        use session = documentStore.OpenSession()
        query {
            for env in session.Query<Environment>() do
            select env
        } 
        |> Seq.sortBy (fun e -> e.Id)
        |> Seq.toArray
    
    let saveAgent (environmentId : string) (agent : Agent) (documentStore : IDocumentStore) = 
        use session = documentStore.OpenSession()
        let env = session.Load<Environment>(environmentId)
        env.AddAgents([agent])
        session.Store(env)
        session.Store(agent)
        session.SaveChanges()

    let getAgents (documentStore : IDocumentStore) = 
        use session = documentStore.OpenSession()
        query {
            for env in session.Query<Agent>() do
            select env
        } 
        |> Seq.sortBy (fun e -> e.Id)
        |> Seq.toArray

    let deleteAgent (id : string) =
        delete<Agent> [id]

    let getAgent ids =
        load<Agent> ids

type RavenDbDataProvider(?inMemory : bool) = 

    let assertDocStore() =
        if RavenDbDataProvider.DocumentStore = null
        then invalidOp "RavenDbDataProvider not initialized, please call IDataProvider.Initialize"
    
    new() = 
        new RavenDbDataProvider(false)    
    
    static member val DocumentStore : IDocumentStore = null with get, set

    interface IDataProvider with
        member x.Initialize(connectionStringName) =
            RavenDbDataProvider.DocumentStore <- 
                 let ds = new DocumentStore(ConnectionStringName = connectionStringName)
                 ds.Conventions.IdentityPartsSeparator <- "-"
                 ds.Conventions.CustomizeJsonSerializer <- new Action<_>(fun s -> s.Converters.Add(new RavenUnionTypeConverter()))
                 ds.Initialize()
                
        member x.GetEnvironments(ids) =
            assertDocStore()
            match ids |> Seq.toList with
            | [] -> Provider.getEnvironments RavenDbDataProvider.DocumentStore
            | a -> Provider.getEnvironment ids RavenDbDataProvider.DocumentStore

        member x.SaveEnvironments(envs) = 
            assertDocStore()
            Provider.save envs RavenDbDataProvider.DocumentStore

        member x.DeleteEnvironment(id) = 
            assertDocStore()
            Provider.deleteEnvironment id RavenDbDataProvider.DocumentStore

        member x.GetAgents(ids) =
            assertDocStore()

            match ids |> Seq.toList with
            | [] -> Provider.getAgents RavenDbDataProvider.DocumentStore
            | a -> Provider.getAgent ids RavenDbDataProvider.DocumentStore

        member x.SaveAgents(agents) = 
            assertDocStore()
            Provider.save agents RavenDbDataProvider.DocumentStore

        member x.DeleteAgent(id) = 
            assertDocStore()
            Provider.deleteAgent id RavenDbDataProvider.DocumentStore

        member x.Dispose() = 
            if RavenDbDataProvider.DocumentStore <> null
            then RavenDbDataProvider.DocumentStore.Dispose()
            RavenDbDataProvider.DocumentStore <- null