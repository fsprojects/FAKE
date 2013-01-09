module Fake.Deploy.Web.InitialData

open System
open Fake.Deploy.Web.Model
open Raven.Client.Indexes


let private createIndexes (assems : seq<Reflection.Assembly>) =
    assems |> Seq.iter (fun ass -> IndexCreation.CreateIndexes(ass, documentStore))

let Init() = 
    createIndexes [Reflection.Assembly.GetExecutingAssembly()]

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