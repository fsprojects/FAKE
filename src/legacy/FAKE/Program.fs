open System
open Fake
open System.IO
open Argu

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let printVersion() =
    traceFAKE "FakePath: %s" fakePath
    traceFAKE "%s" fakeVersionStr

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let printUsage () =
    printfn "-------------------"
    printfn " FAKE usage"
    printfn "-------------------"
    Cli.printUsage ()

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let containsParam param = Seq.map toLower >> Seq.exists ((=) (toLower param))

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let paramIsHelp param = containsParam param ["help"; "?"; "/?"; "-h"; "--help"; "/h"; "/help"]

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let buildScripts = !! "*.fsx" |> Seq.toList

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let encodingToRestore = Console.OutputEncoding
try
    try
        AutoCloseXmlWriter <- true

        let cmdArgs = System.Environment.GetCommandLineArgs()


        let hasRemoveWarning, cmdArgs =
            if cmdArgs |> Seq.contains "--removeLegacyFakeWarning" then
                true, Array.filter (fun arg -> arg <> "--removeLegacyFakeWarning") cmdArgs
            else false, cmdArgs

        let hasRemoveWarningEnvVar = System.Environment.GetEnvironmentVariable("FAKE_NO_LEGACY_WARNING") = "true"
        if not hasRemoveWarning && not hasRemoveWarningEnvVar then
            eprintfn "This runner is now obsolete with FAKE 5, please upgrade to the new .Net Core runner. See https://fake.build/fake-migrate-to-fake-5.html"
            eprintfn "To remove this warning you can append the '--removeLegacyFakeWarning' argument or set the 'FAKE_NO_LEGACY_WARNING' environment variable to 'true'"

        let args = Cli.parsePositionalArgs cmdArgs

        match Cli.parsedArgsOrEx args.Rest with

        //We have new style help args!
        | Choice1Of2(fakeArgs) ->

            //Break to allow a debugger to be attached here
            if fakeArgs.Contains <@ Cli.Break @> then
                Diagnostics.Debugger.Launch() |> ignore
                Diagnostics.Debugger.Break() |> ignore

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
                          if fakeArgs.Contains <@ Cli.Single_Target @> then yield "single-target", "true"
                          if args.Target.IsSome then yield "target", args.Target.Value }

                //Get our fsiargs from somewhere!
                let fsiArgs =
                    match
                        fakeArgs.GetResults <@ Cli.FsiArgs @>,
                        args.Script,
                        List.isEmpty buildScripts with

                    //TODO check for presence of --fsiargs with no args?  Make attribute for UAP?

                    //Use --fsiargs approach.
                    | x::xs, maybeScript, _ ->
                        match FsiArgs.parse (x::xs |> Array.ofList)  with
                        | Choice1Of2(fsiArgs) -> fsiArgs
                        | Choice2Of2(msg) ->
                            match maybeScript with
                            | Some script ->
                                match FsiArgs.parse ((x::xs @ [script]) |> Array.ofList) with
                                | Choice1Of2(fsiArgs) -> fsiArgs
                                | Choice2Of2(msg) ->
                                    failwith (sprintf "Unable to parse --fsiargs.  %s." msg)
                            | None -> 
                                    failwith (sprintf "Unable to parse --fsiargs.  %s." msg)
                    //Script path is specified.
                    | [], Some(script), _ -> FsiArgs([], script, [])

                    //No explicit script, but have in working directory.
                    | [], None, false -> FsiArgs([], List.head buildScripts, [])

                    //Noooo script anywhere!
                    | [], None, true -> failwith "Build script not specified on command line, in fsi args or found in working directory."

                //TODO if printDetails then printEnvironment cmdArgs args

                let useCache = not (fakeArgs.Contains <@ Cli.NoCache @>)
                if not (runBuildScriptWithFsiArgsAt printDetails fsiArgs envVars useCache) then Environment.ExitCode <- 1
                else if printDetails then log "Ready."

                ()

        //None of the new style args parsed, so revert to the old skool.
        | Choice2Of2(ex) ->

            // #1082 print a warning as we've been invoked with invalid OR old-style args.
            // traceImportant "Error parsing command line arguments.  You have a mistake in your args, or are using the pre-2.1.8 argument style:"
            // exceptionAndInnersToString ex |> traceImportant
            // trace "Attempting to run with pre-version 2.18 argument style, for backwards compat."

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
                let useCache = true
                if not (runBuildScript printDetails buildScriptArg fsiArgs args useCache) then Environment.ExitCode <- 1
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

        let isKnownException = exn :? FAKEException
        if not isKnownException then
            sendTeamCityError exn.Message

        Environment.ExitCode <- 1

    killAllCreatedProcesses()

finally
    traceEndBuild()
    Console.OutputEncoding <- encodingToRestore
    if !TargetHelper.ExitCode.exitCode <> 0 then exit !TargetHelper.ExitCode.exitCode
    if Environment.ExitCode <> 0 then exit Environment.ExitCode
