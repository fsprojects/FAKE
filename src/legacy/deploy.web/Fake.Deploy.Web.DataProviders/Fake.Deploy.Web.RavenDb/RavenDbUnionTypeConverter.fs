namespace Fake.Deploy.Web.RavenDb

open System
open Microsoft.FSharp.Reflection
open Raven.Imports.Newtonsoft.Json

type internal RavenUnionTypeConverter() =
    inherit Raven.Imports.Newtonsoft.Json.JsonConverter()

    let doRead pos (reader: Raven.Imports.Newtonsoft.Json.JsonReader) = 
        reader.Read() |> ignore 

    override x.CanConvert(typ:Type) =
        let result = 
            ((typ.GetInterface(typeof<System.Collections.IEnumerable>.FullName) = null) 
            && FSharpType.IsUnion typ)
        result

    override x.WriteJson(writer: Raven.Imports.Newtonsoft.Json.JsonWriter, value: obj, serializer: Raven.Imports.Newtonsoft.Json.JsonSerializer) =
        let t = value.GetType()
        let write (name : string) (fields : obj []) = 
            writer.WriteStartObject()
            writer.WritePropertyName("case")
            writer.WriteValue(name)  
            writer.WritePropertyName("values")
            serializer.Serialize(writer, fields)
            writer.WriteEndObject()   

        let (info, fields) = FSharpValue.GetUnionFields(value, t)
        write info.Name fields

    override x.ReadJson(reader: Raven.Imports.Newtonsoft.Json.JsonReader, objectType: Type, existingValue: obj, serializer: Raven.Imports.Newtonsoft.Json.JsonSerializer) =      
         let cases = FSharpType.GetUnionCases(objectType)
         if reader.TokenType <> Raven.Imports.Newtonsoft.Json.JsonToken.Null  
         then 
            doRead "1" reader
            doRead "2" reader
            let case = cases |> Array.find(fun x -> x.Name = if reader.Value = null then "None" else reader.Value.ToString())
            doRead "3" reader
            doRead "4" reader
            doRead "5" reader
            let fields =  [| 
                   for field in case.GetFields() do
                       let result = serializer.Deserialize(reader, field.PropertyType)
                       reader.Read() |> ignore
                       yield result
             |] 
            let result = FSharpValue.MakeUnion(case, fields)
            while reader.TokenType <> Raven.Imports.Newtonsoft.Json.JsonToken.EndObject do
                doRead "6" reader         
            result
         else
            FSharpValue.MakeUnion(cases.[0], [||]) 

