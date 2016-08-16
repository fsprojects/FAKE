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
    traceFAKE "FakePath: %s" fakePath
    traceFAKE "%s" fakeVersionStr


let printEnvironment cmdArgs args =
    printVersion()

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


let handleCli (results:ParseResults<Cli.FakeArgs>) =

  let mutable didSomething = false
  let mutable exitCode = 0
  let printDetails = results.Contains <@ Cli.FakeArgs.Verbose @>

  if printDetails then
    Paket.Logging.verbose <- true
  Paket.Utils.autoAnswer <- Some true
  use consoleTrace = Paket.Logging.event.Publish |> Observable.subscribe Paket.Logging.traceToConsole

  if results.Contains <@ Cli.FakeArgs.Version @> then
    didSomething <- true
    printVersion()
  results.IterResult (<@ Cli.FakeArgs.Run @>, fun runArgs ->
    didSomething <- true
    if runArgs.Contains <@ Cli.RunArgs.Debug @> then
      Diagnostics.Debugger.Launch() |> ignore
      Diagnostics.Debugger.Break() |> ignore

    try
      //AutoCloseXmlWriter <- true
      //let cmdArgs = System.Environment.GetCommandLineArgs()
      //match isBoot with
      ////Boot.
      //| true ->
      //    let handler = Boot.HandlerForArgs bootArgs//Could be List.empty, but let Boot handle this.
      //    handler.Interact()
      //
      ////Try and run a build script! 
      //| false ->

      if printDetails then printVersion()

      //Maybe log.
      //match fakeArgs.TryGetResult <@ Cli.LogFile @> with
      //| Some(path) -> addXmlListener path
      //| None -> ()

      //Get our fsiargs from somewhere!
      let fsiArgLine = if runArgs.Contains <@ Cli.RunArgs.FsiArgs @> then runArgs.GetResult <@ Cli.RunArgs.FsiArgs @> else ""
      let s = if runArgs.Contains <@ Cli.RunArgs.Script @> then Some (runArgs.GetResult <@ Cli.RunArgs.Script @>)  else None
      let additionalArgs, scriptFile, scriptArgs = 
          match
              splitCommandLine fsiArgLine |> Seq.toList,
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
          | [], None, false -> [], List.head buildScripts, []

          //Noooo script anywhere!
          | [], None, true -> failwith "Build script not specified on command line, in fsi args or found in working directory."

      //Combine the key value pair vars and the flag vars.
      let envVars =
          seq {
            yield! 
              runArgs.GetResults(<@ Cli.RunArgs.EnvironmentVariable @>)
              //|> Seq.map (fun s -> let split = s.Split(':') in split.[0], split.[1])
            if runArgs.Contains <@ Cli.RunArgs.SingleTarget @> then yield "single-target", "true"
            if runArgs.Contains <@ Cli.RunArgs.Target @> then yield "target", runArgs.GetResult <@ Cli.RunArgs.Target @>
            yield "fsiargs-buildscriptargs", String.Join(" ", scriptArgs)
          }

      let useCache = not (runArgs.Contains <@ Cli.RunArgs.NoCache @>)
      if not (FakeRuntime.prepareAndRunScript printDetails additionalArgs scriptFile envVars useCache) then exitCode <- 1
      else if printDetails then log "Ready."
    with
    | exn ->
        if printDetails then
            sprintf "Build failed.\nError:\n%O" exn
            |> traceError
        else
            if exn.InnerException <> null then
                sprintf "Build failed.\nError:\n%s\nInnerException:\n%s" exn.Message exn.InnerException.Message
                |> traceError
                //printUsage()
            else
                sprintf "Build failed.\nError:\n%s" exn.Message
                |> traceError
                //printUsage()

        //let isKnownException = exn :? FAKEException
        //if not isKnownException then
        //    sendTeamCityError exn.Message

        exitCode <- 1

    //killAllCreatedProcesses()
  )

  if not didSomething then
    results.Raise ("Please specify what you want to do!", showUsage = true)

  exitCode

[<EntryPoint>]
let main (args:string[]) =
  let mutable exitCode = 0
  let parser = ArgumentParser.Create<Cli.FakeArgs>("fake")
  try
    let results = parser.Parse(args)
    exitCode <- handleCli results
  with
  | :? ArguParseException as e ->
    printfn "%s" e.Message
    exitCode <- 1
#if !NETSTANDARD1_6
  //if !TargetHelper.ExitCode.exitCode <> 0 then exit !TargetHelper.ExitCode.exitCode
  if Environment.ExitCode <> 0 then exitCode <- Environment.ExitCode
#endif
  exitCode
