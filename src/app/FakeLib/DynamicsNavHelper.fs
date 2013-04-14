module Fake.DynamicsNav

open System
open System.Diagnostics
open System.Text
open System.IO
open Fake.UnitTestHelper

[<RequireQualifiedAccess>]
type NavisionServerType =
| SqlServer
| NativeServer

type DynamicsNavParams =
    { ToolPath: string
      ServerName: string
      Database: string
      WorkingDir: string
      TempLogFile: string
      TimeOut: TimeSpan}

let getNAVClassicPath navClientVersion =
    let subKey = 
        match navClientVersion with
        | "601"
        | "602" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\60\Classic Client\W1 6.0"
        | "700" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\70\RoleTailored Client"
        | "501" -> @"software\microsoft\Dynamics Nav\Cside Client\W1 5.0 SP1"
        | "403" -> @"SOFTWARE\Navision\Microsoft Business Solutions-Navision\W1 4.00"
        | _     -> failwithf "Unknown NAV-Version %s" navClientVersion

    getRegistryValue HKEYLocalMachine subKey "Path"

let getNAVPath navClientVersion = (directoryInfo (getNAVClassicPath navClientVersion)).Parent.FullName

/// Creates the connection information to a Dynamics NAV instance
let createConnectionInfo navClientVersion serverMode serverName targetDatabase =
    let navServicePath = 
        try
            let navServiceRootPath =
                let subKey = 
                    match navClientVersion with
                    | "601"
                    | "602" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\60\Classic Client\W1 6.0"
                    | "700" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\70\Service"
                    | _     -> failwithf "Unknown NAV-Version %s" navClientVersion

                getRegistryValue HKEYLocalMachine subKey "Path"

            (directoryInfo navServiceRootPath).Parent.FullName @@ "Service"    
        with
        | exn -> @"C:\Program Files\Navision700\70\Service"

    let clientExe = 
        match serverMode with
        | NavisionServerType.SqlServer -> "finsql.exe"
        | NavisionServerType.NativeServer -> "fin.exe"

    let finExe = getNAVClassicPath navClientVersion @@ clientExe

    { ToolPath = finExe
      WorkingDir = null
      ServerName = serverName
      Database = targetDatabase
      TempLogFile = "./NavErrorMessages.txt"
      TimeOut = TimeSpan.FromMinutes 20.}

let private analyzeLogFile fileName =
    try
        let lines = ReadFile fileName |> Seq.toList
        lines |> Seq.iter traceError
        DeleteFile fileName
        lines.Length
    with 
    | exn -> 
        traceError exn.Message
        1

let private reportError text logFile =    
    let errors = analyzeLogFile logFile
    failwith (text + (if errors = 1 then " with 1 error." else sprintf " with %d errors." errors))

let private import connectionInfo fileName =
    let fi = fileInfo fileName     
    let deleteFile,fi =
        if fi.Extension = ".nav" then
            true,fi.CopyTo(Path.Combine(fi.Directory.FullName,fi.Name + ".txt"))
        else
            false,fi

    let args = 
        sprintf "command=importobjects, file=\"%s\", logfile=\"%s\", servername=\"%s\", database=\"%s\"" 
            fi.FullName (FullName connectionInfo.TempLogFile) connectionInfo.ServerName connectionInfo.Database

    if not (execProcess3 (fun info ->  
        info.FileName <- connectionInfo.ToolPath
        info.WorkingDirectory <- connectionInfo.WorkingDir
        info.Arguments <- args) connectionInfo.TimeOut)
    then
        if deleteFile then fi.Delete()
        reportError "ImportFile failed" connectionInfo.TempLogFile

    if deleteFile then fi.Delete()

/// Imports the given txt or fob file into the Dynamics NAV client
let ImportFile connectionInfo fileName =
    traceStartTask "ImportFile" fileName
    import connectionInfo fileName                  
    traceEndTask "ImportFile" fileName

/// Creates an importfile from the given files
let CreateImportFile importFileName files =
    let details = importFileName
    
    traceStartTask "CreateImportFile" details
    files
        |> Seq.toList
        |> List.sortBy (fun name -> 
              let firstLine = (ReadFile name |> Seq.head).Split(' ')
              firstLine.[3],firstLine.[2])
        |> AppendTextFiles importFileName

    traceEndTask "CreateImportFile" details

/// Creates an import file from the given files and imports it into the Dynamics NAV client
/// If the import fails, then every file will be tried alone
let ImportFiles connectionInfo importFileName files =
    let details = importFileName
    
    traceStartTask "ImportFiles" details

    CreateImportFile importFileName files

    try 
        ImportFile connectionInfo importFileName
    with exn ->
        files
          |> Seq.iter (fun file -> try ImportFile connectionInfo file with _ -> ())
        raise exn
                          
    traceEndTask "ImportFiles" details

/// Compiles all uncompiled objects in the Dynamics NAV client
let CompileAll connectionInfo =
    let details = ""
    
    traceStartTask "CompileAll" details
    let args = 
      sprintf "command=compileobjects, filter=\"Compiled=0\", logfile=\"%s\", servername=\"%s\", database=\"%s\"" 
        (FullName connectionInfo.TempLogFile) connectionInfo.ServerName connectionInfo.Database

    if not (execProcess3 (fun info ->  
        info.FileName <- connectionInfo.ToolPath
        info.WorkingDirectory <- connectionInfo.WorkingDir
        info.Arguments <- args) connectionInfo.TimeOut)
    then
        reportError "CompileAll failed" connectionInfo.TempLogFile
                  
    traceEndTask "CompileAll" details

