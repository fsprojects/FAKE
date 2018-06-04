namespace Fake.Deploy

open Nancy.Security

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type AuthenticatedUser(userName, claims) = 
    interface IUserIdentity with
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member this.UserName with get () = userName
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]        
        member this.Claims with get () = claims
