module Fake.MsBuildLogger
open Fake
open Microsoft.Build
open Microsoft.Build.Framework
open System
open System.Collections.Generic

type TeamCityLogger () =
    let mutable Verbosity = LoggerVerbosity.Normal
    let mutable Parameters = ""

    interface ILogger with
        member this.Parameters with get() = Parameters and set(value) = Parameters <- value
        member this.Verbosity with get() = Verbosity and set(value) = Verbosity <- value
        member this.Shutdown () = ()
        member this.Initialize(eventSource) = 
            eventSource.ErrorRaised.Add(fun a ->
                let str = sprintf "%s(%d,%d): ERROR %s: `%s` [%s]" a.File a.LineNumber a.ColumnNumber a.Code a.Message a.ProjectFile
                printfn "==> %s" str
                str |> TeamCityHelper.sendTeamCityError
            )
