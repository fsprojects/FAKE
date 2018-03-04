namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Fake.Deploy.Web.Module.NancyOp
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security

type HomeModule() as http =
    inherit FakeModule("/")

    do
        http.RequiresAuthentication()

        http.get "/" (fun p ->
            http.View.["Index"]
        )

        http.get "/agent/{id}" (fun p ->
            let id = p ?> "id"
            http.View.["Agent"].WithModel id
        )

