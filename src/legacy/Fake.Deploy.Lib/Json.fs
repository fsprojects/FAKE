/// Contains extensions for Newtonsoft.Json. **Don't use it directly. It's likely to be changed in further versions.**
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.Json

open Newtonsoft.Json
open System
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations

/// Serializes a object to json
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let serialize x = JsonConvert.SerializeObject(x, Formatting.Indented)

/// Deserializes a text into a object of type 'a
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let deserialize<'a> text : 'a = JsonConvert.DeserializeObject<'a>(text)

/// Deserializes a file into a object of type 'a
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let deserializeFile<'a> = ReadFileAsString >> deserialize<'a>

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type ParsingException(input : string, inner : exn) =
    inherit Exception(sprintf "Faied to parse input: %s" input, inner)

/// Tryes to deserialize a text into a object of type 'a and returns either instance of 'a or parsing error
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let tryDeserialize<'a> (s: string) : Choice<'a, exn> =
    try
        deserialize<'a> s |> Choice1Of2
    with exn -> ParsingException(s, exn)
                :> exn 
                |> Choice2Of2



