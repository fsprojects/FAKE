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

let initLogAsConsole() = 
    log <- LoggerConfiguration()
           |> configureFileLogger
           |> configureConsoleLogger
           |> create

let initLogAsService eventLogWriter = 
    writeToEventLog <- eventLogWriter
    log <- LoggerConfiguration()
           |> configureFileLogger
           |> create

let debug fmt = Printf.kprintf log.Debug fmt

let info fmt = Printf.kprintf log.Information fmt

let warn fmt = Printf.kprintf log.Warning fmt

let error fmt = 
    let log str = 
        log.Error(str)
        writeToEventLog str EventLogEntryType.Error
    Printf.kprintf log fmt

let errorEx (ex : Exception) fmt = 
    let log str = 
        log.Error(ex, str)
        writeToEventLog (sprintf "%sn%s)" str (ex.ToString())) EventLogEntryType.Error
    Printf.kprintf log fmt
