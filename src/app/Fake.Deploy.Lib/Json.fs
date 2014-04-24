/// Contains extensions for Newtonsoft.Json. **Don't use it directly. It's likely to be changed in further versions.**
module Fake.Json

open Newtonsoft.Json
open System
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations

/// Serializes a object to json
let serialize x = JsonConvert.SerializeObject(x, Formatting.Indented)

/// Deserializes a text into a object of type 'a
let deserialize<'a> text : 'a = JsonConvert.DeserializeObject<'a>(text)

/// Deserializes a file into a object of type 'a
let deserializeFile<'a> = ReadFileAsString >> deserialize<'a>
