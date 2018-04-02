open System
open Fake.Runtime
open Fake.Runtime.Environment
open Fake.Runtime.String
open Fake.Runtime.Trace
open Fake.Runtime.ScriptRunner
open Fake.Runtime.HashGeneration
open Fake.Runtime.CoreCache
open Fake.Runtime.FakeRuntime
open System.IO
open Fake.Core.CommandLineParsing
open Paket.FolderScanner

let printVersion() =
    traceFAKE "%s" fakeVersionStr

let printFakePath() =
    traceFAKE "FakePath: %s" fakePath


let printEnvironment cmdArgs args =
    printVersion()
    printFakePath()

    if cmdArgs |> Array.length > 1 then
        traceFAKE "FAKE Arguments:"
        args 
          |> Seq.map fst
          |> Seq.iter (tracefn "%A")

    log ""
    //traceFAKE "FSI-Path: %s" fsiPath
    //traceFAKE "MSBuild-Path: %s" msBuildExe

let containsParam (param:string) = Seq.map (fun (s:string) -> s.ToLower()) >> Seq.exists ((=) (param.ToLower()))

let paramIsHelp param = containsParam param ["help"; "?"; "/?"; "-h"; "--help"; "/h"; "/help"]

let buildScripts = System.IO.Directory.EnumerateFiles(System.IO.Directory.GetCurrentDirectory(), "*.fsx") |> Seq.toList

// http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
let splitBy f (s:string) =
  seq {
    let mutable nextPiece = 0
    for c, i in s |> Seq.mapi (fun i c -> c, i) do
      if f c then
        yield s.Substring(nextPiece, i - nextPiece)
        nextPiece <- i + 1
    yield s.Substring(nextPiece)
  }
let trim (s:string) = s.Trim()
let trimMatchingQuotes quote (s:string) =
  if s.Length >= 2 && s.[0] = quote && s.[s.Length - 1] = quote
  then s.Substring(1, s.Length - 2)
  else s

let splitCommandLine s =
  let mutable inQuotes = false
  s
  |> splitBy (fun c -> if c = '\"' then inQuotes <- not inQuotes
                       not inQuotes && c = ' ')
  |> Seq.map (trim >> trimMatchingQuotes '\"')
  |> Seq.filter (System.String.IsNullOrEmpty >> not)


type RunArguments = {
   Script : string option
   ScriptArguments : string list
   //Target : string option
   FsiArgLine : string list
   //EnvironmentVariables : (string * string) list
   Debug : bool
   //SingleTarget : bool
   NoCache : bool
   VerboseLevel : VerboseLevel
   IsBuild : bool // Did the user call `fake build` or `fake run`?
}

let runOrBuild (args : RunArguments) =
  if args.VerboseLevel.PrintVerbose then
    Trace.log (sprintf "runOrBuild (%A)" args)
  if args.Debug then
    Diagnostics.Debugger.Launch() |> ignore
    Diagnostics.Debugger.Break() |> ignore

  try
    if args.VerboseLevel.PrintVerbose then printVersion()

    //Maybe log.
    //match fakeArgs.TryGetResult <@ Cli.LogFile @> with
    //| Some(path) -> addXmlListener path
    //| None -> ()

    //Get our fsiargs from somewhere!
    let fsiArgs = args.FsiArgLine |> Seq.collect (splitCommandLine) |> Seq.toArray
    let s = args.Script
    let additionalArgs, scriptFile, scriptArgs = 
        match
            s,
            fsiArgs |> Array.toList,
            List.isEmpty buildScripts with

        //TODO check for presence of --fsiargs with no args?  Make attribute for UAP?

        //Use --fsiargs approach.
        | Some(script), fsArgs, _ ->
           match fsiArgs |> Array.tryFindIndex (fun arg -> arg.StartsWith("-") = false) with
           | Some(i) ->
              let fsxPath = fsiArgs.[i]
              if script <> fsxPath then traceFAKE "script specified via fsiargs '%s' does not equal the one we run '%s'." fsxPath script 
              let fsiOpts = if i > 0 then fsiArgs.[0..i-1] else [||]
              let scriptArgs = if fsiArgs.Length > (i+1) then fsiArgs.[i+1..] else [||]
              fsiOpts |> List.ofArray, script, scriptArgs |> List.ofArray
           | None ->
              fsArgs, script, []
        | None, x::xs, _ ->
            let args = x::xs |> Array.ofList
            //Find first arg that does not start with - (as these are fsi options that precede the fsx).
            match args |> Array.tryFindIndex (fun arg -> arg.StartsWith("-") = false) with
            | Some(i) ->
                let fsxPath = args.[i]
                if fsxPath.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase) then
                    let fsiOpts = if i > 0 then args.[0..i-1] else [||]
                    let scriptArgs = if args.Length > (i+1) then args.[i+1..] else [||]
                    fsiOpts |> List.ofArray, fsxPath, scriptArgs |> List.ofArray
                else 
                  let msg = sprintf "Expected argument %s to be the build script path, but it does not have the .fsx extension." fsxPath
                  failwithf "Unable to parse --fsiargs.  %s" msg
            | None ->
                  let msg = "Unable to locate the build script path."
                  failwithf "Unable to parse --fsiargs.  %s" msg
        //No explicit script, but have in working directory.
        | None, [], false -> 
          match buildScripts |> List.tryFind (fun f -> Path.GetFileName f = "build.fsx") with
          | Some find -> [], find, []
          | None -> [], List.head buildScripts, []

        //Noooo script anywhere!
        | None, [], true -> failwith "Build script not specified on command line, in fsi args or found in working directory."

    let useCache = not args.NoCache
    if not (FakeRuntime.prepareAndRunScript args.VerboseLevel additionalArgs scriptFile args.ScriptArguments useCache) then false
    else 
        if args.VerboseLevel.PrintVerbose then log "Ready."
        true
  with
  | exn ->
      use logPaket =
        // Required when 'silent' because we use paket API for error printing
        if args.VerboseLevel = Trace.Silent then
          Paket.Logging.event.Publish
          |> Observable.subscribe Paket.Logging.traceToConsole
        else { new IDisposable with member __.Dispose () = () }      
      traceError "Script failed"
      if Environment.GetEnvironmentVariable "FAKE_DETAILED_ERRORS" = "true" then
          Paket.Logging.printErrorExt true true true exn
      else Paket.Logging.printErrorExt args.VerboseLevel.PrintVerbose args.VerboseLevel.PrintVerbose false exn

      //let isKnownException = exn :? FAKEException
      //if not isKnownException then
      //    sendTeamCityError exn.Message

      false

