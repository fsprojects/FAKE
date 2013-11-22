namespace Fake.Deploy.Web

open System
open System.IO
open System.Linq
open System.Net
open System.Web.Http
open System.Web
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks
open Newtonsoft.Json

[<AutoOpen>]
module ApiHelpers =

    type JsonNetFormatter(?jsonSerializerSettings:JsonSerializerSettings) as self =
        inherit Formatting.MediaTypeFormatter()
    
        let encoding = new Text.UTF8Encoding(false, true)
    
        let _jsonSerializerSettings = 
            let jss = defaultArg jsonSerializerSettings (new JsonSerializerSettings())
            jss.Converters.Add(new Helpers.NewtonsoftUnionTypeConverter())
            jss.Converters.Add(new Converters.IsoDateTimeConverter())
            jss
    
        do
            // Fill out the mediatype and encoding we support
            self.SupportedMediaTypes.Add(new Headers.MediaTypeHeaderValue("application/json"));
        
        override x.CanWriteType(``type``:Type) =
            true
    
        override x.CanReadType(``type`` : Type) = 
            true
            
        override x.ReadFromStreamAsync(``type``:Type, stream:Stream, contentHeaders:HttpContent, formatterContext:Formatting.IFormatterLogger) =
            // Create a serializer
            let serializer = JsonSerializer.Create(_jsonSerializerSettings); 
            // Create task reading the content
            Task.Factory.StartNew(fun () ->        
                use streamReader = new StreamReader(stream, encoding)
                use jsonTextReader = new JsonTextReader(streamReader)
                serializer.Deserialize(jsonTextReader, ``type``))
    
        override x.WriteToStreamAsync(``type``:Type, value:obj, stream:Stream, contentHeaders:HttpContent, transportContext:TransportContext) =
            // Create a serializer
            let serializer = JsonSerializer.Create(_jsonSerializerSettings);
    
            // Create task writing the serialized content
            Task.Factory.StartNew(fun () ->
                use jsonTextWriter = new JsonTextWriter(new StreamWriter(stream, encoding))
                jsonTextWriter.CloseOutput <- false
                serializer.Serialize(jsonTextWriter, value)
                jsonTextWriter.Flush())
       
    open System.Net.Http

    module Async = 
         
         let toTask (async : Async<_>) = Task.Factory.StartNew(fun _ -> Async.RunSynchronously(async))

    let jsonSettings = 
         let jss = new JsonSerializerSettings()
         jss.Converters.Add(new Helpers.NewtonsoftUnionTypeConverter())
         jss.Converters.Add(new Converters.IsoDateTimeConverter())
         jss.NullValueHandling <- NullValueHandling.Ignore
         jss

    let savePostedFiles (path : string) (controller : ApiController) = 
        let request = controller.Request
        if (not <| request.Content.IsMimeMultipartContent()) 
        then raise(new HttpResponseException(HttpStatusCode.UnsupportedMediaType))
        else 
            async {
                 let! provider = request.Content.ReadAsMultipartAsync(new MultipartFormDataStreamProvider(path)) |> Async.AwaitTask; 
                 return 
                     provider, seq {
                          for fileData in provider.FileData do
                              yield fileData.LocalFileName
                     }
             }

    let created content (controller : ApiController) = 
        let resp = new HttpResponseMessage(HttpStatusCode.Created);
        resp.Headers.Location <- controller.Request.Headers.Referrer
        resp.Content <- new ObjectContent(content.GetType(), content, new JsonNetFormatter(jsonSettings))
        resp

