namespace Fake.Deploy.Web

open System
open System.IO
open System.Linq
open System.Net
open System.Web
open System.Threading.Tasks
open Newtonsoft.Json

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<AutoOpen>]
module ApiHelpers =
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    module Async = 
         let toTask (async : Async<_>) = Task.Factory.StartNew(fun _ -> Async.RunSynchronously(async))
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let jsonSettings = 
         let jss = new JsonSerializerSettings()
         jss.Converters.Add(new Helpers.NewtonsoftUnionTypeConverter())
         jss.Converters.Add(new Converters.IsoDateTimeConverter())
         jss.NullValueHandling <- NullValueHandling.Ignore
         jss


