namespace Fake.Deploy.Web

module Model = 

    open System
    open System.IO
    open System.Web
    open Raven.Imports.Newtonsoft.Json
    open Raven.Client.Embedded
    open Microsoft.FSharp.Reflection
    open System.ComponentModel.DataAnnotations

    type UnionTypeConverter() =
        inherit JsonConverter()
    
        let doRead pos (reader: JsonReader) = 
            reader.Read() |> ignore 
    
        override x.CanConvert(typ:Type) =
            let result = 
                ((typ.GetInterface(typeof<System.Collections.IEnumerable>.FullName) = null) 
                && FSharpType.IsUnion typ)
            result
    
        override x.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
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
    
        override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) =      
             let cases = FSharpType.GetUnionCases(objectType)
             if reader.TokenType <> JsonToken.Null  
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
                while reader.TokenType <> JsonToken.EndObject do
                    doRead "6" reader         
                result
             else
                FSharpValue.MakeUnion(cases.[0], [||]) 

    type Environment() =
        [<Required>]member val Id : string = null with get, set
        [<Required>]member val Description : string = null with get, set
    
    type Agent() =
        [<Required>]member val Id : string = null with get, set
        [<Required>]member val Name : string = null with get, set
        [<Required>]member val Server : string = null with get, set
        [<Required>]member val Port : int = 0 with get, set
        [<Required>]member val Environment : string = null with get, set

        static member Create(name : string, environment : string, url : Uri) =
            Agent(
                Id = null,
                Name = name,
                Server = url.Host,
                Port = url.Port,
                Environment = environment
            )    

    let private documentStore = 
        let ds = new EmbeddableDocumentStore()
        ds.DataDirectory <- "~/App_Data/"
        ds.Conventions.IdentityPartsSeparator <- "-"
        ds.Conventions.CustomizeJsonSerializer <- new Action<_>(fun s -> s.Converters.Add(new UnionTypeConverter()))
        ds.Initialize()

    let Save (instances : seq<_>) =
        use session = documentStore.OpenSession()
        let count = ref 0
        for inst in instances do
            session.Store(inst)
            incr(count)
            if !count > 29 then session.SaveChanges()
        session.SaveChanges()
    
    let Agent (id : string) = 
        match id with
        | null -> Agent()
        | _ -> 
            use session = documentStore.OpenSession()
            session.Load<Agent>(id)

    let DeleteAgent (id : string) =
        use session = documentStore.OpenSession()
        let x = session.Load<Agent>(id)        
        session.Delete(x)
        session.SaveChanges()   

    let Agents() = 
        use session = documentStore.OpenSession()
        query {
            for env in session.Query<Agent>() do
            select env
        } |> Seq.toArray

    let Environment (id : string) = 
        match id with
        | null -> Environment()
        | _ ->
            use session = documentStore.OpenSession()
            session.Load<Environment>(id)

    let DeleteEnvironment (id : string) =
        use session = documentStore.OpenSession()
        let x = session.Load<Environment>(id)        
        session.Delete(x)
        session.SaveChanges()

    let Environments() = 
        use session = documentStore.OpenSession()
        query {
            for env in session.Query<Environment>() do
            select env
        } |> Seq.toArray




