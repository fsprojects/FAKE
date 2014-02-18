/// Contains helper function which allow to interact with Microsoft Dynamics NAV.
module Fake.DynamicsNav

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Threading
open System.Xml
open Fake.UnitTestHelper

[<RequireQualifiedAccess>]
/// A Dynamics NAV server type
type NavisionServerType = 
    | SqlServer
    | NativeServer

/// A parameter type to interact with Dynamics NAV
type DynamicsNavParams = 
    { ToolPath : string
      ServerName : string
      Database : string
      WorkingDir : string
      TempLogFile : string
      TimeOut : TimeSpan }

/// Retrieves the the file name of the Dynamics NAV ClassicClient for the given version from the registry.
let getNAVClassicPath navClientVersion = 
    let subKey = 
        match navClientVersion with
        | "601" | "602" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\60\Classic Client\W1 6.0"
        | "700" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\70\RoleTailored Client"
        | "701" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\71\RoleTailored Client"
        | "501" -> @"software\microsoft\Dynamics Nav\Cside Client\W1 5.0 SP1"
        | "403" -> @"SOFTWARE\Navision\Microsoft Business Solutions-Navision\W1 4.00"
        | _ -> failwithf "Unknown NAV-Version %s" navClientVersion
    getRegistryValue HKEYLocalMachine subKey "Path"

/// Gets the directory of the Dynamics NAV ClassicClient for the given version from the registry.
let getNAVPath navClientVersion = (directoryInfo (getNAVClassicPath navClientVersion)).Parent.FullName

/// Creates the connection information to a Dynamics NAV instance.
let createConnectionInfo navClientVersion serverMode serverName targetDatabase = 
    let navServicePath = 
        try 
            let navServiceRootPath = 
                let subKey = 
                    match navClientVersion with
                    | "601" | "602" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\60\Classic Client\W1 6.0"
                    | "700" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\70\Service"
                    | "701" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\71\Service"
                    | _ -> failwithf "Unknown NAV-Version %s" navClientVersion
                getRegistryValue HKEYLocalMachine subKey "Path"
            (directoryInfo navServiceRootPath).Parent.FullName @@ "Service"
        with exn -> @"C:\Program Files\Navision701\71\Service"
    
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
      TimeOut = TimeSpan.FromMinutes 20. }

let private analyzeLogFile fileName = 
    try 
        let lines = ReadFile fileName |> Seq.toList
        lines |> Seq.iter traceError
        DeleteFile fileName
        lines.Length
    with exn -> 
        traceError exn.Message
        1

let private reportError text logFile = 
    let errors = analyzeLogFile logFile
    failwith (text + (if errors = 1 then " with 1 error."
                      else sprintf " with %d errors." errors))

let private import connectionInfo fileName = 
    let fi = fileInfo fileName
    
    let deleteFile, fi = 
        if fi.Extension = ".nav" then true, fi.CopyTo(Path.Combine(fi.Directory.FullName, fi.Name + ".txt"))
        else false, fi
    
    let args = 
        sprintf "command=importobjects, file=\"%s\", logfile=\"%s\", servername=\"%s\", database=\"%s\"" fi.FullName 
            (FullName connectionInfo.TempLogFile) connectionInfo.ServerName connectionInfo.Database
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- connectionInfo.ToolPath
                info.WorkingDirectory <- connectionInfo.WorkingDir
                info.Arguments <- args) connectionInfo.TimeOut
    then 
        if deleteFile then fi.Delete()
        reportError "ImportFile failed" connectionInfo.TempLogFile
    if deleteFile then fi.Delete()

let private export connectionInfo filter fileName = 
    let fi = fileInfo fileName
    let args = 
        sprintf "command=exportobjects, file=\"%s\", logfile=\"%s\", filter=\"%s\", servername=\"%s\", database=\"%s\"" 
            fi.FullName (FullName connectionInfo.TempLogFile) filter connectionInfo.ServerName connectionInfo.Database
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- connectionInfo.ToolPath
                info.WorkingDirectory <- connectionInfo.WorkingDir
                info.Arguments <- args) connectionInfo.TimeOut
    then 
        fi.Delete()
        reportError "Export failed" connectionInfo.TempLogFile

/// Exports objects from the Dynamics NAV client based on the given filter to the given .txt or .fob file
let ExportObjects connectionInfo filter fileName = 
    traceStartTask "ExportObjects" fileName
    export connectionInfo filter fileName
    traceEndTask "ExportObjects" fileName

/// Exports all objects from the Dynamics NAV client to the given .txt or .fob file
let ExportAllObjects connectionInfo fileName = 
    traceStartTask "ExportAllObjects" fileName
    export connectionInfo "" fileName
    traceEndTask "ExportAllObjects" fileName

