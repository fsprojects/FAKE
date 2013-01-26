module Fake.Deploy.Web.InitialData

open System
open System.Web.Security
open System.Linq
open Fake.Deploy.Web

let private createRole (name : string) = 
        match Roles.GetAllRoles() |> Seq.tryFind (fun r -> r.ToLower() = name.ToLower()) with
        | Some(role) -> ()
        | None -> Roles.CreateRole(name)

let private createUser (name : string) password email roles = 
        match Membership.GetUser(name) with
        | a when a <> null ->  
            Roles.AddUserToRoles(a.UserName, roles)
        | _ -> 
            match Membership.CreateUser(name, password, email, null, null, true) with
            | a, MembershipCreateStatus.Success -> Roles.AddUserToRoles(a.UserName, roles)
            | _,s -> failwithf "Could not create user %s" (s.ToString())

let Init(adminUsername, adminPassword, adminEmail, dataProvider : IDataProvider) =
    
    createRole "Administrator"
    createUser adminUsername adminPassword adminEmail [|"Administrator"|]
    
    let agent1 = Agent.Create("http://localhost:8081","localhost")
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

    dataProvider.SaveAgents agents
    dataProvider.SaveEnvironments environments