namespace Fake.Deploy.Web.RavenDb

open System
open System.Text
open Fake.Deploy.Web
open System.Web.Security
open System.Web

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type RavenDbMembershipProvider() =     

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]    
    let computeHash (password : string) = 
        System.Security.Cryptography.SHA512.Create().ComputeHash(Encoding.Default.GetBytes(password))

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let validatePassword (hash : byte[]) (password : string) =
        hash = (computeHash password)
    
    interface IMembershipProvider with
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Id with get() = "RavenDB"

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "url";
                              Description = "url to RavenDB. ex: http://localhost:8081" }
                         ] |> Seq.ofList

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Initialize(settings) =
              Provider.init settings.["url"]

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.AddUserToRoles(username, roles) =
            match Provider.tryGetUser username with
            | Some(user) ->
                for role in roles do
                    try
                        let role = Provider.load<Role> [role] |> Seq.head
                        user.Roles.Add(role.Id)
                    with
                        | :? ArgumentException as e -> failwithf "Could not add user to role %s it does not exist" role
                        | a -> reraise()
                Provider.save [user]
            | None -> failwithf "Could not find user %s" username

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.CreateRole(role) =
               let role : Role = { Id = role }
               Provider.save [role]

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.CreateUser(username, password, email) =
            match Provider.tryGetUser username with
            | None ->
                let user = 
                    {
                        Id = username
                        Username = username
                        Password = (computeHash password)
                        Email = email
                        Roles = new ResizeArray<_>([])
                    }
                
                Provider.save [user]
                MembershipCreateStatus.Success, user
            | Some(u) -> failwithf "User already exists"

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.DeleteUser(username) =
            Provider.deleteUser username
            true

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.GetUsers() = Provider.getAllUsers()

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.GetAllRoles() = Provider.getRoles() |> Array.map (fun r -> r.Id)

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.GetUser(username) = Provider.tryGetUser username

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Login(username, password, rememberme) =
               match Provider.tryGetUser username with
               | Some(user) -> 
                    if validatePassword user.Password password
                    then
                        FormsAuthentication.SetAuthCookie(username, rememberme); true
                    else false
               | None -> false

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Logout() = FormsAuthentication.SignOut()

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.RemoveUserFromRoles(username, roles) =
               match Provider.tryGetUser username with
               | Some(user) ->
                    for role in roles do
                        user.Roles.Remove(role) |> ignore
                    Provider.save [user]
               | None -> failwithf "Could not find user %s" username

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Dispose() = 
            Provider.dispose()
            



