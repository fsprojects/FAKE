[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.DeployAgentModule
open System
open Fake
open Fake.DeploymentHelper
open Nancy
open Nancy.ModelBinding
open Microsoft.FSharp.Reflection

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<AutoOpen>]
module Op =
    let private nullString:string = null

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let (?>) (target : obj) targetKey =
        let t = target :?> DynamicDictionary
        let x = t.[targetKey] :?> DynamicDictionaryValue
        if x.HasValue then x.Value.ToString() else nullString

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type FakeModule(path) =
    inherit NancyModule(path)

    let http (httpMethod:NancyModule.RouteBuilder) urlPart f =
        httpMethod.[urlPart] <- fun x -> f x |> box

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    new () = FakeModule("")
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]    
    member this.get urlPart f = http this.Get urlPart f
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.post urlPart f = http this.Post urlPart f
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.put urlPart f = http this.Put urlPart f
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.delete urlPart f = http this.Delete urlPart f
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.InternalServerError (e:Exception) =
        this.Response
            .AsText(e.ToString())
            .WithStatusCode HttpStatusCode.InternalServerError

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.returnAsJson f logError =
        try
            this.Response.AsJson (f())
        with e ->
            logError e
            this.InternalServerError e

