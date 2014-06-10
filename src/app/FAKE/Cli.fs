///
/// New Command line interface for FAKE that utilises UnionArgParser.
///
[<RequireQualifiedAccessAttribute>]
module Cli

open System
open Nessos.UnionArgParser

type FakeArg = 
    | [<AltCommandLine("-ev")>] EnvVar of string * string
    | [<AltCommandLine("-ef")>] EnvFlag of string
    | [<AltCommandLine("-lf")>] LogFile of string
    | [<AltCommandLine("-pd")>] PrintDetails
    | [<AltCommandLine("-v")>] Version
    | [<Rest>] FsiArgs of string
    | [<Rest>] Boot of string
    interface IArgParserTemplate with
        member x.Usage = 
            match x with
            | EnvVar _ -> "Set environment variable <name> <value>. Supports multiple."
            | EnvFlag _ -> "Set environment variable flag <name> 'true'. Supports multiple."
            | LogFile _ -> "Build output log file path."
            | PrintDetails _ -> "Print details of FAKE's activity."
            | FsiArgs _ -> "Pass args after this switch to FSI when running the build script."
            | Version _ -> "Print FAKE version information."
            | Boot _ -> "TBC"

/// Return the parsed FAKE args or the parse exception.
let parsedArgsOrEx args = 
    try
        let args = args |> Seq.skip 1 |> Array.ofSeq
        let parser = UnionArgParser<FakeArg>()
        Choice1Of2(parser.Parse(args))
    with | ex -> Choice2Of2(ex)

/// Prints the FAKE argument usage.
let printUsage () =
    printfn @"
    fake.exe [<scriptPath>] [<targetName>] [switches]

    Switches:
    %s" (UnionArgParser<FakeArg>().Usage())
    
type Args = { Script: string option; Target: string option; Rest: string [] }

/// Parses the positional args and provides the remaining tail args.
let parsePositionalArgs (args:string []) = 

    //Support this usage.
    //fake.exe <script>.fsx <targetName> [switches]
    //fake.exe <targetName> [switches]
    let maybeScript, maybeTarget = 
        if args.Length > 1 then
            let isScriptArg (arg:string) = arg.EndsWith(".fsx", StringComparison.InvariantCultureIgnoreCase)
            //Don't consider it the positional target if looks like switch or old kvp arg.
            let isTargetArg (arg:string) = not <| (arg.StartsWith("-") || arg.Contains("="))
            let arg1 = args.[1]
            let maybeScriptOrTarget = 
                if isScriptArg arg1 then Some(Choice1Of2(arg1))
                elif isTargetArg arg1 then Some(Choice2Of2(arg1))
                else None
            match maybeScriptOrTarget with
            | Some(Choice1Of2(script)) when args.Length > 2 ->
                let arg2 = args.[2]
                if isTargetArg arg2 then Some(script), Some(arg2)
                else Some(script), None
            | Some(Choice1Of2(script)) -> Some(script), None
            | Some(Choice2Of2(target)) -> None, Some(target)
            | None -> None, None
        else None, None

    let restOfArgs = 
        let tailIndex = 
            match maybeScript, maybeTarget with
            | Some(_), Some(_) -> 3 | Some(_), None | None, Some(_) -> 2 | None, None -> 1
        if args.Length-1 >= tailIndex
        then Array.concat (seq { yield [| args.[0] |]; yield args.[tailIndex..] })
        else [| args.[0] |]

    { Script = maybeScript; Target = maybeTarget; Rest = restOfArgs }
