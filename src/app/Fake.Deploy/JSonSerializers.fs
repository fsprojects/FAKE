namespace Fake
open Fake.Json
open Newtonsoft.Json
open Nancy.Serialization.JsonNet

type JsonSerializerForFsharp =
    inherit JsonNetSerializer
    
    new (s:JsonSerializer) =
        s.Converters.Add(UnionTypeConverter())
        { inherit JsonNetSerializer(s) }

type JsonBodySerializerForFsharp =
    inherit JsonNetBodyDeserializer
    
    new (s:JsonSerializer) =
        s.Converters.Add(UnionTypeConverter())
        { inherit JsonNetBodyDeserializer(s) }

