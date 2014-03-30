/// Contains extensions for Newtonsoft.Json. **Don't use it directly. It's likely to be changed in further versions.**
module Fake.Json

open Newtonsoft.Json
open System
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations

/// Newtonsoft.Json converter for union types
type UnionTypeConverter() = 
    inherit JsonConverter()
    let doRead (reader : JsonReader) = reader.Read() |> ignore
    
    override x.CanConvert(typ : Type) = 
        let result = 
            ((typ.GetInterface(typeof<System.Collections.IEnumerable>.FullName) = null) && FSharpType.IsUnion typ)
        result
    
    override x.WriteJson(writer : JsonWriter, value : obj, serializer : JsonSerializer) = 
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
    
    override x.ReadJson(reader : JsonReader, objectType : Type, existingValue : obj, serializer : JsonSerializer) = 
        let cases = FSharpType.GetUnionCases(objectType)
        if reader.TokenType <> JsonToken.Null then 
            doRead reader
            doRead reader
            let value = 
                if reader.Value = null then "None"
                else reader.Value.ToString()
            
            let case = cases |> Array.find (fun x -> x.Name = value)
            doRead reader
            doRead reader
            doRead reader
            let fields = 
                [| for field in case.GetFields() do
                       let result = serializer.Deserialize(reader, field.PropertyType)
                       reader.Read() |> ignore
                       yield result |]
            
            let result = FSharpValue.MakeUnion(case, fields)
            while reader.TokenType <> JsonToken.EndObject do
                doRead reader
            result
        else FSharpValue.MakeUnion(cases.[0], [||])

let private settings = 
    let s = new JsonSerializerSettings()
    s.Converters.Add(new UnionTypeConverter())
    s

/// Serializes a object to json
let serialize x = JsonConvert.SerializeObject(x, Formatting.Indented, settings)

/// Deserializes a text into a object of type 'a
let deserialize<'a> text : 'a = JsonConvert.DeserializeObject<'a>(text, settings)

/// Deserializes a file into a object of type 'a
let deserializeFile<'a> = ReadFileAsString >> deserialize<'a>
