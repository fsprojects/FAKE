namespace Fake.Deploy.Web.Controllers

open System.IO
open System.Web
open System.Web.Mvc
open System.Web.Http
open System.Net
open System.Net.Http
open Fake.Deploy.Web
open Fake.Deploy.Web.Model
open Fake.HttpClientHelper
open log4net

type EnvironmentController() =
    inherit ApiController()

    let logger = LogManager.GetLogger("EnvironmentController")

    member this.Get() = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Model.getEnvironments())
        with e ->
            logger.Error("An error occured retrieving environments" ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Get(id : string) = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Model.getEnvironment id)
        with e ->
            logger.Error(sprintf "An error occured retrieving environment %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Post(env : Model.Environment) = 
        try
            saveEnvironment env
            this.Request.CreateResponse(HttpStatusCode.Created)
        with e ->
            logger.Error("An error occured saving environment" ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Delete(id : string) = 
        try
            Model.deleteEnvironment id
            this.Request.CreateResponse(HttpStatusCode.OK)
        with e ->
            logger.Error(sprintf "An error occured delete environment %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
    
type AgentController() = 
    inherit ApiController()
    
    let logger = LogManager.GetLogger("AgentController")

    member this.Get() = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Model.getAgents())
        with e ->
            logger.Error("An error occured retrieving agents" ,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Get(id : string) =
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Model.getAgent id)
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
                        let agent = Model.Agent.Create(agentUrl, agentName)
                        Model.saveAgent environmentId agent
                        return this.Request.CreateResponse(HttpStatusCode.Created)
                    with e ->
                        logger.Error("An error occured saving agent" ,e)
                        return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
                else return this.Request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, "Expected URL encoded form data")
            } |> Async.toTask

    member this.Delete(id : string) =
        try
            Model.deleteAgent id
            this.Request.CreateResponse(HttpStatusCode.OK)
        with e ->
            logger.Error(sprintf "An error occured retrieving agent %s" id,e)
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
        

type PackageController() =
    inherit ApiController()

    let logger = LogManager.GetLogger("PackageController")

    let packageTemp = 
        let dir = DirectoryInfo(HttpContext.Current.Server.MapPath("~/App_Data/Package_Temp"))
        if not <| dir.Exists then dir.Create()
        dir

    member this.Post() =
        async {
            try
                let! result = savePostedFiles packageTemp.FullName this
                let provider, fileNames = result
                let filePath = Path.Combine(packageTemp.FullName, fileNames |> Seq.head)
                let agentUrl = provider.FormData.Get("agentUrl")

                match postDeploymentPackage (agentUrl.Trim('/') + "/fake/") filePath with
                | Failure(err) -> 
                    return raise(err :?> System.Exception)
                | Success -> 
                    if File.Exists(filePath) then File.Delete(filePath)
                    return created (sprintf "Successfully deployed package to %s" agentUrl) this
                | _ -> return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unexpected response")
            with e ->
                logger.Error("An error occured deploying package",e)
                return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
        } |> Async.toTask

