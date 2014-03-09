/// Contains Logger implementations for MsBuild.
module Fake.MsBuildLogger

open Fake
open Microsoft.Build
open Microsoft.Build.Framework
open System
open System.Collections.Generic
open System.IO

/// [omit]
let errToStr (a : BuildErrorEventArgs) = sprintf "%s: %s(%d,%d): %s" a.Code a.File a.LineNumber a.ColumnNumber a.Message

/// Abstract MSBuild Logger class.
type MSBuildLogger() = 
    let mutable Verbosity = LoggerVerbosity.Normal
    let mutable Parameters = ""
    
    /// Abstract function which registers an event listener.
    abstract RegisterEvents : IEventSource -> unit
    
    override t.RegisterEvents e = ()
    interface ILogger with
        
        member this.Parameters 
            with get () = Parameters
            and set (value) = Parameters <- value
        
        member this.Verbosity 
            with get () = Verbosity
            and set (value) = Verbosity <- value
        
        member this.Shutdown() = ()
        member this.Initialize(eventSource) = this.RegisterEvents(eventSource)

/// TeamCity Logger for MSBuild
type TeamCityLogger() = 
    inherit MSBuildLogger()
    override this.RegisterEvents(eventSource) = 
        eventSource.ErrorRaised.Add(fun a -> errToStr a |> TeamCityHelper.sendTeamCityError)

/// The ErrorLogFile
let ErrorLoggerFile = Path.Combine(Path.GetTempPath(), "Fake.Errors.txt")

/// TeamCity Logger for MSBuild
type ErrorLogger() = 
    inherit MSBuildLogger()
    let errors = new List<BuildErrorEventArgs>()
    override this.RegisterEvents(eventSource) = 
        eventSource.BuildStarted.Add(fun _ -> 
            let fi = fileInfo ErrorLoggerFile
            if fi.Exists then fi.Delete())
        eventSource.ErrorRaised.Add(fun a -> errors.Add a)
        eventSource.BuildFinished.Add(fun a -> 
            errors
            |> Seq.map errToStr
            |> fun e -> String.Join(Environment.NewLine, e)
            |> fun e -> 
                if a.Succeeded then ()
                else File.WriteAllText(ErrorLoggerFile, e))
