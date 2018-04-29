namespace Fake.Deploy.Web

open System
open System.Linq.Expressions
open System.Runtime.CompilerServices
open Nancy.Security
open Nancy.ViewEngines.Razor

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module public Extensions =
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let IsInRole(u : IUserIdentity) role = 
        u.Claims |> Seq.exists(fun c -> c = role)
