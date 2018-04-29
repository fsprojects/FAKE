namespace Fake.Core

type DocoptResult =
  | NoResult
  | Flag
  | Flags of int
  | Argument of string
  | Arguments of string list
  override x.ToString() =
    match x with
    | NoResult -> "NoResult"
    | Flag -> "Flag"
    | Flags f -> sprintf "Flags(%d)" f
    | Argument arg -> sprintf "Argument(%s)" arg
    | Arguments args -> sprintf "Arguments([%s])" (System.String.Join(";", args :> _ seq))

type DocoptMap = Map<string, DocoptResult>

[<RequireQualifiedAccess>]
module DocoptResult =
  let getFlagCount flag (map:DocoptMap) =
    match Map.tryFind flag map with
    | Some (NoResult) -> 0
    | Some (Flag) -> 1
    | Some (Flags n) -> n
    | Some (Argument _) -> 1
    | Some (Arguments l) -> l.Length
    | None -> 0
  let hasFlag flag (map:DocoptMap) =
    getFlagCount flag map > 0

  let tryGetArgument flag (map:DocoptMap) =
    match Map.tryFind flag map with
    | Some (Argument arg)
    | Some (Arguments [arg]) -> Some arg
    | Some (Arguments args) -> failwithf "Expected argument %s only a single time, but got %A" flag args
    | _ -> None

  let tryGetArguments flag (map:DocoptMap) =
    match Map.tryFind flag map with
    | Some (Argument arg) -> Some [arg]
    | Some (Arguments args) -> Some args
    | _ -> None
