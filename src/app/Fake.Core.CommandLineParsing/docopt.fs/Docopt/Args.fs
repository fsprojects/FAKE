namespace Fake.Core.CommandLineParsing

type ParseResult =
  | NoResult
  | Flag
  | Flags of int
  | Argument of string
  | Arguments of string list

type ResultMap = Map<string, ParseResult>

module ParseResult =
  let getFlagCount flag (map:ResultMap) =
    match Map.tryFind flag map with
    | Some (NoResult) -> 0
    | Some (Flag) -> 1
    | Some (Flags n) -> n
    | Some (Argument _) -> 1
    | Some (Arguments l) -> l.Length
    | None -> 0
  let hasFlag flag (map:ResultMap) =
    getFlagCount flag map > 0

  let tryGetArgument flag (map:ResultMap) =
    match Map.tryFind flag map with
    | Some (Argument arg) -> Some arg
    | Some (Arguments args) -> failwithf "Expected argument %s only a single time, but got %A" flag args
    | _ -> None

  let tryGetArguments flag (map:ResultMap) =
    match Map.tryFind flag map with
    | Some (Argument arg) -> Some [arg]
    | Some (Arguments args) -> Some args
    | _ -> None
