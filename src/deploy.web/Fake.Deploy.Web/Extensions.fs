namespace Fake.Deploy.Web

open System
open System.Linq.Expressions
open System.Runtime.CompilerServices
open Nancy.Security
open Nancy.ViewEngines.Razor

[<ExtensionAttribute>]
module public Extensions =
        [<ExtensionAttribute>]
        let IsAuthenticated2(u : IUserIdentity) role = 
            false
        
        [<ExtensionAttribute>]
        let IsInRole(u : IUserIdentity) role = 
            u.Claims |> Seq.exists(fun c -> c = role)
