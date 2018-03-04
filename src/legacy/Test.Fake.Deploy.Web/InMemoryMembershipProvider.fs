namespace Test.Fake.Deploy.Web
open System
open System.Text
open Fake.Deploy.Web
open Fake.Deploy.Web.Data
open System.Web.Security
open System.Collections.Generic

type InMemoryMembershipProvider () =
    
    interface IMembershipProvider with
        member x.Id with get() = "InMem"

        member x.ParameterDescriptions 
            with get() = Seq.empty

        member x.Initialize(settings) = ()
        member x.AddUserToRoles(username, roles) = ()

        member x.CreateRole(role) =
               let role : Role = { Id = role }
               InMemoryProvider.roles.[role.Id] <- role

        member x.CreateUser(username, password, email) =
            let user =  { Id = username; Username = username; Password = Encoding.Unicode.GetBytes(password); Email = email; Roles = new ResizeArray<_>([]) }
            InMemoryProvider.users.[user.Username] <- user
            MembershipCreateStatus.Success, user

        member x.DeleteUser(username) = 
            match InMemoryProvider.users.TryGetValue username with
            | false, _ -> false
            | true, _ ->
                InMemoryProvider.users.Remove username |> ignore
                true

        member x.GetUsers() = InMemoryProvider.users.Values |> Array.ofSeq

        member x.GetAllRoles() = InMemoryProvider.roles.Values |> Seq.map (fun r -> r.Id) |> Array.ofSeq

        member x.GetUser(username) = InMemoryProvider.tryGetUser username

        member x.Login(username, password, rememberme) = true

        member x.Logout() = ()

        member x.RemoveUserFromRoles(username, roles) = ()

        member x.Dispose() = ()


