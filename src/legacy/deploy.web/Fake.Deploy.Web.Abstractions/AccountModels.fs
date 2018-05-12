namespace Fake.Deploy.Web

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Globalization
open System.Runtime.Serialization

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
[<DataContract>]
type Login = {
    [<Required;DataMember>]UserName : string
    [<Required;DataMember>]Password : string
    [<DataMember>]RememberMe : bool
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
[<DataContract>]
type Registration = {
    [<Required;DataMember>]UserName : string
    [<Required;DataMember>]Email : string

    [<Required;DataMember;StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
    Password : string

    [<Required;DataMember>]
    ConfirmPassword : string
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
[<DataContract>]
type Role = {
    [<Required;DataMember>]
    Id : string
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
[<DataContract>]
type User = {
    [<DataMember>]Id : string
    [<DataMember>]Username : string
    [<DataMember>]Email : string
    [<DataMember>]Roles : ResizeArray<string>
    [<DataMember>]Password : byte[]
}

