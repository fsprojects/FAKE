module Fake.Json

open Newtonsoft.Json

/// Serializes a object to json
let serialize x = JsonConvert.SerializeObject x

/// Deserializes a text into a object of type 'a
let deserialize<'a> text :'a = JsonConvert.DeserializeObject<'a>(text)

/// Deserializes a file into a object of type 'a
let deserializeFile<'a> = ReadFileAsString >> deserialize<'a>