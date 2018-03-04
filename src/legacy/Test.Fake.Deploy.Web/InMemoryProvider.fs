namespace Test.Fake.Deploy.Web
open System
open System.Text
open Fake.Deploy.Web
open Fake.Deploy.Web.Data
open System.Web.Security
open System.Collections.Generic

module InMemoryProvider =
    let users = Dictionary<string, User>()
    let roles = Dictionary<string, Role>()
    let environments = Dictionary<string, Environment>()
    let agents = Dictionary<string, Agent>()

    let tryGetUser u = 
        match users.TryGetValue(u) with
        | false, _ -> None
        | true, u -> Some u
