namespace Fake.Deploy.Web

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Globalization
open System.Runtime.Serialization

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<CLIMutable>]
[<DataContract>]
type Login = {
    [<Required;DataMember>]UserName : string
    [<Required;DataMember>]Password : string
    [<DataMember>]RememberMe : bool
}

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
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

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<CLIMutable>]
[<DataContract>]
type Role = {
    [<Required;DataMember>]
    Id : string
}

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<CLIMutable>]
[<DataContract>]
type User = {
    [<DataMember>]Id : string
    [<DataMember>]Username : string
    [<DataMember>]Email : string
    [<DataMember>]Roles : ResizeArray<string>
    [<DataMember>]Password : byte[]
}

