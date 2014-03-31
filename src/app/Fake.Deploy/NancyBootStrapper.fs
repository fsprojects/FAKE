namespace Fake
open System
open Nancy
open Nancy.Security
open Newtonsoft.Json

type NancyBootStrapper() =
    inherit DefaultNancyBootstrapper()

    override this.ApplicationStartup (container, pipelines) =
        StaticConfiguration.EnableRequestTracing <- true
        StaticConfiguration.DisableErrorTraces <- false
        Nancy.Json.JsonSettings.MaxJsonLength <- 10 * 1024 * 1024
        base.ApplicationStartup(container, pipelines)

    override this.ConfigureApplicationContainer container =
        base.ConfigureApplicationContainer container
        container.Register(typedefof<Nancy.Serialization.JsonNet.JsonNetSerializer>, typedefof<JsonSerializerForFsharp>) |> ignore
        container.Register(typedefof<Nancy.Serialization.JsonNet.JsonNetBodyDeserializer>, typedefof<JsonBodySerializerForFsharp>) |> ignore
