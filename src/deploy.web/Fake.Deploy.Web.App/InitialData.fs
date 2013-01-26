module Fake.Deploy.Web.InitialData

open System
open Fake.Deploy.Web.Model
open Raven.Client.Indexes
open RavenDBMembership.Provider
open RavenDBMembership.MVC.Models
open System.Linq

let private createIndexes (assems : seq<Reflection.Assembly>) =
    assems |> Seq.iter (fun ass -> IndexCreation.CreateIndexes(ass, documentStore))

let private createRole (name : string) (provider : IMembershipService) = 
        match provider.GetAllRoles() |> Seq.tryFind (fun r -> r.ToLower() = name.ToLower()) with
        | Some(role) -> ()
        | None -> provider.AddRole(name)
        provider

let private createUser (name : string) password email roles (provider : IMembershipService) = 
        match provider.GetUser(name) with
        | a when a <> null ->  
            provider.UpdateUser(a, roles)
        | _ -> 
            if provider.CreateUser(name, password, email) = Web.Security.MembershipCreateStatus.Success
            then
                Threading.Thread.Sleep(1000) //This is crap but it deals with the stale query from raven, needs sorting properly
                let user = provider.GetUser(name)
                provider.UpdateUser(user, roles)
        provider

let Init() = 
    RavenDBMembershipProvider.DocumentStore <- documentStore
    RavenDBRoleProvider.DocumentStore <- documentStore
    createIndexes [Reflection.Assembly.GetExecutingAssembly()]

    let provider = new AccountMembershipService()

    provider
    |> createRole "Administrator"
    |> createUser "Admin" "admin" "fake.deploy@gmail.com" [|"Administrator"|]
    |> ignore

    let agent1 = Agent.Create("http://localhost:8080","localhost")
    let agents = [agent1]

    let environments = 
        [ { Id = "environments-1";
            Name = "Development";
            Description = "Development Environment";
            Agents = [agent1.Ref]}
          { Id = "environments-2";
            Name = "Integration";
            Description = "Integration Environment";
            Agents = [] }
          { Id = "environments-3";
            Name = "Staging";
            Description = "User Acceptance and pre-Production environment";
            Agents = [] }
          { Id = "environments-4"; 
            Name = "Production";
            Description = "Production environment";
            Agents = [] } ]

    Save agents
    Save environments