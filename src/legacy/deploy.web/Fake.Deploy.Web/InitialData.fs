[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.Deploy.Web.InitialData

open System
open System.Web.Security
open System.Linq
open Fake.Deploy.Web

let private createRole (name : string) (provider : IMembershipProvider) = 
        match provider.GetAllRoles() |> Seq.tryFind (fun r -> r.ToLower() = name.ToLower()) with
        | Some(role) -> ()
        | None -> provider.CreateRole(name)

let private createUser (name : string) password email roles (provider : IMembershipProvider) = 
        match provider.GetUser(name) with
        | Some(a) ->  
            provider.AddUserToRoles(a.Username, roles)
        | _ -> 
            match provider.CreateUser(name, password, email) with
            | MembershipCreateStatus.Success, a -> provider.AddUserToRoles(a.Username, roles)
            | _,s -> failwithf "Could not create user %s" (s.ToString())

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Init(adminUsername, adminPassword, adminEmail, dataProvider : IDataProvider, memberProvider : IMembershipProvider) =
    createRole "Administrator" memberProvider
    createUser adminUsername adminPassword adminEmail [|"Administrator"|] memberProvider
    
    let agent1 = Agent.Create "http://localhost:8080" "environments-1" "localhost"
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
