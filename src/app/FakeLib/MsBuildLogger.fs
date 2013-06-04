module Fake.MsBuildLogger
open Fake
open Microsoft.Build
open Microsoft.Build.Framework
open System
open System.Collections.Generic
open System.IO

type MSBuildLogger () = 
    let mutable Verbosity = LoggerVerbosity.Normal
    let mutable Parameters = ""

    abstract member RegisterEvents : IEventSource -> unit
    default t.RegisterEvents e = ()

    member this.errToStr (a:BuildErrorEventArgs) = 
        sprintf "%s: %s(%d,%d): %s" a.Code a.File a.LineNumber a.ColumnNumber a.Message

    interface ILogger with
        member this.Parameters with get() = Parameters and set(value) = Parameters <- value
        member this.Verbosity with get() = Verbosity and set(value) = Verbosity <- value
        member this.Shutdown () = ()
        member this.Initialize(eventSource) = this.RegisterEvents(eventSource)

type TeamCityLogger () =
    inherit MSBuildLogger()
        override this.RegisterEvents(eventSource) = 
            eventSource.ErrorRaised.Add(fun a -> this.errToStr a |> TeamCityHelper.sendTeamCityError )

let ErrorLoggerFile = Path.Combine(Path.GetTempPath(), "Fake.Errors.txt")

type ErrorLogger () =
    inherit MSBuildLogger()

    let errors = new List<BuildErrorEventArgs>()

    
    override this.RegisterEvents(eventSource) = 
        eventSource.ErrorRaised.Add(fun a -> errors.Add a)

        eventSource.BuildFinished.Add(fun a ->
            let errMsg = 
                errors 
                |> Seq.map this.errToStr
                |> fun e -> String.Join(Environment.NewLine, e)
                |> fun e -> if a.Succeeded then "" else e
            File.WriteAllText(ErrorLoggerFile, errMsg)
        )
