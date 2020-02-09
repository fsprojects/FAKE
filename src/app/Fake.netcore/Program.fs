open System
open Fake.Runtime
open Fake.Runtime.Environment
open Fake.Runtime.String
open Fake.Runtime.Trace
open Fake.Runtime.ScriptRunner
open Fake.Runtime.CoreCache
open Fake.Runtime.FakeRuntime
open System.IO
open Fake.Core
open Paket.FolderScanner

let sw = System.Diagnostics.Stopwatch.StartNew()

let printVersion() =
    traceFAKE "%s (this line is written to standard error, see https://github.com/fsharp/FAKE/issues/2066)" fakeVersionStr

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
   RestoreOnlyGroup : bool
   VerboseLevel : VerboseLevel
   IsBuild : bool // Did the user call `fake build` or `fake run`?
}

let reportExn (verb:VerboseLevel) exn =
  use logPaket =
    // Required when 'silent' because we use paket API for error printing
    if verb = Trace.Silent then
      Paket.Logging.event.Publish
      |> Observable.subscribe Paket.Logging.traceToConsole
    else { new IDisposable with member __.Dispose () = () }      
  if Environment.GetEnvironmentVariable "FAKE_DETAILED_ERRORS" = "true" then
      Paket.Logging.printErrorExt true true true exn
  else Paket.Logging.printErrorExt verb.PrintVerbose verb.PrintVerbose verb.PrintVerbose exn

let runOrBuild (args : RunArguments) =
  if args.VerboseLevel.PrintVerbose then
    Trace.log (sprintf "runOrBuild (%A)" args)
  if args.Debug then
    Diagnostics.Debugger.Launch() |> ignore
    Diagnostics.Debugger.Break() |> ignore

  // This is to ensure we always write these to stderr (even when we are in silent mode).
  let forceWrite = Trace.defaultConsoleTraceListener.Write
  try
    if args.VerboseLevel.PrintVerbose then printVersion()

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
           match fsiArgs |> Array.tryFindIndex (fun arg -> not (arg.StartsWith "-")) with
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
            match args |> Array.tryFindIndex (fun arg -> not (arg.StartsWith "-")) with
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

    if scriptArgs <> [] then
        traceFAKE "FAKE 5 ignores script args given via --fsiargs, given was %A" scriptArgs
    let useCache = not args.NoCache
    try
      let config = FakeRuntime.createConfigSimple args.VerboseLevel additionalArgs scriptFile args.ScriptArguments useCache args.RestoreOnlyGroup
      let prepared = FakeRuntime.prepareFakeScript config
      let runResult, cache, context = FakeRuntime.runScript prepared
      let result =
        match runResult with
        | Runners.RunResult.SuccessRun warnings ->
          if warnings <> "" then
            traceFAKE "%O" warnings
          if args.VerboseLevel.PrintVerbose then log "Ready."
          true
        | Runners.RunResult.CompilationError err ->
          let indentString num (str:string) =
            let indentString = String('\t', num)
            let splitMsg = str.Split([|"\r\n"; "\n"|], StringSplitOptions.None)
            indentString + String.Join(sprintf "%s%s" Environment.NewLine indentString, splitMsg)
          if args.VerboseLevel.PrintVerbose then
            // in case stderr is not redirected
            TraceMessage("Script is not valid, see standard error for details.", true) |> forceWrite

          ErrorMessage "Script is not valid:" |> forceWrite
          ErrorMessage (indentString 1 err.FormattedErrors) |> forceWrite
          false
        | Runners.RunResult.RuntimeError err ->
          if args.VerboseLevel.PrintVerbose then
            // in case stderr is not redirected
            TraceMessage ("Script reported an error, see standard error for details.", true) |> forceWrite
          ErrorMessage "Script reported an error:" |> forceWrite
          reportExn args.VerboseLevel err
          false
      if args.VerboseLevel.PrintNormal && Environment.GetEnvironmentVariable "FAKE_DISABLE_HINTS" <> "true" then
        for hint in FakeRuntimeHints.retrieveHints prepared context runResult cache do
          if hint.Important then
            traceFAKE "Warning: %s" hint.Text
          else
            tracefn "Hint: %s" hint.Text
      result        
    finally
      sw.Stop()
      if args.VerboseLevel.PrintNormal then
        Fake.Profile.print true sw.Elapsed
  with
  | exn ->
      if args.VerboseLevel.PrintVerbose then
        // in case stderr is not redirected
        TraceMessage ("There was a problem while setting up the environment, see standard error for details.", true) |> forceWrite
      ErrorMessage "There was a problem while setting up the environment:" |> forceWrite
      reportExn args.VerboseLevel exn
      
      tracefn "Hint: %s" "If you just upgraded the fake-runner you can try to remove the .fake directory and try again."
      false

type CliAction =
  | ShowVersion
  | RunOrBuild of RunArguments
  | ShowHelp
  | InvalidUsage of string

let private initPaket (verboseLevel:VerboseLevel) =
  if verboseLevel.PrintPaket then
    Paket.Logging.verbose <- true
    Paket.Logging.verboseWarnings <- true
  Paket.Utils.autoAnswer <- Some true
  // When silent we don't want Paket output
  if verboseLevel.PrintNormal then
    Paket.Logging.event.Publish
    |> Observable.subscribe Paket.Logging.traceToConsole
  else
    { new System.IDisposable with 
        member __.Dispose() = () }
      