/// Imports the given .txt or .fob file into the Dynamics NAV client
let ImportFile connectionInfo fileName = 
    traceStartTask "ImportFile" fileName
    import connectionInfo fileName
    traceEndTask "ImportFile" fileName

/// Creates an import file from the given .txt files.
let CreateImportFile importFileName files = 
    let details = importFileName
    traceStartTask "CreateImportFile" details
    files
    |> Seq.toList
    |> List.sortBy (fun name -> 
           let firstLine = (ReadFile name |> Seq.head).Split(' ')
           firstLine.[3], firstLine.[2])
    |> AppendTextFiles importFileName
    traceEndTask "CreateImportFile" details

/// Creates an import file from the given .txt files and imports it into the Dynamics NAV client.
/// If the import fails, then every file will be tried alone.
let ImportFiles connectionInfo importFileName files = 
    let details = importFileName
    traceStartTask "ImportFiles" details
    CreateImportFile importFileName files
    try 
        ImportFile connectionInfo importFileName
    with exn -> 
        files |> Seq.iter (fun file -> 
                     try 
                         ImportFile connectionInfo file
                     with _ -> ())
        raise exn
    traceEndTask "ImportFiles" details

/// Compiles all uncompiled objects in the Dynamics NAV client.
let CompileAll connectionInfo = 
    let details = ""
    traceStartTask "CompileAll" details
    let args = 
        sprintf "command=compileobjects, filter=\"Compiled=0\", logfile=\"%s\", servername=\"%s\", database=\"%s\"" 
            (FullName connectionInfo.TempLogFile) connectionInfo.ServerName connectionInfo.Database
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- connectionInfo.ToolPath
                info.WorkingDirectory <- connectionInfo.WorkingDir
                info.Arguments <- args) connectionInfo.TimeOut
    then reportError "CompileAll failed" connectionInfo.TempLogFile
    traceEndTask "CompileAll" details

/// The parameter type allows to interact with Dynamics NAV RTC.
type RTCParams = 
    { ToolPath : string
      ServerName : string
      ServiceTierName : string
      Company : string
      Port : int
      WorkingDir : string
      TempLogFile : string
      TimeOut : TimeSpan }

/// Creates the connection information to a Dynamics NAV RTC instance
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

/// Runs a codeunit with the given ID on the RTC client
let RunCodeunit connectionInfo (codeunitID : int) = 
    let details = codeunitID.ToString()
    traceStartTask "Running Codeunit" details
    let args = 
        sprintf "-consolemode \"DynamicsNAV://%s:%d/%s/%s/runcodeunit?codeunit=%d\" -ShowNavigationPage:0" 
            connectionInfo.ServerName connectionInfo.Port connectionInfo.ServiceTierName connectionInfo.Company 
            codeunitID
    
    let exitCode = 
        ExecProcess (fun info -> 
            info.FileName <- connectionInfo.ToolPath
            info.WorkingDirectory <- connectionInfo.WorkingDir
            info.Arguments <- args) connectionInfo.TimeOut
    if exitCode <> 0 && exitCode <> 255 then 
        reportError (sprintf "Running codeunit %d failed with ExitCode %d" codeunitID exitCode) 
            connectionInfo.TempLogFile
    traceEndTask "Running Codeunit" details

/// Opens a page with the given ID on the RTC client
let OpenPage connectionInfo pageNo = 
    let details = sprintf "%d" pageNo
    traceStartTask "OpenPage" details
    let protocol = 
        sprintf @"dynamicsnav://%s:%d/%s/%s/runpage?page=%d" connectionInfo.ServerName connectionInfo.Port 
            connectionInfo.ServiceTierName connectionInfo.Company pageNo
    let p = new Process()
    p.StartInfo <- new ProcessStartInfo(protocol)
    let result = p.Start()
    traceEndTask "OpenPage" details
    result

/// Returns all running NAV processes.
let getNAVProcesses() = 
    Process.GetProcesses() 
    |> Seq.filter 
           (fun p -> 
           p.ProcessName.StartsWith("fin") || p.ProcessName = "finsql" || p.ProcessName.StartsWith("slave") 
           || p.ProcessName.StartsWith("Microsoft.Dynamics.Nav.Client"))

/// Closes all running Dynamics NAV instances
let CloseAllNavProcesses raiseExceptionIfNotFound = 
    let details = ""
    traceStartTask "CloseNAV" details
    let closedProcesses = 
        getNAVProcesses()
        |> Seq.map (fun p -> p.Kill())
        |> Seq.toList
    if closedProcesses = [] && raiseExceptionIfNotFound then failwith "Could not kill NAV processes"
    traceEndTask "CloseNAV" details

