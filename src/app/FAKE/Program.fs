open System
open Fake
open System.IO

let printEnvironment cmdArgs args =
    traceFAKE "FakePath: %s" fakePath
    traceFAKE "%s" fakeVersionStr

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
      
let buildScripts = !! "*.fsx" |> Seq.toList

try
    try            
        AutoCloseXmlWriter <- true            
        let cmdArgs = System.Environment.GetCommandLineArgs()                
        let printDetails = cmdArgs |> Seq.map (fun (a:string) -> a.ToLower()) |> Seq.exists ((=) "details")

        if (cmdArgs.Length = 2 && cmdArgs.[1].ToLower() = "help") || (cmdArgs.Length = 1 && List.length buildScripts = 0) then CommandlineParams.printAllParams() else
        
        let buildScriptArg = if cmdArgs.Length > 1 && cmdArgs.[1].EndsWith ".fsx" then cmdArgs.[1] else Seq.head buildScripts
        
        let args = CommandlineParams.parseArgs (cmdArgs |> Seq.filter buildScriptArg)
        
        traceStartBuild()
        if printDetails then printEnvironment cmdArgs args

        if not (runBuildScript printDetails buildScriptArg args) then
            Environment.ExitCode <- 1
        else
            if printDetails then log "Ready."
    with
    | exn -> 
        if exn.InnerException <> null then
            sprintf "Build failed.\nError:\n%O\nInnerException:\n%O" exn exn.InnerException
            |> traceError
        else
            sprintf "Build failed.\nError:\n%O" exn
            |> traceError
        sendTeamCityError exn.Message
        Environment.ExitCode <- 1

    if buildServer = BuildServer.TeamCity then
        killFSI()
        killMSBuild()

finally
    traceEndBuild()