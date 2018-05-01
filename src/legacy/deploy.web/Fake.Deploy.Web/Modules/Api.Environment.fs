namespace Fake.Deploy.Web.Module
open System
open Nancy
open Nancy.ModelBinding
open Nancy.Security
open log4net

open Fake.Deploy.Web
open Fake.Deploy.Web.Module.NancyOp

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type ApiEnvironment (dataProvider : IDataProvider) as http =
    inherit FakeModule("/api/v1/environment")

    do

    let logger = LogManager.GetLogger(http.GetType().Name)

    do
        http.get "/" (fun p ->
            http.returnAsJson
                (fun () -> dataProvider.GetEnvironments([]))
                (fun e -> logger.Error("An error occured retrieving environments" ,e))
        )

        http.get "/{id}" (fun p ->
            let id = (p ?> "id")
            try
                match dataProvider.GetEnvironments [id] |> Seq.tryFind(fun i -> true) with
                    | None -> http.Response.AsText("Not found").WithStatusCode HttpStatusCode.NotFound |> box
                    | Some e -> http.Response.AsJson e |> box
            with e ->
                logger.Error(sprintf "An error occured retrieving environment %s" id, e)
                http.InternalServerError e |> box
        )

        http.post "/" (fun p ->
            try
                let tmp = http.Bind<Environment>()
                let env = Environment.Create tmp.Name tmp.Description tmp.Agents
                dataProvider.SaveEnvironments [env]
                http.Response
                    .AsJson(env)
                    .WithStatusCode(HttpStatusCode.Created)
            with e ->
                logger.Error("An error occured saving environment" ,e)
                http.InternalServerError e
        )

        http.delete "/{id}" (fun p ->
            let id = p ?> "id"
            try
                dataProvider.DeleteEnvironment id
                HttpStatusCode.NoContent |> box
            with e ->
                logger.Error(sprintf "An error occured deleting environment %s" id,e)
                http.InternalServerError e |> box
        )
