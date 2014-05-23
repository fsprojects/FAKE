namespace Fake.Deploy

open System
open System.Collections.Concurrent
open Nancy.Security

type UserMap = 
    { User : IUserIdentity
      ValidTo : DateTime }

type UserMapper(createGuid : unit -> Guid) = 
    let users = ConcurrentDictionary<Guid, UserMap>()
    let expires() = DateTime.Now.AddMinutes(10.)
    
    let removeStale() = 
        let stale = users |> Seq.filter (fun r -> r.Value.ValidTo <= DateTime.Now)
        stale |> Seq.iter (fun s -> users.TryRemove(s.Key) |> ignore)
    
    member this.GetUser token = 
        removeStale()
        match users.TryGetValue token with
        | false, _ -> None
        | _, u ->
            users.TryUpdate(token, { u with ValidTo = expires() }, u) |> ignore
            Some u.User
    
    member this.RemoveByToken token = users.TryRemove(token) |> ignore
    
    member this.RemoveByUser(user : IUserIdentity) = 
        if user <> null then 
            match users |> Seq.tryFind (fun x -> x.Value.User.UserName = user.UserName) with
            | None -> ()
            | Some u -> this.RemoveByToken u.Key
    
    member this.AddUser user = 
        let id = createGuid()
        users.TryAdd(id, 
                     { User = user
                       ValidTo = expires() })
        |> ignore
        id