type RTCParams =
    { ToolPath: string
      ServerName: string
      ServiceTierName: string
      Company: string
      Port: int
      WorkingDir: string
      TempLogFile: string
      TimeOut: TimeSpan}

/// Creates the connection information to a Dynamics NAV RTC
let createRTCConnectionInfo navClientVersion serverName serviceTierName port company =
    let navRTCPath = getNAVPath navClientVersion @@ "RoleTailored Client"
    let rtcExe = navRTCPath @@ "Microsoft.Dynamics.NAV.Client.exe"

    { ToolPath = rtcExe
      ServerName = serverName
      ServiceTierName = serviceTierName
      Company = company
      Port = port
      WorkingDir = null
      TempLogFile = "./NavErrorMessages.txt"
      TimeOut = TimeSpan.FromMinutes 20. }

/// Runs a codeunit with the RTC client
let RunCodeunit connectionInfo (codeunit:int) =
    let details = codeunit.ToString()
    traceStartTask "Running Codeunit" details
    let args = 
      sprintf "-consolemode \"DynamicsNAV://%s:%d/%s/%s/runcodeunit?codeunit=%d\" -ShowNavigationPage:0" 
        connectionInfo.ServerName
        connectionInfo.Port
        connectionInfo.ServiceTierName 
        connectionInfo.Company 
        codeunit

    let exitCode =
        execProcessAndReturnExitCode (fun info ->  
            info.FileName <- connectionInfo.ToolPath
            info.WorkingDirectory <- connectionInfo.WorkingDir
            info.Arguments <- args) connectionInfo.TimeOut
    if exitCode <> 0 && exitCode <> 255 then
        reportError (sprintf "Running codeunit %d failed with ExitCode %d" codeunit exitCode) connectionInfo.TempLogFile
                  
    traceEndTask "Running Codeunit" details

/// Opens a page with the RTC client
let OpenPage connectionInfo pageNo =
    let details = sprintf "%d" pageNo
    traceStartTask "OpenPage" details
    let protocol = sprintf @"dynamicsnav://%s:%d/%s/%s/runpage?page=%d" connectionInfo.ServerName connectionInfo.Port connectionInfo.ServiceTierName connectionInfo.Company pageNo

    let p = new Process()
    p.StartInfo <- new ProcessStartInfo(protocol)
    let result = p.Start()

    traceEndTask "OpenPage" details
    result

/// Closes all running Dynamics NAV instances
let CloseAllNavProcesses raiseExceptionIfNotFound =
    let details = ""
    traceStartTask "CloseNAV" details
    let closedProcesses =
        Process.GetProcesses()
          |> Seq.filter(fun p -> 
                p.ProcessName.StartsWith("fin") || 
                p.ProcessName = "finsql" || 
                p.ProcessName.StartsWith("slave") || 
                p.ProcessName.StartsWith("Microsoft.Dynamics.Nav.Client"))
          |> Seq.map(fun p -> p.Kill())
          |> Seq.toList

    if closedProcesses = [] && raiseExceptionIfNotFound then
        failwith "Could not kill NAV processes"

    traceEndTask "CloseNAV" details


let analyzeTestResults fileName =
    let messages = ReadFile fileName

    if Seq.isEmpty messages then
        failwithf "Communication error. The message file %s is empty." fileName

    let findNext pattern (messages:string seq) =
        messages
        |> Seq.skipWhile (fun x -> x.StartsWith pattern |> not)
        |> Seq.head
        |> replace pattern ""

    let tryFindNext pattern (messages:string seq) =
        try
           Some (findNext pattern messages)
        with 
        | _ -> None 

    if tryFindNext "TestSuiteNotFound;" messages <> None then None else

    match tryFindNext "TestSuite;" messages with
    | None -> None
    | Some suiteName ->
        let rec getTests (messages:string seq) =
            let messages =
                messages
                |> Seq.skip 1
                |> Seq.skipWhile (fun x -> x.StartsWith "Starting TestCase" |> not)        

            let currentMessages =
                messages
                |> Seq.takeWhile (fun x -> x.StartsWith "EndOfTest;" |> not)

            if Seq.isEmpty messages then [] else

        
            match tryFindNext "TestCase;" currentMessages with
            | None -> getTests messages
            | Some testName -> 
                let status = 
                    match currentMessages |> Seq.tryFind (fun x -> x.StartsWith "Error;" || x.StartsWith "Ignored;" ) with
                    | Some error when error.StartsWith "Error;"  -> 
                        let msg = error.Replace("Error;","").Split [|';'|]
                        Failure (msg.[0],msg.[1])
                    | Some error when error.StartsWith "Ignored;"  -> 
                        let msg = error.Replace("Ignored;","").Split [|';'|]
                        if msg.Length > 2 then Ignored (msg.[0],msg.[1]) else Ignored("","")
                    | _ -> Ok

                let runTime = 
                    match tryFindNext "Runtime;" currentMessages with
                    | None -> TimeSpan.Zero
                    | Some time ->
                        match Int32.TryParse time with
                        | true,rt -> TimeSpan.FromMilliseconds (float rt)
                        | _ -> TimeSpan.Zero

                { Name = testName 
                  RunTime = runTime
                  Status = status } :: getTests messages

        let tests = getTests messages

        Some { SuiteName = suiteName; Tests = tests }