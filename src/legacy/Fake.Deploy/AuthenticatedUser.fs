namespace Fake.Deploy

open Nancy.Security

type AuthenticatedUser(userName, claims) = 
    interface IUserIdentity with
        member this.UserName with get () = userName
        member this.Claims with get () = claims