module Fake.DotNet.Cli.IntegrationTests.TemplateTests

open Expecto
open System
open System.IO

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing

let templateProj = "fake-template.fsproj"
let templatePackageName = "fake-template"
let templateName = "fake"

//TODO: add DotNetCli helpers for the `new` command

let uninstallTemplate () =
    DotNet.exec id "new" (sprintf "-u %s" templatePackageName)

let installTemplateFrom pathToNupkg =
    DotNet.exec id "new" (sprintf "-i %s" pathToNupkg)

type BootstrapKind =
| Tool
| Project
with override x.ToString () = match x with | Tool -> "tool" | Project -> "project"

let isFAKEHelpOutput (r: ProcessResult) =
    r.ExitCode = 1 && r.Messages |> List.exists (fun m -> m.Contains("fake"))

let shouldSucceed message (r: ProcessResult) =
    Expect.isTrue (r.OK || isFAKEHelpOutput r) (sprintf "%s. Messages:\n:%A" message r)

let runTemplate rootDir kind =
    Directory.ensure rootDir
    let inDir = DotNet.Options.withWorkingDirectory rootDir
    DotNet.exec inDir "new" (sprintf "%s --bootstrap %s" templateName (string kind))

let invokeScript dir scriptName =
    let fullScriptPath = Path.Combine(dir, scriptName)
    Process.execWithResult (fun x -> x.WithWorkingDirectory(dir).WithFileName(fullScriptPath)) (System.TimeSpan.FromSeconds 60.)
    |> shouldSucceed "should invoke the script file"

[<Tests>]
let tests =
    // we need to (uninstall) the template, install the packed version, and then execute that template
    testList "Fake.DotNet.Cli.IntegrationTests.Template tests" [
        // TODO: entire test list is pending because it won't run interactively, because it needs user input :-/
        ptestList "can install and run the template" [
            uninstallTemplate () |> shouldSucceed "should clear out preexisting templates"
            let templateNupkg = GlobbingPattern.create "../../template/fake-template/bin/Release/fake-template.*.nupkg" |> Seq.head
            installTemplateFrom templateNupkg |> shouldSucceed "should install new FAKE template"

            let scriptFile =
                if Environment.isUnix
                then "fake.sh"
                else "fake.cmd"

            yield test "can install a project-style template" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                do runTemplate tempDir Project |> shouldSucceed "should run the template"
                invokeScript tempDir scriptFile
            }

            /// ignored because the .net tool install to a subdirectory is broken: https://github.com/fsharp/FAKE/pull/1989#issuecomment-396057330
            yield ptest "can install a tool-style template" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                do runTemplate tempDir Tool |> shouldSucceed "should run the template"
                invokeScript tempDir scriptFile
            }
        ]
    ]
