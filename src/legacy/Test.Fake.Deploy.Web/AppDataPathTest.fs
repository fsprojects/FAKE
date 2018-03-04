module Test.Fake.Deploy.Web.Data
open System
open System.IO
open Nancy
open Nancy.Testing
open Newtonsoft.Json
open Xunit
open Fake.Deploy.Web
open Fake.Deploy.Web.Data

type Dummy = { x: int }

[<Fact>]
let ``should get path to appdata`` () =
    let path = Path.Combine(Path.GetDirectoryName(Uri(typedefof<Dummy>.Assembly.CodeBase).AbsolutePath), "App_Data")
    Assert.Equal<string>(path, Fake.Deploy.Web.Data.appdata.FullName)
