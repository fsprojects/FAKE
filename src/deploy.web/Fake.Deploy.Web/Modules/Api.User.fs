namespace Fake.Deploy.Web.Module
open System
open Nancy
open Nancy.ModelBinding
open Nancy.Security
open Fake.Deploy.Web

//User = Account ?
type ApiUser () as http =
    inherit FakeModule("/user")
    
    do
        http.get "/foo" (fun p -> "")
