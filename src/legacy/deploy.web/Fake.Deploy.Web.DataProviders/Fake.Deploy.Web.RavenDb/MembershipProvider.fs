namespace Fake.Deploy.Web.RavenDb

open System
open System.Text
open Fake.Deploy.Web
open System.Web.Security
open System.Web

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type RavenDbMembershipProvider() =     

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
    let computeHash (password : string) = 
        System.Security.Cryptography.SHA512.Create().ComputeHash(Encoding.Default.GetBytes(password))

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let validatePassword (hash : byte[]) (password : string) =
        hash = (computeHash password)
    
    interface IMembershipProvider with
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Id with get() = "RavenDB"

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "url";
                              Description = "url to RavenDB. ex: http://localhost:8081" }
                         ] |> Seq.ofList

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Initialize(settings) =
              Provider.init settings.["url"]

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.CreateRole(role) =
               let role : Role = { Id = role }
               Provider.save [role]

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.DeleteUser(username) =
            Provider.deleteUser username
            true

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.GetUsers() = Provider.getAllUsers()

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.GetAllRoles() = Provider.getRoles() |> Array.map (fun r -> r.Id)

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.GetUser(username) = Provider.tryGetUser username

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Login(username, password, rememberme) =
               match Provider.tryGetUser username with
               | Some(user) -> 
                    if validatePassword user.Password password
                    then
                        FormsAuthentication.SetAuthCookie(username, rememberme); true
                    else false
               | None -> false

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Logout() = FormsAuthentication.SignOut()

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.RemoveUserFromRoles(username, roles) =
               match Provider.tryGetUser username with
               | Some(user) ->
                    for role in roles do
                        user.Roles.Remove(role) |> ignore
                    Provider.save [user]
               | None -> failwithf "Could not find user %s" username

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Dispose() = 
            Provider.dispose()
            



