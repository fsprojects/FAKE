namespace Fake.Deploy.Web.File
open Fake.Deploy.Web
open System
open System.Text
open System.IO
open System.Runtime.Serialization.Json
open System.Net.Security
open System.Security.Cryptography
open System.Web.Security
open System.Web

type FileMembershipProvider () =

    let saltSize = 16
    let hashSize = 32
    let salt () = 
        let buff = Array.zeroCreate(saltSize)
        (new RNGCryptoServiceProvider()).GetBytes(buff)
        buff
         
    let computeHash (salt:byte[]) (password:string) = 
        let pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000)
        let hash = pbkdf2.GetBytes(hashSize)
        let saltAndHash = Array.zeroCreate(saltSize + hashSize)
        Array.Copy(salt, 0, saltAndHash, 0, saltSize)
        Array.Copy(hash, 0, saltAndHash, saltSize, hashSize)
        saltAndHash

    let validatePassword (hash : byte[]) (password : string) =
        let salt = Array.zeroCreate(saltSize)
        Array.Copy(hash, 0, salt, 0, saltSize)
        hash = (computeHash salt password)

    interface IMembershipProvider with
        member x.Id with get() = "File"

        member x.ParameterDescriptions 
            with get() =
                [ { ParameterName = "datafolder"; 
                    Description = "Path to where you want data to be stored. Ex: C:\\Data" }
                ] |> Seq.ofList

        member x.Initialize(settings) =
              Provider.init settings.["datafolder"]

        member x.AddUserToRoles(username, roles) =
            let allRoles = Provider.getRoles()
            match Provider.tryGetUser username with
            | Some(user) ->
                let roles' = 
                    roles 
                    |> Seq.map(fun r -> allRoles |> Seq.tryFind(fun x -> x.Id.Equals(r, StringComparison.CurrentCultureIgnoreCase) ) )
                    |> Seq.filter(fun r -> r.IsSome)
                    |> Seq.map (fun r -> r.Value.Id)
                user.Roles.AddRange(roles')
                Provider.saveUsers [user]
            | None -> failwithf "Could not find user %s" username

        member x.CreateRole(role) =
               let role : Role = { Id = role }
               Provider.saveRoles [role]

        member x.CreateUser(username, password, email) =
            match Provider.tryGetUser username with
            | None ->
                let user = 
                    {
                        Id = username
                        Username = username
                        Password = (computeHash (salt()) password)
                        Email = email
                        Roles = new ResizeArray<_>([])
                    }
                
                Provider.saveUsers [user]
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
                    //FormsAuthentication.SetAuthCookie(username, rememberme)
                    true
                else false
            | None -> false

        member x.Logout() = FormsAuthentication.SignOut()

        member x.RemoveUserFromRoles(username, roles) =
            match Provider.tryGetUser username with
            | Some(user) ->
                for role in roles do
                    user.Roles.Remove(role) |> ignore
                Provider.saveUsers [user]
            | None -> failwithf "Could not find user %s" username


        member x.Dispose() = 
            ()
            
