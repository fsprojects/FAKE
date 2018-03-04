namespace Fake.Deploy.Web.Module
open System
open System.Web.Security
open log4net
open Nancy
open Nancy.ModelBinding
open Nancy.Security
open Fake.Deploy.Web
open Fake.Deploy.Web.Module.NancyOp

type ApiUser (membershipProvider : IMembershipProvider) as http =
    inherit FakeModule("/api/v1/user")


    let logger = LogManager.GetLogger(http.GetType().Name)
    
    do
        http.get "/" (fun p ->
            http.returnAsJson
                (fun () -> membershipProvider.GetUsers())
                (fun e -> logger.Error("An error occured retrieving users" , e))
        )

        http.get "/{id}" (fun p ->
            let id = p ?> "id"
            try
                match membershipProvider.GetUser id with
                | Some u -> http.Response.AsJson(u)
                | None -> http.Response.AsText("").WithStatusCode HttpStatusCode.NotFound
            with e ->
                logger.Error(sprintf "An error occured retrieving user %s" id, e)
                http.InternalServerError e
        )

        http.post "/" (fun p ->
            let user = http.Bind<Registration>()
            try
                if user.Password = user.ConfirmPassword then
                    match membershipProvider.CreateUser(user.UserName, user.Password, user.Email) with
                    | MembershipCreateStatus.Success, user -> 
                        http.Response.AsJson(user).WithStatusCode HttpStatusCode.Created
                    | _, s -> http.Response.AsText(sprintf "User not created %s" (s.ToString())).WithStatusCode HttpStatusCode.InternalServerError
                else http.Response.AsText("Passwords do not match").WithStatusCode HttpStatusCode.BadRequest
            with e ->
                logger.Error(sprintf "Error creating user %A" user)
                http.InternalServerError e
        )

        http.delete "/{id}" (fun p ->
            let id = p ?> "id"
            try
                match membershipProvider.DeleteUser id with
                | false -> http.Response.AsText(sprintf "Could not find user %s" id).WithStatusCode HttpStatusCode.NotFound
                | true -> http.Response.AsText("").WithStatusCode HttpStatusCode.NoContent
            with e ->
                logger.Error(sprintf "An error occured deleting user %s" id, e)
                http.InternalServerError e
        )