/// Waits until all NAV processes have stopped or fails after given timeout.
/// ## Parameters
///  - `name` - The name of the processes in question.
///  - `timeout` - The timespan to time out after.
let ensureAllNAVProcessesHaveStopped timeout = 
    let endTime = DateTime.Now.Add timeout
    while DateTime.Now <= endTime && (getNAVProcesses() <> Seq.empty) do
        tracefn "Waiting for NAV process to stop (Timeout: %A)" endTime
        Thread.Sleep 1000
    if getNAVProcesses() <> Seq.empty then failwith "The NAV process has not stopped (check the logs for errors)"

/// Analyzes the Dynamics NAV test results
let analyzeTestResults fileName = 
    let messages = ReadFile fileName
    if Seq.isEmpty messages then failwithf "Communication error. The message file %s is empty." fileName
    let findNext pattern (messages : string seq) = 
        messages
        |> Seq.skipWhile (fun x -> x.StartsWith pattern |> not)
        |> Seq.head
        |> replace pattern ""
    
    let tryFindNext pattern (messages : string seq) = 
        try 
            Some(findNext pattern messages)
        with _ -> None
    
    if tryFindNext "TestSuiteNotFound;" messages <> None then None
    else 
        match tryFindNext "TestSuite;" messages with
        | None -> None
        | Some suiteName -> 
            let rec getTests (messages : string seq) = 
                let messages = 
                    messages
                    |> Seq.skip 1
                    |> Seq.skipWhile (fun x -> x.StartsWith "Starting TestCase" |> not)
                
                let currentMessages = messages |> Seq.takeWhile (fun x -> x.StartsWith "EndOfTest;" |> not)
                if Seq.isEmpty messages then []
                else 
                    match tryFindNext "TestCase;" currentMessages with
                    | None -> getTests messages
                    | Some testName -> 
                        let status = 
                            match currentMessages 
                                  |> Seq.tryFind (fun x -> x.StartsWith "Error;" || x.StartsWith "Ignored;") with
                            | Some error when error.StartsWith "Error;" -> 
                                let msg = error.Replace("Error;", "").Split [| ';' |]
                                Failure(msg.[0], msg.[1])
                            | Some error when error.StartsWith "Ignored;" -> 
                                let msg = error.Replace("Ignored;", "").Split [| ';' |]
                                if msg.Length > 2 then Ignored(msg.[0], msg.[1])
                                else Ignored("", "")
                            | _ -> Ok
                        
                        let runTime = 
                            match tryFindNext "Runtime;" currentMessages with
                            | None -> TimeSpan.Zero
                            | Some time -> 
                                match Int32.TryParse time with
                                | true, rt -> TimeSpan.FromMilliseconds(float rt)
                                | _ -> TimeSpan.Zero
                        
                        { Name = testName
                          RunTime = runTime
                          Status = status } :: getTests messages
            
            let tests = getTests messages
            Some { SuiteName = suiteName
                   Tests = tests }

/// Analyzes the XML-based Dynamics NAV test results from XMLPort 130021
let analyzeXmlTestResults (fileName : string) (testSuite : string) = 
    let doc = new XmlDocument()
    doc.Load(fileName)
    let suite = doc.SelectSingleNode("/TestSuites/TestSuite[Name='" + testSuite + "']")
    
    let tests = 
        suite.SelectNodes("TestLines/TestLine[Level='2']")
        |> Seq.cast<XmlNode>
        |> Seq.map (fun node -> 
               let testName = node.SelectSingleNode("Name").InnerText
               let testCodeunit = node.SelectSingleNode("TestCodeunit").InnerText
               
               let startTime = 
                   match DateTime.TryParse(node.SelectSingleNode("StartTime").InnerText) with
                   | true, dt -> dt
                   | _ -> DateTime.Now
               
               let endTime = 
                   match DateTime.TryParse(node.SelectSingleNode("FinishTime").InnerText) with
                   | true, dt -> dt
                   | _ -> DateTime.Now
               
               let status = 
                   match node.SelectSingleNode("Run").InnerText with
                   | "No" -> Ignored("", "")
                   | _ -> 
                       match node.SelectSingleNode("Result").InnerText with
                       | "Failure" -> 
                           Failure
                               (node.SelectSingleNode("FirstError").InnerText, 
                                "(CU: " + testCodeunit + ", " + node.SelectSingleNode("Function").InnerText + ")\n")
                       | _ -> Ok
               
               { Name = testName
                 RunTime = endTime - startTime
                 Status = status })
        |> List.ofSeq
    Some { SuiteName = testSuite
           Tests = tests }
