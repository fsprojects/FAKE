open System
open Fake
open System.IO
open Microsoft.FSharp.Compiler.Interactive

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
        
        let args = CommandlineParams.parseArgs cmdArgs
        
        traceStartBuild()
        if printDetails then printEnvironment cmdArgs args

        let exe = "MyTest.exe"
        let standardOpts =  [| "--noframework"; "-r:mscorlib.dll"; "-r:FSharp.Core.dll"; "-r:System.dll"; "-r:System.Core.dll"; |]
        let srcCodeServices = new Runner.SimpleSourceCodeServices()
        let argv = [| yield "fsc.exe"; yield "-o"; yield exe; yield! standardOpts; yield buildScriptArg; |]
        let exec = true
        let stdin, stdout = new Samples.ConsoleApp.CompilerInputStream(), new Samples.ConsoleApp.CompilerOutputStream()
        let stdins, stdouts = (new StreamReader(stdin)), (new StreamWriter(stdout))
        let streams =
            if exec then
                Some ((stdins :> TextReader), (stdouts :> TextWriter), (stdouts :> TextWriter))
            else None
        let errors, result, assemblyOpt = srcCodeServices.CompileToDynamicAssembly (argv, streams)
        stdouts.Flush()
        tracefn "Errors - %A" errors
        let outs = stdout.Read()
        tracefn "Generated output..."
        tracefn "%A" outs

//        if not (runBuildScript printDetails buildScriptArg args) then
//            Environment.ExitCode <- 1
//        else
//            if printDetails then log "Ready."
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

    if buildServer = BuildServer.TeamCity then
        killFSI()
        killMSBuild()

finally
    traceEndBuild()