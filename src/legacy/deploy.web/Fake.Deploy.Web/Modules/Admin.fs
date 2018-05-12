namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Admin () as http =
    inherit FakeModule("/admin")

    do
        //Require admin role!
        http.RequiresAuthentication()

        http.get "/agent" (fun _ -> http.View.["agent"])

        http.get "/environment" (fun p -> http.View.["Environment"])

        http.get "/users" (fun _ -> http.View.["users"])
