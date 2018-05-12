namespace Fake.Deploy.Web

open System
open System.IO
open System.Linq
open System.Net
open System.Web
open System.Threading.Tasks
open Newtonsoft.Json

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<AutoOpen>]
module ApiHelpers =
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    module Async = 
         let toTask (async : Async<_>) = Task.Factory.StartNew(fun _ -> Async.RunSynchronously(async))
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let jsonSettings = 
         let jss = new JsonSerializerSettings()
         jss.Converters.Add(new Helpers.NewtonsoftUnionTypeConverter())
         jss.Converters.Add(new Converters.IsoDateTimeConverter())
         jss.NullValueHandling <- NullValueHandling.Ignore
         jss


