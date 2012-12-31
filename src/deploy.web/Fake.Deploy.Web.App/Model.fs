namespace Fake.Deploy.Web

module Model = 

    open System
    open System.IO
    open System.Web
    open System.Runtime.Serialization
    open Raven.Imports.Newtonsoft.Json
    open Raven.Client.Embedded
    open Raven.Client.Indexes
    open Microsoft.FSharp.Reflection
    open System.ComponentModel.DataAnnotations
    open System.ComponentModel.Composition

    type internal UnionTypeConverter() =
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

    [<CLIMutable>]
    [<DataContract>]
    type Agent = {
        [<DataMember>]Id : string
        [<DataMember>]Name : string
        [<DataMember>]Address : Uri
        }
        with
            static member Create(url : string, ?name : string) =
                let url = Uri(url)
                {
                    Id = null
                    Name = defaultArg name url.Host
                    Address = url
                }

    [<CLIMutable>]
    [<DataContract>]
    type Environment = {
            [<DataMember>]Id : string
            [<DataMember>]Description : string
            [<DataMember>]Agents : seq<Agent>
        }
        with
            member x.AddAgents(agents : seq<Agent>) = 
                   { x with Agents = Seq.append agents x.Agents }

    let private documentStore = 
        let ds = new EmbeddableDocumentStore(RunInMemory = true)
        ds.Conventions.CustomizeJsonSerializer <- new Action<_>(fun s -> s.Converters.Add(new UnionTypeConverter()))
        ds.Initialize()
    
    let private createIndexes (assems : seq<Reflection.Assembly>) =
        assems |> Seq.iter (fun ass -> IndexCreation.CreateIndexes(ass, documentStore)) 

    let private createData() = 
        [
            { Id = "Development";
              Description = "Development Environment";
              Agents = [Agent.Create("http://localhost:8080"); Agent.Create("http://dev-2:8080")]}
            { Id = "Integration";
              Description = "Integration Environment";
              Agents = [Agent.Create("http://int-1:8080"); Agent.Create("http://int-2:8080")] }
            { Id = "Staging";
              Description = "User Acceptance and pre-Production environment";
              Agents = [Agent.Create("http://uat-1:8080"); Agent.Create("http://uat-2:8080")] }
            { Id = "Production";
              Description = "User Acceptance and pre-Production environment";
              Agents = [Agent.Create("http://prod-1:8080"); Agent.Create("http://prod-2:8080")] }
        ]



    let Save (instances : seq<_>) =
        use session = documentStore.OpenSession()
        let count = ref 0
        for inst in instances do
            session.Store(inst)
            incr(count)
            if !count > 29 then session.SaveChanges()
        session.SaveChanges()

    let Init(assembly : seq<Reflection.Assembly>) = 
        createIndexes assembly
        createData() |> Save

    let Environment (id : string) = 
        match id with
        | null -> { Id = null; Description = null; Agents = Seq.empty }
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




