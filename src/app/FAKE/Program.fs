open System
open Fake
open System.IO

try
    try            
        AutoCloseXmlWriter <- true            
        let cmdArgs = System.Environment.GetCommandLineArgs()

        if cmdArgs.Length <= 1 || cmdArgs.[1] = "help" then Console.WriteLine "FAKE [buildScript]" else

        let args = 
            let splitter = [|'='|]
            cmdArgs 
                |> Seq.skip 1
                |> Seq.map (fun (a:string) ->
                        if a.Contains "=" then
                            let s = a.Split splitter
                            if s.[0] = "logfile" then
                                addXmlListener s.[1]
                            s.[0], s.[1]
                        else
                            a,"1")
                |> Seq.toList
        
        traceStartBuild()
        traceFAKE "FakePath: %s" fakePath 
        traceFAKE "%s" fakeVersionStr
        if buildServer = LocalBuild then
            trace localBuildLabel
        else
            tracefn "Build-Version: %s" buildVersion
      
        if cmdArgs |> Array.length > 1 then 
            traceFAKE "FAKE Arguments:"
            args |> Seq.iter (tracefn "%A")

        log ""
        traceFAKE "FSI-Path: %s" fsiPath
        traceFAKE "MSBuild-Path: %s" msBuildExe
          
        if not (runBuildScript cmdArgs.[1] args) then
            Environment.ExitCode <- 1
        else
            log "Ready."
    with
    | exn -> 
        WaitUntilEverythingIsPrinted()
        if exn.InnerException <> null then
            sprintf "Build failed.\nError:\n%s\nInnerException:\n%s" exn.Message exn.InnerException.Message
            |> traceError
        else
            sprintf "Build failed.\nError:\n%s" exn.Message
            |> traceError
        sendTeamCityError exn.Message
        Environment.ExitCode <- 1
        WaitUntilEverythingIsPrinted()
finally
    traceEndBuild()
    WaitUntilEverythingIsPrinted()