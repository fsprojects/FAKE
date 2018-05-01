namespace Fake.Deploy.Web
open Nancy
open Nancy.Security
open Nancy.Authentication.Forms
open System
open System.IO
open System.Collections.Generic
open Fake.Deploy.Web

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type AuthenticatedUser(userName, claims) =
    interface IUserIdentity with
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.UserName with get () = userName
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member this.Claims with get () = claims

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type UserMapper () =
    
    let users = Dictionary<Guid, User>()

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.RemoveUser (user : IUserIdentity) =
        if user <> null then 
            match users |> Seq.tryFind(fun x -> x.Value.Username = user.UserName) with
            | None -> ()
            | Some u -> users.Remove u.Key |> ignore

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.AddUser user =
        let id = Guid.NewGuid()
        users.Add (id, user)
        id

    interface IUserMapper with
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.GetUserFromIdentifier (identifier, context) =
            let file = Path.Combine(Data.appdata.FullName, "")
            if Data.isInitialized()
            then
                let exists, user = users.TryGetValue(identifier)
                match exists with
                | true -> AuthenticatedUser(user.Username, user.Roles) :> IUserIdentity
                | false -> null
            else
                null



