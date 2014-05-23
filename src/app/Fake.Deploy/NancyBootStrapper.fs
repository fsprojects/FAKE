namespace Fake

open System
open Newtonsoft.Json
open Nancy
open Nancy.Security
open Nancy.Authentication.Stateless
open Nancy.Serialization.JsonNet
open Fake.AppConfig
open Fake.Deploy
open Fake.Deploy.Auth
open Fake.DeployAgentModule
open Fake.SshRsaModule

type NancyBootStrapper() = 
    inherit DefaultNancyBootstrapper()
    let dummyUser = AuthenticatedUser("None", []) :> IUserIdentity
    
    let statelessAuthConfig (userMapper : UserMapper) = 
        StatelessAuthenticationConfiguration(fun ctx -> 
            match (AppConfig.Authorization) with
            | Off -> dummyUser
            | On -> 
                let token = ctx.Request.Headers.Item(AuthTokenName) |> Seq.tryFind(fun x -> true)
                match token with
                | None -> null
                | Some token ->
                    match Guid.TryParse token with
                    | false, _ -> null
                    | true, token ->
                        match userMapper.GetUser token with
                        | None -> null
                        | Some u -> AuthenticatedUser(u.UserName, u.Claims) :> IUserIdentity)
    
    override this.ApplicationStartup(container, pipelines) = 
        StaticConfiguration.EnableRequestTracing <- true
        StaticConfiguration.DisableErrorTraces <- false
        Nancy.Json.JsonSettings.MaxJsonLength <- 10 * 1024 * 1024
        let userMapper = container.Resolve<UserMapper>()
        StatelessAuthentication.Enable(pipelines, statelessAuthConfig userMapper)
        base.ApplicationStartup(container, pipelines)
    
    override this.ConfigureApplicationContainer container = 
        base.ConfigureApplicationContainer container
        container.Register(typedefof<JsonNetSerializer>, typedefof<JsonSerializerForFsharp>) |> ignore
        container.Register (typedefof<JsonNetBodyDeserializer>, typedefof<JsonBodySerializerForFsharp>) |> ignore

        container.Register<UserMapper, UserMapper>().AsSingleton() |> ignore
        let keys = 
            match AppConfig.Authorization with
            | Off -> []
            | On -> loadPublicKeys AppConfig.AuthorizedKeysFile |> List.ofArray
        container.Register<list<PublicKey>, list<PublicKey>>(keys) |> ignore
        container.Register<unit -> Guid, unit -> Guid>(Guid.NewGuid) |> ignore
