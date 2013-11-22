namespace Fake.Deploy.Web.RavenDb

open System
open System.Text
open Fake.Deploy.Web
open System.Web.Security
open System.Web

type RavenDbMembershipProvider() =     
    
    let computeHash (password : string) = 
        System.Security.Cryptography.SHA512.Create().ComputeHash(Encoding.Default.GetBytes(password))

    let validatePassword (hash : byte[]) (password : string) =
        hash = (computeHash password)
    
    interface IMembershipProvider with
        member x.Id with get() = "RavenDB"

        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "url";
                              Description = "url to RavenDB. ex: http://localhost:8081" }
                         ] |> Seq.ofList

        member x.Initialize(settings) =
              Provider.init settings.["url"]

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

        member x.CreateRole(role) =
               let role : Role = { Id = role }
               Provider.save [role]

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

        member x.DeleteUser(username) =
            Provider.deleteUser username
            true

        member x.GetUsers() = Provider.getAllUsers()

        member x.GetAllRoles() = Provider.getRoles() |> Array.map (fun r -> r.Id)

        member x.GetUser(username) = Provider.tryGetUser username

        member x.Login(username, password, rememberme) =
               match Provider.tryGetUser username with
               | Some(user) -> 
                    if validatePassword user.Password password
                    then
                        FormsAuthentication.SetAuthCookie(username, rememberme); true
                    else false
               | None -> false

        member x.Logout() = FormsAuthentication.SignOut()

        member x.RemoveUserFromRoles(username, roles) =
               match Provider.tryGetUser username with
               | Some(user) ->
                    for role in roles do
                        user.Roles.Remove(role) |> ignore
                    Provider.save [user]
               | None -> failwithf "Could not find user %s" username


        member x.Dispose() = 
            Provider.dispose()
            



