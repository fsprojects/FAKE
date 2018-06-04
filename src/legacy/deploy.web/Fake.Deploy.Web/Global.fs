namespace Fake.Deploy.Web

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Global =

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let Version = 
        typeof<Fake.Deploy.Web.AuthenticatedUser>.Assembly.GetName().Version.ToString()

