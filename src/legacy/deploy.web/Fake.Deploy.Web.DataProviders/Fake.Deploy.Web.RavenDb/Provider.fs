namespace Fake.Deploy.Web.RavenDb

open System
open Fake.Deploy.Web
open Raven.Imports.Newtonsoft.Json
open Raven.Client
open Raven.Client.Document

module internal Provider = 
    
    let mutable store : IDocumentStore = null 

    let assertDocStore() =
        if store = null
        then invalidOp "RavenDbDataProvider not initialized"

    let init(url) =
         let ds = new DocumentStore(Url = url)
         ds.Conventions.IdentityPartsSeparator <- "-"
         ds.Conventions.CustomizeJsonSerializer <- new Action<_>(fun s -> s.Converters.Add(new RavenUnionTypeConverter()))
         store <- ds.Initialize()

    let dispose() =
        if store <> null && not(store.WasDisposed)
        then store.Dispose()
        store <- null
       
    let load<'a>(ids : string seq) =
        assertDocStore()
        use session = store.OpenSession()   
        session.Load<'a>(ids)
        |> Array.map box
        |> Array.choose (function
                       | null -> None
                       | a -> Some(a |> unbox<'a>))

    let save (instances : seq<_>) =
        assertDocStore()
        use session = store.OpenSession()
        let count = ref 0
        for inst in instances do
            session.Store(inst)
            incr(count)
            if !count > 29 then session.SaveChanges()
        session.SaveChanges()

    let delete<'a> (ids : seq<string>) =
        assertDocStore()
        use session = store.OpenSession()
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

    let getEnvironments() = 
        assertDocStore()
        use session = store.OpenSession()
        query {
            for env in session.Query<Environment>() do
            select env
        } 
        |> Seq.sortBy (fun e -> e.Id)
        |> Seq.toArray
    
    let saveAgent (environmentId : string) (agent : Agent) = 
        assertDocStore()
        use session = store.OpenSession()
        let env = session.Load<Environment>(environmentId)
        let env = env.AddAgents([agent])
        session.Store(env)
        session.Store(agent)
        session.SaveChanges()

    let getAgents() = 
        assertDocStore()
        use session = store.OpenSession()
        query {
            for env in session.Query<Agent>() do
            select env
        } 
        |> Seq.sortBy (fun e -> e.Id)
        |> Seq.toArray

    let deleteAgent (id : string) =
        use session = store.OpenSession()
        let agent = session.Load<Agent>(id)
        let env = session.Load<Environment>(agent.EnvironmentId)
        let env = env.RemoveAgents [agent]
        session.Store(env)
        session.Delete(agent)
        session.SaveChanges()

    let getAgent ids =
        load<Agent> ids

    let getAllUsers() = 
        assertDocStore()
        use session = store.OpenSession()
        query {
            for env in session.Query<User>() do
            select env
        } 
        |> Seq.sortBy (fun e -> e.Id)
        |> Seq.toArray

    let deleteUser (id : string) =
        delete<User> [id]

    let getUsers ids =
        load<User> ids 

    let tryGetUser id = 
        match getUsers [id] |> Array.map (box) with
        | [|h|] when h <> null -> Some(unbox<User> h)
        | _ -> None

    let getRoles() =
        assertDocStore() 
        use session = store.OpenSession()
        query {
            for role in session.Query<Role>() do
            select role
        } |> Seq.toArray
        

