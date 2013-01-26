namespace Fake.Deploy.Web.Controllers

open System.IO
open System.Web
open System.Web.Mvc
open System.Web.Http
open System.Net
open System.Net.Http
open Fake.Deploy.Web
open Fake.HttpClientHelper
open System.Web.Security
open log4net

module ApiModels =

    type CreateUserModel = {
        UserName : string
        Password : string
        ConfirmPassword : string
        Email : string
    }

type UserController() =
    inherit ApiController()

    let logger = LogManager.GetLogger("UserController")

    member this.Get() = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Membership.GetAllUsers())
        with e ->
            logger.Error("An error occured retrieving users" ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Get(id : string) = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Data.getUser id)
        with e ->
            logger.Error(sprintf "An error occured retrieving user %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Post(model : ApiModels.CreateUserModel) = 
        async {
            try
                if model.Password = model.ConfirmPassword then
                    match Data.registerUser model.UserName model.Password model.Email with
                    | MembershipCreateStatus.Success, user -> return this.Request.CreateResponse(HttpStatusCode.Created)
                    | _,s -> return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, sprintf "User not created %s" (s.ToString()));
                else return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Passwords do not match");
            with e -> 
                return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
        } |> Async.toTask

    member this.Delete(id : string) = 
        try
            if Data.deleteUser id
            then this.Request.CreateResponse(HttpStatusCode.OK)
            else this.Request.CreateErrorResponse(HttpStatusCode.NotFound, sprintf "Could not find user %s" id)
        with e ->
            logger.Error(sprintf "An error occured deleting user %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

type EnvironmentController() =
    inherit ApiController()

    let logger = LogManager.GetLogger("EnvironmentController")

    member this.Get() = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Data.getEnvironments())
        with e ->
            logger.Error("An error occured retrieving environments" ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Get(id : string) = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Data.getEnvironment id)
        with e ->
            logger.Error(sprintf "An error occured retrieving environment %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Post(env : Environment) = 
        try
            Data.saveEnvironment env
            this.Request.CreateResponse(HttpStatusCode.Created)
        with e ->
            logger.Error("An error occured saving environment" ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Delete(id : string) = 
        try
            Data.deleteEnvironment id
            this.Request.CreateResponse(HttpStatusCode.OK)
        with e ->
            logger.Error(sprintf "An error occured delete environment %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
    
type AgentController() = 
    inherit ApiController()
    
    let logger = LogManager.GetLogger("AgentController")

    member this.Get() = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Data.getAgents())
        with e ->
            logger.Error("An error occured retrieving agents" ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Get(id : string) =
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Data.getAgent id)
        with e ->
            logger.Error(sprintf "An error occured retrieving agent %s" id ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Post(data : HttpRequestMessage) =
            async {
                if data.Content.IsFormData() 
                then
                    let! formData = data.Content.ReadAsFormDataAsync() |> Async.AwaitTask
                    let agentUrl = formData.Get("agentUrl").Trim('/') + "/"
                    let agentName = formData.Get("agentName")
                    let environmentId = formData.Get("environmentId")
                    try
                        let agent = Agent.Create(agentUrl, agentName)
                        Data.saveAgent environmentId agent
                        return this.Request.CreateResponse(HttpStatusCode.Created)
                    with e ->
                        logger.Error("An error occured saving agent" ,e)
                        return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
                else return this.Request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, "Expected URL encoded form data")
            } |> Async.toTask

    member this.Delete(id : string) =
        try
            Data.deleteAgent id
            this.Request.CreateResponse(HttpStatusCode.OK)
        with e ->
            logger.Error(sprintf "An error occured retrieving agent %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
     
type RollbackRequest = {
    agentUrl : string
    version : string
    appName : string
}   

type PackageController() =
    inherit ApiController()

    let logger = LogManager.GetLogger("PackageController")

    let packageTemp = 
        let dir = DirectoryInfo(HttpContext.Current.Server.MapPath("~/App_Data/Package_Temp"))
        if not <| dir.Exists then dir.Create()
        dir

    [<HttpPost>]
    member this.Rollback(body : RollbackRequest) =
        async {
            try
               match rollbackTo (body.agentUrl.Trim('/') + "/fake/") body.appName body.version with
               | Failure(err) -> 
                   return this.Request.CreateResponse(HttpStatusCode.InternalServerError, err)
               | Success a -> 
                   let msg = this.Request.CreateResponse(HttpStatusCode.OK, a)
                   msg.Headers.Location <- this.Request.Headers.Referrer
                   return msg
               | _ -> 
                   return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unexpected response")
            with e ->
                logger.Error("An error occured rolling back package",e)
                return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
        } |> Async.toTask

    [<HttpPost>]
    member this.Deploy() =
        async {
            try
                let! result = savePostedFiles packageTemp.FullName this
                let provider, fileNames = result
                let filePath = Path.Combine(packageTemp.FullName, fileNames |> Seq.head)
                let agentUrl = provider.FormData.Get("agentUrl")

                match postDeploymentPackage (agentUrl.Trim('/') + "/fake/") filePath with
                | Failure(err) -> 
                    return this.Request.CreateResponse(HttpStatusCode.InternalServerError, err)
                | Success a -> 
                    if File.Exists(filePath) then File.Delete(filePath)
                    return created a this
                | _ -> 
                    return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unexpected response")
            with e ->
                logger.Error("An error occured deploying package",e)
                return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
        } |> Async.toTask

