namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Fancy
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security

type Admin (data : IDataProvider, users : IMembershipProvider) as http =
    inherit FakeModule("/admin")

    do
        http.get "/agent/{id}" (fun _ id -> data.GetEnvironments([id]) |> Seq.head)

        http.get "/environment" (fun p -> http.View.["Environment"])

        http.get "/users" (fun _ ->
            http.View.["users"].WithModel (users.GetUsers())
        )
