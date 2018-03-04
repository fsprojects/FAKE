namespace Fake.Deploy.Web
open Nancy
open Nancy.Security
open Nancy.Authentication.Forms
open System
open System.IO
open System.Collections.Generic
open Fake.Deploy.Web

type AuthenticatedUser(userName, claims) =
    interface IUserIdentity with
        member this.UserName with get () = userName
        member this.Claims with get () = claims

type UserMapper () =
    
    let users = Dictionary<Guid, User>()

    member this.RemoveUser (user : IUserIdentity) =
        if user <> null then 
            match users |> Seq.tryFind(fun x -> x.Value.Username = user.UserName) with
            | None -> ()
            | Some u -> users.Remove u.Key |> ignore

    member this.AddUser user =
        let id = Guid.NewGuid()
        users.Add (id, user)
        id

    interface IUserMapper with
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



