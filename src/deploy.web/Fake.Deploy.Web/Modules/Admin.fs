namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Fancy
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security

type Admin () as http =
    inherit FakeModule("/admin")

    do
        http.get "/agent/{id}" (fun _ id -> "")

        http.get "/environment" (fun _ -> "")

        http.get "/users" (fun _ -> "")
