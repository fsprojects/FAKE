namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Fake.Deploy.Web.Module.NancyOp
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
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

