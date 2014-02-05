open System
open Fake
open System.IO
open Nessos.UnionArgParser

let printVersion() =
    traceFAKE "FakePath: %s" fakePath
    traceFAKE "%s" fakeVersionStr

let printUsage () =
    printfn "-------------------"
    printfn " FAKE usage"
    printfn "-------------------"
    Cli.printUsage ()
    printfn "--------------------"
    printfn " Classic FAKE usage"
    printfn "--------------------"
    CommandlineParams.printAllParams()

let printEnvironment cmdArgs args =
    printVersion()

    if buildServer = LocalBuild then
        trace localBuildLabel
    else
        tracefn "Build-Version: %s" buildVersion

    if cmdArgs |> Array.length > 1 then
        traceFAKE "FAKE Arguments:"
        args 
          |> Seq.map fst
          |> Seq.iter (tracefn "%A")

    log ""
    traceFAKE "FSI-Path: %s" fsiPath
    traceFAKE "MSBuild-Path: %s" msBuildExe

let containsParam param = Seq.map toLower >> Seq.exists ((=) (toLower param))

let paramIsHelp param = containsParam param ["help"; "?"; "/?"; "-h"; "--help"; "/h"; "/help"]

let buildScripts = !! "*.fsx" |> Seq.toList

try
    try
        AutoCloseXmlWriter <- true

        let cmdArgs = System.Environment.GetCommandLineArgs()

        let args = Cli.parsePositionalArgs cmdArgs

        match Cli.parsedArgsOrEx args.Rest with

        //We have new style help args!
        | Choice1Of2(fakeArgs) ->

            //Boot and version force us to ignore other args, so check for them and handle.
            let isBoot, bootArgs = fakeArgs.Contains <@ Cli.Boot @>, fakeArgs.GetResults <@ Cli.Boot @>
            let isVersion = fakeArgs.Contains <@ Cli.Version @>
            let printDetails = fakeArgs.Contains <@ Cli.PrintDetails @>

            match isVersion, isBoot with

            //Version.
            | true, _ -> printVersion()

            //Boot.
            | false, true ->
                let handler = Boot.HandlerForArgs bootArgs//Could be List.empty, but let Boot handle this.
                handler.Interact()

            //Try and run a build script! 
            | false, false ->

                traceStartBuild()
                if printDetails then printVersion()

                //Maybe log.
                match fakeArgs.TryGetResult <@ Cli.LogFile @> with
                | Some(path) -> addXmlListener path
                | None -> ()

                //Combine the key value pair vars and the flag vars.
                let envVars =
                    seq { yield! fakeArgs.GetResults <@ Cli.EnvFlag @> |> Seq.map (fun name -> name, "true")
                          yield! fakeArgs.GetResults <@ Cli.EnvVar @>
                          if args.Target.IsSome then yield "target", args.Target.Value }

                //Get our fsiargs from somewhere!
                let fsiArgs = 
                    match
                        fakeArgs.GetResults <@ Cli.FsiArgs @>,
                        args.Script,
                        List.isEmpty buildScripts with

                    //TODO check for presence of --fsiargs with no args?  Make attribute for UAP?

                    //Use --fsiargs approach.
                    | x::xs, _, _ ->
                        match FsiArgs.parse (x::xs |> Array.ofList)  with
                        | Choice1Of2(fsiArgs) -> fsiArgs
                        | Choice2Of2(msg) -> failwith (sprintf "Unable to parse --fsiargs.  %s." msg)

                    //Script path is specified.
                    | [], Some(script), _ -> FsiArgs([], script, [])

                    //No explicit script, but have in working directory.
                    | [], None, false -> FsiArgs([], List.head buildScripts, [])

                    //Noooo script anywhere!
                    | [], None, true -> failwith "Build script not specified on command line, in fsi args or found in working directory."
                    
                //TODO if printDetails then printEnvironment cmdArgs args

                if not (runBuildScriptWithFsiArgsAt "" printDetails fsiArgs envVars) then Environment.ExitCode <- 1
                else if printDetails then log "Ready."

                ()

        //None of the new style args parsed, so revert to the old skool.
        | Choice2Of2(ex) ->
            if (cmdArgs.Length = 2 && paramIsHelp cmdArgs.[1]) || (cmdArgs.Length = 1 && List.length buildScripts = 0) then printUsage () else
            match Boot.ParseCommandLine(cmdArgs) with
            | None ->
                let buildScriptArg = if cmdArgs.Length > 1 && cmdArgs.[1].EndsWith ".fsx" then cmdArgs.[1] else Seq.head buildScripts
                let fakeArgs = cmdArgs |> Array.filter (fun x -> x.StartsWith "-d:" = false)
                let fsiArgs = cmdArgs |> Array.filter (fun x -> x.StartsWith "-d:") |> Array.toList
                let args = CommandlineParams.parseArgs (fakeArgs |> Seq.filter ((<>) buildScriptArg) |> Seq.filter ((<>) "details"))

                traceStartBuild()
                let printDetails = containsParam "details" cmdArgs
                if printDetails then
                    printEnvironment cmdArgs args
                if not (runBuildScript printDetails buildScriptArg fsiArgs args) then Environment.ExitCode <- 1
                else if printDetails then log "Ready."
            | Some handler ->
                handler.Interact()
    with
    | exn -> 
        if exn.InnerException <> null then
            sprintf "Build failed.\nError:\n%s\nInnerException:\n%s" exn.Message exn.InnerException.Message
            |> traceError
            printUsage()
        else
            sprintf "Build failed.\nError:\n%s" exn.Message
            |> traceError
            printUsage()

        sendTeamCityError exn.Message
        Environment.ExitCode <- 1

    if buildServer = BuildServer.TeamCity then
        killAllCreatedProcesses()

finally
    traceEndBuild()
