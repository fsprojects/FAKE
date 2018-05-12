namespace Fake.Deploy.Web

open System
open System.Linq.Expressions
open System.Runtime.CompilerServices
open Nancy.Security
open Nancy.ViewEngines.Razor

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module public Extensions =
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let IsInRole(u : IUserIdentity) role = 
        u.Claims |> Seq.exists(fun c -> c = role)
