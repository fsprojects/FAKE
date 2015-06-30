[<AutoOpen>]
/// This module contains functions which allow to read command line options
module Fake.CommandLineHelper

type internal ParamValue = 
| String of string
| NoValue

type private ParseMemo = {
    Values : list<string * ParamValue>
    Previous : string option
}

let private isKey (value : string) = 
    value.StartsWith "-" || 
    value.StartsWith "--" || 
    value.StartsWith "/"
let cleanKey (value : string) = 
    let keys = ["--"; "-"; "/"]
    let key = 
        keys |> List.tryFind(fun key -> value.StartsWith key)
    match key with
    | None -> value
    | Some key -> value.Substring(key.Length)

let private parseCLIParams (args : string seq) =

    let parsed = 
        args
        |> Seq.fold(fun memo value ->
            match memo.Previous with
            | None -> { memo with Previous = Some value }
            | Some prev -> 
                if not (isKey value) then
                    { memo with 
                        Values = memo.Values @ [cleanKey prev, String value]
                        Previous = None }
                else 
                    { memo with 
                        Values = memo.Values @ [cleanKey prev, NoValue]
                        Previous = Some value }
        ) {Values = []; Previous = None}
    
    let values = 
        match parsed.Previous with
        | None -> parsed.Values
        | Some last -> 
            parsed.Values @ [last, NoValue]
    Map.ofList values

let private parsedParams = lazy(System.Environment.GetCommandLineArgs() |> parseCLIParams)

let internal explicitGetCLIParam args name : string option =
    match Map.tryFind (cleanKey name) args with
    | None -> None
    | Some opt -> 
        match opt with
        | String value -> Some value 
        | NoValue -> Some ""

let internal explicitGetCLIFlag args name : bool option =
    match Map.tryFind (cleanKey name) args with
    | None -> None
    | Some opt -> 
        match opt with
        | String value -> 
            Some (System.Boolean.Parse value)
        | NoValue -> Some true

/// Get a Command Line parametters value. Some if specified, None if not.
let getCLIParam name =
    explicitGetCLIParam parsedParams.Value name

/// Get a Command Line parametters value and convert it to a string.
let getCLIFlag name : bool option =
    explicitGetCLIFlag parsedParams.Value name