exception PaketInitException of Exception

let handleAction (verboseLevel:VerboseLevel) (action:CliAction) =

  use consoleTrace =
    try initPaket verboseLevel
    with e ->
      // Hard to debug, see https://github.com/isaacabraham/vsts-fsharp/issues/33
      raise (PaketInitException e)

  match action with
  | ShowVersion ->
    printVersion()
    printFakePath()
    traceFAKE "Paket.Core: %s" Fake.Runtime.FakeRuntimeHints.paketVersion
    0
  | ShowHelp ->
    printf "%s" Cli.fakeArgsHint
    printf "%s" Cli.fakeUsage
    printf "%s" Cli.fakeAdditionalHelp
    0
  | InvalidUsage str ->
    eprintfn "%s" str
    printf "%s" Cli.fakeArgsHint
    printf "%s" Cli.fakeUsage
    printf "%s" Cli.fakeAdditionalHelp
    1
  | RunOrBuild arg ->
    let success = runOrBuild arg
    if not success then 1 else 0

let parseAction (results:DocoptMap) =
  let verbLevel =
    DocoptResult.getFlagCount "--verbose" results
  let isSilent =
    DocoptResult.hasFlag "--silent" results
  let verboseLevel =
    match isSilent, verbLevel with
    | true, _ -> VerboseLevel.Silent
    | _, 0 -> VerboseLevel.Normal
    | _, 1 -> VerboseLevel.Verbose
    | _ -> VerboseLevel.VerbosePaket
  let isRun = DocoptResult.hasFlag "run" results
  let isBuild = DocoptResult.hasFlag "build" results
  verboseLevel, 
  if DocoptResult.hasFlag "--version" results then
    ShowVersion
  elif DocoptResult.hasFlag "--help" results || DocoptResult.hasFlag "--help" results then
    ShowHelp
  elif isRun || isBuild then
    let arg = {
       Script =
          if isRun then DocoptResult.tryGetArgument "<script.fsx>" results
          else DocoptResult.tryGetArgument "--script" results
       ScriptArguments =
          match DocoptResult.tryGetArguments "<scriptargs>" results with
          | Some args -> args
          | None -> []
       FsiArgLine =
         match DocoptResult.tryGetArguments "--fsiargs" results with
         | Some args -> args
         | None -> []

       Debug = DocoptResult.hasFlag "--debug" results
       NoCache = DocoptResult.hasFlag "--nocache" results
       RestoreOnlyGroup = DocoptResult.hasFlag "--partial-restore" results || Environment.GetEnvironmentVariable ("FAKE_PARTIAL_RESTORE") = "true"
       VerboseLevel = verboseLevel
       IsBuild = not isRun // Did the user call `fake build` or `fake run`?
    }

    RunOrBuild arg
  else
    InvalidUsage "Please specify what you want to do!"

[<EntryPoint>]
let main (args:string[]) =
  let resolution = Environment.GetEnvironmentVariable ("PAKET_DISABLE_RUNTIME_RESOLUTION")
  if System.String.IsNullOrEmpty resolution then
    Environment.SetEnvironmentVariable ("PAKET_DISABLE_RUNTIME_RESOLUTION", "true")

  // Immediatly start to build up some performance maps in the background
  let paketPerfTask =
      async {
        try
            Paket.Constants.NuGetCacheFolder |> ignore // make sure to call ..cctor
            do! Paket.PublicAPI.PreCalculateMaps() |> Async.AwaitTask
        with e -> eprintfn "Building paket performance maps failed: %O" e
      }
      |> Async.StartAsTask
      
  let mutable exitCode = 0
  let encoding = Console.OutputEncoding
  try
    let verbLevel, results =
      use __ = Fake.Profile.startCategory Fake.Profile.Category.Cli
      let parser = Docopt(Cli.fakeUsage)
      let rawResults = parser.Parse(args)
      parseAction rawResults
    exitCode <- handleAction verbLevel results
  with
  | PaketInitException exn ->
    printfn "Error while initializing Paket.Core (please report this to Paket):"
    reportExn VerboseLevel.VerbosePaket exn
    exitCode <- 1
  | exn ->
    printfn "Error while parsing command line, usage is:"
    printf "%s" Cli.fakeArgsHint
    printf "%s" Cli.fakeUsage 
    printf "%s" Cli.fakeAdditionalHelp

    // need to enable this as otherwise no proper error is reported
    use consoleTrace =
      // When silent we don't want Paket output
      Paket.Logging.event.Publish
      |> Observable.subscribe Paket.Logging.traceToConsole
    reportExn VerboseLevel.Normal exn
    exitCode <- 1
  try
    Console.OutputEncoding <- encoding
  with e ->
    // See https://github.com/fsharp/FAKE/issues/2406
    printfn "(Warning) Error while resetting OutputEncoding to '%O':" encoding
    reportExn VerboseLevel.Normal e
  try
    // Potential fix for https://github.com/fsharp/FAKE/issues/2173
    Console.ResetColor()
  with e ->
    // See https://github.com/fsharp/FAKE/issues/2406
    printfn "(Warning) Error while Console.ResetColor:"
    reportExn VerboseLevel.Normal e
#if !NETSTANDARD1_6
  //if !TargetHelper.ExitCode.exitCode <> 0 then exit !TargetHelper.ExitCode.exitCode
  if Environment.ExitCode <> 0 then exitCode <- Environment.ExitCode
#endif
  exitCode
