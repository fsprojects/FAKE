namespace Fake.Deploy.Web

open System
open System.IO
open System.Linq
open System.Net
open System.Web
open System.Threading.Tasks
open Newtonsoft.Json

[<AutoOpen>]
module ApiHelpers =

    module Async = 
         let toTask (async : Async<_>) = Task.Factory.StartNew(fun _ -> Async.RunSynchronously(async))

    let jsonSettings = 
         let jss = new JsonSerializerSettings()
         jss.Converters.Add(new Helpers.NewtonsoftUnionTypeConverter())
         jss.Converters.Add(new Converters.IsoDateTimeConverter())
         jss.NullValueHandling <- NullValueHandling.Ignore
         jss


