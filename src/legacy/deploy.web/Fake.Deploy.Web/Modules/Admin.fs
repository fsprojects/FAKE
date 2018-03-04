namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security

type Admin () as http =
    inherit FakeModule("/admin")

    do
        //Require admin role!
        http.RequiresAuthentication()

        http.get "/agent" (fun _ -> http.View.["agent"])

        http.get "/environment" (fun p -> http.View.["Environment"])

        http.get "/users" (fun _ -> http.View.["users"])
