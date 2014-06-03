///
/// New Command line interface for FAKE that utilises UnionArgParser.
///
[<RequireQualifiedAccessAttribute>]
module Cli

open Nessos.UnionArgParser

type FakeArg = 
    | [<AltCommandLine("-s")>] Script of string
    | [<AltCommandLine("-t")>] Target of string
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
            | Script _ -> "Build script file path." 
            | Target _ -> "Target to run."
            | EnvVar _ -> "One or more environment variables to set."
            | EnvFlag _ -> "One or more environment variable names that will be set to 'true'."
            | LogFile _ -> "Path for build output log file."
            | PrintDetails _ -> "Print details of FAKE's activity."
            | FsiArgs _ -> "Pass all args following this switch to FSI when running the build script.  MUST BE LAST!"
            | Version _ -> "Print the FAKE version information."
            | Boot _ -> "TBC"

/// Return the parsed FAKE args or the parse exception.
let parsedArgsOrEx args = 
    try
        let args = args |> Seq.skip 1 |> Array.ofSeq
        let parser = UnionArgParser<FakeArg>()
        Choice1Of2(parser.Parse(args))
    with | ex -> Choice2Of2(ex)

/// Prints the FAKE argument usage.
let printUsage () = printfn "%s" (UnionArgParser<FakeArg>().Usage())
    
