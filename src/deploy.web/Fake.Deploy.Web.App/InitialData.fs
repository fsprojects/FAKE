module Fake.Deploy.Web.InitialData

open System
open Fake.Deploy.Web.Model
open Raven.Client.Indexes

let private createData() = 
    [
        { Id = "environments-1";
          Name = "Development";
          Description = "Development Environment";
          Agents = []}
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
          Description = "User Acceptance and pre-Production environment";
          Agents = [] }
    ]

let private createIndexes (assems : seq<Reflection.Assembly>) =
    assems |> Seq.iter (fun ass -> IndexCreation.CreateIndexes(ass, documentStore))

let Init() = 
    createIndexes [Reflection.Assembly.GetExecutingAssembly()]
    createData() |> Save