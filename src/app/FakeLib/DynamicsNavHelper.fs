module Fake.DynamicsNav

open System
open System.Diagnostics
open System.Text
open System.IO

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

/// Creates the connection information to a Dynamics NAV instance
let createConnectionInfo navClientVersion serverMode serverName targetDatabase =
    let navClassicPath =
        let subKey = 
            match navClientVersion with
            | "601"
            | "602" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\60\Classic Client\W1 6.0"
            | "700" -> @"SOFTWARE\Microsoft\Microsoft Dynamics NAV\70\RoleTailored Client"
            | "501" -> @"software\microsoft\Dynamics Nav\Cside Client\W1 5.0 SP1"
            | "403" -> @"SOFTWARE\Navision\Microsoft Business Solutions-Navision\W1 4.00"
            | _     -> failwithf "Unknown NAV-Version %s" navClientVersion

        getRegistryValue HKEYLocalMachine subKey "Path"

    let navPath = (directoryInfo navClassicPath).Parent.FullName
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

    let navRTCPath = navPath @@ "RoleTailored Client"

    let clientExe = 
        match serverMode with
        | NavisionServerType.SqlServer -> "finsql.exe"
        | NavisionServerType.NativeServer -> "fin.exe"

    let finExe = navClassicPath @@ clientExe

    { ToolPath =  finExe
      WorkingDir = null
      ServerName = serverName
      Database = targetDatabase
      TempLogFile = "./NavErrorMessages.txt"
      TimeOut = TimeSpan.FromMinutes 5.}

let private analyzeLogFile fileName =
    let lines = ReadFile fileName |> Seq.toList
    lines |> Seq.iter traceError
    DeleteFile fileName
    lines.Length

let private reportError text logFile =
    let errors = analyzeLogFile logFile
    failwith (text + (if errors = 1 then " with 1 error." else sprintf " with %d errors." errors))

let private import connectionInfo fileName =
    let fi = fileInfo fileName
    let fi =
        if fi.Extension = ".nav" then
            fi.CopyTo(Path.Combine(fi.Directory.FullName,fi.Name + ".txt"))
        else
            fi

    let args = 
        sprintf "command=importobjects, file=\"%s\", logfile=\"%s\", servername=\"%s\", database=\"%s\"" 
            fi.FullName (FullName connectionInfo.TempLogFile) connectionInfo.ServerName connectionInfo.Database

    if not (execProcess3 (fun info ->  
        info.FileName <- connectionInfo.ToolPath
        info.WorkingDirectory <- connectionInfo.WorkingDir
        info.Arguments <- args) connectionInfo.TimeOut)
    then
        reportError "ImportFile failed" connectionInfo.TempLogFile

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

/// Opens a page with the RTC client
let OpenPage server port serviceTierName company pageNo =
    let details = sprintf "%d" pageNo
    traceStartTask "OpenPage" details
    let protocol = sprintf @"dynamicsnav://%s:%s/%s/%s/runpage?page=%d" server port serviceTierName company pageNo

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