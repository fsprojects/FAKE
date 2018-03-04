namespace Fake.Deploy.Web.Module
open System
open Nancy
open Newtonsoft.Json


module NancyOp =
    let private nullString:string = null

    let (?>) (target : obj) targetKey =
        let t = target :?> DynamicDictionary
        let x = t.[targetKey] :?> DynamicDictionaryValue
        if x.HasValue then x.Value.ToString() else nullString

    let fromJSON<'T> str =
        JsonConvert.DeserializeObject<'T>(str)


type FakeModule(path) =
    inherit NancyModule(path)

    let http (httpMethod:NancyModule.RouteBuilder) urlPart f =
        httpMethod.[urlPart] <- fun x -> f x |> box

    new () = FakeModule("")
    member this.get urlPart f = http this.Get urlPart f
    member this.post urlPart f = http this.Post urlPart f
    member this.put urlPart f = http this.Put urlPart f
    member this.delete urlPart f = http this.Delete urlPart f
    member this.InternalServerError (e:Exception) =
        this.Response
            .AsText(e.ToString())
            .WithStatusCode HttpStatusCode.InternalServerError

    member this.returnAsJson f logError =
        try
            this.Response.AsJson (f())
        with e ->
            logError e
            this.InternalServerError e
