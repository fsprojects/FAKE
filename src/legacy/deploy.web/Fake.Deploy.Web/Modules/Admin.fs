namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type Admin () as http =
    inherit FakeModule("/admin")

    do
        //Require admin role!
        http.RequiresAuthentication()

        http.get "/agent" (fun _ -> http.View.["agent"])

        http.get "/environment" (fun p -> http.View.["Environment"])

        http.get "/users" (fun _ -> http.View.["users"])
