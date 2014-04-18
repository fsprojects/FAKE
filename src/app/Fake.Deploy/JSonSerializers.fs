namespace Fake
open Fake.Json
open Newtonsoft.Json
open Nancy.Serialization.JsonNet

type JsonSerializerForFsharp =
    inherit JsonNetSerializer
    
    new (s:JsonSerializer) =
        { inherit JsonNetSerializer(s) }

type JsonBodySerializerForFsharp =
    inherit JsonNetBodyDeserializer
    
    new (s:JsonSerializer) =
        { inherit JsonNetBodyDeserializer(s) }

