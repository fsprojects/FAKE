/// Contains support for various build servers
namespace Fake.BuildServer

open System.IO
open Fake.Core
open Fake.IO

[<AutoOpen>]
module GitLabImportExtensions =
    type DotNetCoverageTool with
        member x.GitLabName =
            match x with
            | DotNetCoverageTool.DotCover -> "dotcover"
            | DotNetCoverageTool.PartCover -> "partcover"
            | DotNetCoverageTool.NCover -> "ncover"
            | DotNetCoverageTool.NCover3 -> "ncover3"

    type ImportData with
        member x.GitLabName =
            match x with
            | ImportData.BuildArtifactWithName _
            | ImportData.BuildArtifact -> "buildArtifact"
            | ImportData.DotNetCoverage _ -> "dotNetCoverage"
            | ImportData.DotNetDupFinder -> "DotNetDupFinder"
            | ImportData.PmdCpd -> "pmdCpd"
            | ImportData.Pmd -> "pmd"
            | ImportData.ReSharperInspectCode -> "ReSharperInspectCode"
            | ImportData.Jslint -> "jslint"
            | ImportData.FindBugs -> "findBugs"
            | ImportData.Checkstyle -> "checkstyle"
            | ImportData.Gtest -> "gtest"
            | ImportData.Surefire -> "surefire"
            | ImportData.FxCop -> "FxCop"
            | ImportData.Mstest -> "mstest"
            | ImportData.Nunit NunitDataVersion.Nunit -> "nunit"
            | ImportData.Nunit NunitDataVersion.Nunit3 -> "nunit3"
            | ImportData.Junit -> "junit"
            | ImportData.Xunit -> "xunit"

[<RequireQualifiedAccess>]
module GitLab =

    type Environment =
        static member CommitSha = Environment.environVar "CI_COMMIT_SHA"
        static member CommitRefName = Environment.environVar "CI_COMMIT_REF_NAME"
        static member PipelineId = Environment.environVar "CI_PIPELINE_ID"

    /// Implements a TraceListener for TeamCity build servers.
    /// ## Parameters
    ///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
    ///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
    type internal GitLabTraceListener() =

        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg = 
                let color = ConsoleWriter.colorMap msg
                let importantMessagesToStdErr = true
                let write = ConsoleWriter.writeAnsiColor //else ConsoleWriter.write
                match msg with
                | TraceData.ImportantMessage text | TraceData.ErrorMessage text ->
                    write importantMessagesToStdErr color true text
                | TraceData.LogMessage(text, newLine) | TraceData.TraceMessage(text, newLine) ->
                    write false color newLine text
                | TraceData.OpenTag (tag, descr) ->
                    match descr with
                    | Some d -> write false color true (sprintf "Starting %s '%s': %s" tag.Type tag.Name d)
                    | _ -> write false color true (sprintf "Starting %s '%s'" tag.Type tag.Name)  
                | TraceData.CloseTag (tag, time, state) ->
                    write false color true (sprintf "Finished (%A) '%s' in %O" state tag.Name time)
                | TraceData.BuildState state ->
                    write false color true (sprintf "Changing BuildState to: %A" state)
                | TraceData.ImportData (typ, path) ->
                    let name = Path.GetFileName path
                    let target = Path.Combine("artifacts", name)
                    let targetDir = Path.GetDirectoryName target
                    Directory.ensure targetDir
                    Shell.cp_r path target
                    write false color true (sprintf "Import data '%O': %s -> %s" typ path target)
                | TraceData.TestOutput (test, out, err) ->
                    write false color true (sprintf "Test '%s' output:\n\tOutput: %s\n\tError: %s" test out err)
                | TraceData.BuildNumber number ->
                    write false color true (sprintf "Build Number: %s" number)
                | TraceData.TestStatus (test, status) ->
                    write false color true (sprintf "Test '%s' status: %A" test status)

    let defaultTraceListener =
      GitLabTraceListener() :> ITraceListener
    let detect () =
        BuildServer.buildServer = BuildServer.GitLabCI
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-AppVeyor environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        () 
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }
