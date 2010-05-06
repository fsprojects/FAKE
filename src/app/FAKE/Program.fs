open System
open Fake
open System.IO

let showFakeCommands() = traceFAKE "FAKE [buildScript]"

try  
    let cmdArgs = System.Environment.GetCommandLineArgs()  
    traceStartBuild()

    if cmdArgs.Length = 0 then showFakeCommands() else

    traceFAKE "FakePath: %s" fakePath   
    traceFAKE "%s" fakeVersionStr
    if buildServer = LocalBuild then
        trace localBuildLabel
    else
        tracefn "Build-Version: %s" buildVersion
      
    if cmdArgs |> Array.length > 1 then traceFAKE "FAKE Arguments:"
    let _,args = 
        cmdArgs 
            |> Array.fold (fun (i,acc) (a:string) -> 
                if i > 1 then
                    logf "%A" a
                    if a.Contains "=" then
                        let s = a.Split([|'='|])
                        i+1,(s.[0],s.[1])::acc
                    else
                        i+1,(a,"1")::acc
                else
                    (i+1,acc))
            (0,[])

    log ""
    traceFAKE "FSI-Path: %s" fsiPath
    traceFAKE "MSBuild-Path: %s" msBuildExe
          
    if not (runBuildScript cmdArgs.[1] args) then
        Environment.ExitCode <- 1
    else
        log "Ready."
with
| exn -> 
    if exn.InnerException <> null then
        sprintf "Build failed.\nError:\n%s\nInnerException:\n%s" exn.Message exn.InnerException.Message
          |> traceError
    else
        sprintf "Build failed.\nError:\n%s" exn.Message
          |> traceError
    sendTeamCityError exn.Message
    Environment.ExitCode <- 1
    
traceEndBuild()