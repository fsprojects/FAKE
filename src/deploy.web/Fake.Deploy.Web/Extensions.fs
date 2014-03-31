namespace Fake.Deploy.Web

open System
open System.Linq.Expressions
open System.Runtime.CompilerServices
open Nancy.Security
open Nancy.ViewEngines.Razor

module public Extensions =
    let IsInRole(u : IUserIdentity) role = 
        u.Claims |> Seq.exists(fun c -> c = role)
