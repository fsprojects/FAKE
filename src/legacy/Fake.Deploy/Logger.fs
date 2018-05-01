[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.Logger

open System
open System.Diagnostics
open System.Globalization
open System.IO
open Serilog

let mutable private log = LoggerConfiguration().CreateLogger()
let mutable private writeToEventLog = fun s (e : EventLogEntryType) -> ()

let private configureFileLogger (logConfig : LoggerConfiguration) = 
    let pathFormat = 
        let dir = AppConfig.LogDirectory
        
        let dir' = 
            if dir.StartsWith("~") then dir.Replace("~", AppConfig.WorkDirectory)
            else dir
            |> Path.GetFullPath
        Path.Combine(dir', "{Date}.log")
    
    let minLogLevel = Events.LogEventLevel.Information
    let outputTemplate = "{Timestamp} [{Level}] {Message}{NewLine}{Exception}"
    let formatProvider = CultureInfo("sv-SE")
    let maxFileSize = Nullable<int64>(1024L * 1024L * 1024L)
    let maxFilesToKeep = Nullable<int>(30)
    logConfig.WriteTo.RollingFile(pathFormat, minLogLevel, outputTemplate, formatProvider, maxFileSize, maxFilesToKeep)
             .MinimumLevel.Information()

let private configureConsoleLogger (logconfig : LoggerConfiguration) = 
    logconfig.WriteTo.ColoredConsole().MinimumLevel.Information()

let private create (logconfig : LoggerConfiguration) = 
    logconfig.CreateLogger()

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let initLogAsConsole() = 
    log <- LoggerConfiguration()
           |> configureFileLogger
           |> configureConsoleLogger
           |> create

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let initLogAsService eventLogWriter = 
    writeToEventLog <- eventLogWriter
    log <- LoggerConfiguration()
           |> configureFileLogger
           |> create

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let debug fmt = Printf.kprintf log.Debug fmt

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let info fmt = Printf.kprintf log.Information fmt

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let warn fmt = Printf.kprintf log.Warning fmt

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let error fmt = 
    let log str = 
        log.Error(str)
        writeToEventLog str EventLogEntryType.Error
    Printf.kprintf log fmt

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let errorEx (ex : Exception) fmt = 
    let log str = 
        log.Error(ex, str)
        writeToEventLog (sprintf "%sn%s)" str (ex.ToString())) EventLogEntryType.Error
    Printf.kprintf log fmt
