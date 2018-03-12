/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO


module TeamCity =
    /// Implements a TraceListener for TeamCity build servers.
    /// ## Parameters
    ///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
    ///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
    type internal TeamCityTraceListener(importantMessagesToStdErr, colorMap) =

        interface ITraceListener with
            /// Writes the given message to the Console.
            member this.Write msg = 
                let color = colorMap msg
                match msg with
                | StartMessage -> ()
                | OpenTag (tag, description) ->
                    TeamCityWriter.sendOpenBlock tag.Name (sprintf "%s: %s" tag.Type description)
                | CloseTag (tag) ->
                    TeamCityWriter.sendCloseBlock tag.Name
                | ImportantMessage text | ErrorMessage text ->
                    ConsoleWriter.write importantMessagesToStdErr color true text
                | LogMessage(text, newLine) | TraceMessage(text, newLine) ->
                    ConsoleWriter.write false color newLine text
                | FinishedMessage -> ()

    let defaultTraceListener =
      TeamCityTraceListener(false, ConsoleWriter.colorMap) :> ITraceListener
    let detect () =
        BuildServer.buildServer = BuildServer.TeamCity
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-TeamCity environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        () 
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }