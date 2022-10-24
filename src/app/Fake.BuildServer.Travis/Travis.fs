namespace Fake.BuildServer

open Fake.Core

/// <summary>
/// Native support for Travis specific APIs.
/// </summary>
/// <remarks>
/// The general documentation on how to use CI server integration can be found <a href="/guide/buildserver.html">here</a>.
/// This module does not provide any special APIs please use FAKE APIs and they should integrate into this CI server.
/// If some integration is not working as expected or you have features you would like to use directly please open an issue.
/// </remarks>
[<RequireQualifiedAccess>]
module Travis =
    /// <summary>
    /// Implements a TraceListener for Travis build servers.
    /// </summary>
    /// [omit]
    type internal TravisTraceListener() =
        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg =
                let color = ConsoleWriter.colorMap msg
                let write = ConsoleWriter.writeAnsiColor

                match msg with
                | TraceData.ImportantMessage text
                | TraceData.ErrorMessage text -> write true color true text
                | TraceData.LogMessage (text, newLine)
                | TraceData.TraceMessage (text, newLine) -> write false color newLine text
                | TraceData.OpenTag (tag, descr) ->
                    match descr with
                    | Some d -> write false color true (sprintf "Starting %s '%s': %s" tag.Type tag.Name d)
                    | _ -> write false color true (sprintf "Starting %s '%s'" tag.Type tag.Name)
                | TraceData.CloseTag (tag, time, state) ->
                    write false color true (sprintf "Finished (%A) '%s' in %O" state tag.Name time)
                | TraceData.ImportData (typ, path) -> write false color true (sprintf "Import data '%O': %s" typ path)
                | TraceData.TestOutput (test, out, err) ->
                    write false color true (sprintf "Test '%s' output:\n\tOutput: %s\n\tError: %s" test out err)
                | TraceData.BuildNumber number -> write false color true (sprintf "Build Number: %s" number)
                | TraceData.TestStatus (test, status) ->
                    write false color true (sprintf "Test '%s' status: %A" test status)
                | TraceData.BuildState (state, _) -> write false color true (sprintf "Build State: %A" state)

    /// [omit]
    let defaultTraceListener = TravisTraceListener() :> ITraceListener

    /// [omit]
    let detect () = BuildServer.buildServer = Travis

    /// [omit]
    let install (force: bool) =
        if not (detect ()) then
            failwithf "Cannot run 'install()' on a non-Travis environment"

        if force || not (CoreTracing.areListenersSet ()) then
            CoreTracing.setTraceListeners [ defaultTraceListener ]

        ()

    /// [omit]
    let Installer =
        { new BuildServerInstaller() with
            member __.Install() = install (false)
            member __.Detect() = detect () }
