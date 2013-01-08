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

type EnvironmentController() =
    inherit ApiController()

    member this.Get() = 
        try
            this.Request.CreateResponse(HttpStatusCode.OK, Model.getEnvironments())
        with e ->
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Post(env : Model.Environment) = 
        try
            saveEnvironment env
            this.Request.CreateResponse(HttpStatusCode.Created)
        with e ->
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)

    member this.Delete(id : string) = 
        try
            Model.deleteEnvironment id
            this.Request.CreateResponse(HttpStatusCode.OK)
        with e ->
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
    
type AgentController() = 
    inherit ApiController()
    
    member this.Get() = Model.getAgents()

    member this.Get(id : string) = Model.getAgent id

    member this.Post(data : HttpRequestMessage) =
            async {
                if data.Content.IsFormData() 
                then
                    let! formData = data.Content.ReadAsFormDataAsync() |> Async.AwaitTask
                    let agentUrl = formData.Get("agentUrl")
                    let agentName = formData.Get("agentName")
                    let environmentId = formData.Get("environmentId")
                    try
                        let agent = Model.Agent.Create(agentUrl, agentName)
                        Model.saveAgent environmentId agent
                        return created (sprintf "Created Agent %s" agentName) this
                    with e ->
                        return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
                else return this.Request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, "Expected URL encoded form data")
            } |> Async.toTask

    member this.Delete(id : string) =
        try
            Model.deleteAgent id
            this.Request.CreateResponse(HttpStatusCode.OK)
        with e ->
            this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
        

type PackageController() =
    inherit ApiController()

    member this.Post() =
        let path = HttpContext.Current.Server.MapPath("~/App_Data/Package_Temp")
        async {
            let! result = savePostedFiles path this
            let provider, fileNames = result
            let filePath = Path.Combine(path, fileNames |> Seq.head)
            let agentUrl = provider.FormData.Get("agentUrl")
            match postDeploymentPackage (agentUrl + "/fake/") filePath with
            | Failure(err) -> return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, err :?> System.Exception)
            | Success -> 
                if File.Exists(filePath) then File.Delete(filePath)
                return created (sprintf "Successfully deployed package to %s" agentUrl) this
            | _ -> return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unexpected response")
        } |> Async.toTask