let handleCli (results:Map<string, ParseResult>) =

  let mutable didSomething = false
  let mutable exitCode = 0
  let mutable runarg = None
  let verbLevel =
    ParseResult.getFlagCount "--verbose" results
  let isSilent =
    ParseResult.hasFlag "--silent" results
  let verboseLevel =
    match isSilent, verbLevel with
    | true, _ -> VerboseLevel.Silent
    | _, 0 -> VerboseLevel.Normal
    | _, 1 -> VerboseLevel.Verbose
    | _ -> VerboseLevel.VerbosePaket  
  if verboseLevel.PrintPaket then
    Paket.Logging.verbose <- true
    Paket.Logging.verboseWarnings <- true
  Paket.Utils.autoAnswer <- Some true
  use consoleTrace =
    // When silent we don't want Paket output
    if verboseLevel.PrintNormal then
      Paket.Logging.event.Publish
      |> Observable.subscribe Paket.Logging.traceToConsole
    else
      { new System.IDisposable with 
          member __.Dispose() = () }
  if ParseResult.hasFlag "--version" results then
    didSomething <- true
    printVersion()
    printFakePath()

  if ParseResult.hasFlag "--help" results || ParseResult.hasFlag "--help" results then
    didSomething <- true
    printf "%s" Cli.fakeUsage

  let isRun = ParseResult.hasFlag "run" results
  let isBuild = ParseResult.hasFlag "build" results
  if isRun || isBuild then
    let arg = {
       Script =
          if isRun then ParseResult.tryGetArgument "<script.fsx>" results
          else ParseResult.tryGetArgument "--script" results
       ScriptArguments =
          match ParseResult.tryGetArguments "<scriptargs>" results with
          | Some args -> args
          | None -> []
       FsiArgLine =
         match ParseResult.tryGetArguments "<script.fsx>" results with
         | Some args -> args
         | None -> []

       Debug = ParseResult.hasFlag "--debug" results
       NoCache = ParseResult.hasFlag "--nocache" results
       VerboseLevel = verboseLevel
       IsBuild = not isRun // Did the user call `fake build` or `fake run`?
    }
    let success = runOrBuild arg
    if not success then exitCode <- 1
  else
    if not didSomething then
      exitCode <- 1
      eprintfn ("Please specify what you want to do!")
      printfn "%s" Cli.fakeUsage

  exitCode

[<EntryPoint>]
let main (args:string[]) =
  let resolution = Environment.GetEnvironmentVariable ("PAKET_DISABLE_RUNTIME_RESOLUTION")
  if System.String.IsNullOrEmpty resolution then
    Environment.SetEnvironmentVariable ("PAKET_DISABLE_RUNTIME_RESOLUTION", "true")

  let mutable exitCode = 0
  let encoding = Console.OutputEncoding
  try
    let parser = Docopt(Cli.fakeUsage)
    let results = parser.Parse(args)
    exitCode <- handleCli results
  with
  | :? ArgvException as e ->
    printfn "Usage error: %s" e.Message
    printfn "%s" Cli.fakeUsage
    exitCode <- 1
  Console.OutputEncoding <- encoding
#if !NETSTANDARD1_6
  //if !TargetHelper.ExitCode.exitCode <> 0 then exit !TargetHelper.ExitCode.exitCode
  if Environment.ExitCode <> 0 then exitCode <- Environment.ExitCode
#endif
  exitCode
