namespace Fake.Deploy.Web.Module
open System
open Nancy
open Newtonsoft.Json

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module NancyOp =
    let private nullString:string = null

    let (?>) (target : obj) targetKey =
        let t = target :?> DynamicDictionary
        let x = t.[targetKey] :?> DynamicDictionaryValue
        if x.HasValue then x.Value.ToString() else nullString

    let fromJSON<'T> str =
        JsonConvert.DeserializeObject<'T>(str)

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type FakeModule(path) =
    inherit NancyModule(path)

    let http (httpMethod:NancyModule.RouteBuilder) urlPart f =
        httpMethod.[urlPart] <- fun x -> f x |> box

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    new () = FakeModule("")

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.get urlPart f = http this.Get urlPart f
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.post urlPart f = http this.Post urlPart f
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.put urlPart f = http this.Put urlPart f
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.delete urlPart f = http this.Delete urlPart f
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.InternalServerError (e:Exception) =
        this.Response
            .AsText(e.ToString())
            .WithStatusCode HttpStatusCode.InternalServerError

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.returnAsJson f logError =
        try
            this.Response.AsJson (f())
        with e ->
            logError e
            this.InternalServerError e
