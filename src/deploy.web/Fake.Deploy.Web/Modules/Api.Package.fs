namespace Fake.Deploy.Web.Module
open System
open Nancy
open Nancy.ModelBinding
open Nancy.Security
open Fake.Deploy.Web

type ApiPackage () as http =
    inherit FakeModule("/package")

    do
        http.get "/foo" (fun p -> "")
