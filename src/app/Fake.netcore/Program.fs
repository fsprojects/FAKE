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
open Argu

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
   Target : string option
   FsiArgLine : string
   EnvironmentVariables : (string * string) list
   Debug : bool
   SingleTarget : bool
   NoCache : bool
   PrintDetails : bool
   IsBuild : bool // Did the user call `fake build` or `fake run`?
}

let runOrBuild (args : RunArguments) =
  if args.Debug then
    Diagnostics.Debugger.Launch() |> ignore
    Diagnostics.Debugger.Break() |> ignore

  try
    if args.PrintDetails then printVersion()

    //Maybe log.
    //match fakeArgs.TryGetResult <@ Cli.LogFile @> with
    //| Some(path) -> addXmlListener path
    //| None -> ()

    //Get our fsiargs from somewhere!
    
    let s = args.Script
    let additionalArgs, scriptFile, scriptArgs = 
        match
            splitCommandLine args.FsiArgLine |> Seq.toList,
            s,
            List.isEmpty buildScripts with

        //TODO check for presence of --fsiargs with no args?  Make attribute for UAP?

        //Use --fsiargs approach.
        | x::xs, _, _ ->
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
        //Script path is specified.
        | [], Some(script), _ -> [], script, []

        //No explicit script, but have in working directory.
        | [], None, false -> 
          match buildScripts |> List.tryFind (fun f -> Path.GetFileName f = "build.fsx") with
          | Some find -> [], find, []
          | None -> [], List.head buildScripts, []

        //Noooo script anywhere!
        | [], None, true -> failwith "Build script not specified on command line, in fsi args or found in working directory."

    //Combine the key value pair vars and the flag vars.
    let envVars =
        seq {
          yield! args.EnvironmentVariables
            //|> Seq.map (fun s -> let split = s.Split(':') in split.[0], split.[1])
          if args.SingleTarget then yield "single-target", "true"
          if args.Target.IsSome then yield "target", args.Target.Value
          yield "fsiargs-buildscriptargs", String.Join(" ", scriptArgs)
        }

    let useCache = not args.NoCache
    if not (FakeRuntime.prepareAndRunScript args.PrintDetails additionalArgs scriptFile envVars useCache) then false
    else 
        if args.PrintDetails then log "Ready."
        true
  with
  | exn ->
      traceError "Script failed with"
      if Environment.GetEnvironmentVariable "FAKE_DETAILED_ERRORS" = "true" then
          Paket.Logging.printErrorExt true true false exn
      else Paket.Logging.printErrorExt args.PrintDetails args.PrintDetails false exn

      //let isKnownException = exn :? FAKEException
      //if not isKnownException then
      //    sendTeamCityError exn.Message

      false

let handleCli (results:ParseResults<Cli.FakeArgs>) =

  let mutable didSomething = false
  let mutable exitCode = 0
  let mutable runarg = None
  let verbLevel = (results.GetResults <@ Cli.FakeArgs.Verbose @>) |> List.length
  let printDetails = verbLevel > 0
  if verbLevel > 1 then
    Paket.Logging.verbose <- true
    Paket.Logging.verboseWarnings <- true
  Paket.Utils.autoAnswer <- Some true
  use consoleTrace = Paket.Logging.event.Publish |> Observable.subscribe Paket.Logging.traceToConsole

  if results.Contains <@ Cli.FakeArgs.Version @> then
    didSomething <- true
    printVersion()
    printFakePath()
    
  results.IterResult (<@ Cli.FakeArgs.Run @>, fun runArgs ->
    runarg <- Some {
       Script = if runArgs.Contains <@ Cli.RunArgs.Script @> then Some (runArgs.GetResult <@ Cli.RunArgs.Script @>) else None
       Target = if runArgs.Contains <@ Cli.RunArgs.Target @> then Some (runArgs.GetResult <@ Cli.RunArgs.Target @>) else None
       FsiArgLine = if runArgs.Contains <@ Cli.RunArgs.FsiArgs @> then runArgs.GetResult <@ Cli.RunArgs.FsiArgs @> else ""
       EnvironmentVariables = runArgs.GetResults(<@ Cli.RunArgs.EnvironmentVariable @>)
       Debug = runArgs.Contains <@ Cli.RunArgs.Debug @>
       SingleTarget =  runArgs.Contains <@ Cli.RunArgs.SingleTarget @>
       NoCache = runArgs.Contains <@ Cli.RunArgs.NoCache @>
       PrintDetails = printDetails
       IsBuild = false // Did the user call `fake build` or `fake run`?
    }
  )

  results.IterResult (<@ Cli.FakeArgs.Build @>, fun buildArg ->
    if runarg.IsSome then failwithf "`fake run` was already executed, executing `fake build` at the same time is impossible!"
    runarg <- Some {
       Script = if buildArg.Contains <@ Cli.BuildArgs.Script @> then Some (buildArg.GetResult <@ Cli.BuildArgs.Script @>) else None
       Target = if buildArg.Contains <@ Cli.BuildArgs.Target @> then Some (buildArg.GetResult <@ Cli.BuildArgs.Target @>) else None
       FsiArgLine = if buildArg.Contains <@ Cli.BuildArgs.FsiArgs @> then buildArg.GetResult <@ Cli.BuildArgs.FsiArgs @> else ""
       EnvironmentVariables = buildArg.GetResults(<@ Cli.BuildArgs.EnvironmentVariable @>)
       Debug = buildArg.Contains <@ Cli.BuildArgs.Debug @>
       SingleTarget =  buildArg.Contains <@ Cli.BuildArgs.SingleTarget @>
       NoCache = buildArg.Contains <@ Cli.BuildArgs.NoCache @>
       PrintDetails = printDetails
       IsBuild = true // Did the user call `fake build` or `fake run`?
    }
  )

  match runarg with
  | Some arg ->
    let success = runOrBuild arg
    if not success then exitCode <- 1
  | None when not didSomething ->
    results.Raise ("Please specify what you want to do!", showUsage = true)
  | None -> ()  

  exitCode

[<EntryPoint>]
let main (args:string[]) =
  let resolution = Environment.GetEnvironmentVariable ("PAKET_DISABLE_RUNTIME_RESOLUTION")
  if System.String.IsNullOrEmpty resolution then
    Environment.SetEnvironmentVariable ("PAKET_DISABLE_RUNTIME_RESOLUTION", "true")

  let mutable exitCode = 0
  let encoding = Console.OutputEncoding
  let parser = ArgumentParser.Create<Cli.FakeArgs>("fake")
  try
    let results = parser.Parse(args)
    exitCode <- handleCli results
  with
  | :? ArguParseException as e ->
    printfn "%s" e.Message
    exitCode <- 1
  Console.OutputEncoding <- encoding
#if !NETSTANDARD1_6
  //if !TargetHelper.ExitCode.exitCode <> 0 then exit !TargetHelper.ExitCode.exitCode
  if Environment.ExitCode <> 0 then exitCode <- Environment.ExitCode
#endif
  exitCode
