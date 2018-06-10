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
| None
with override x.ToString () = match x with | Tool -> "tool" | Project -> "project" | None -> "none"

let shouldSucceed message (r: ProcessResult) =
    Expect.isTrue r.OK (sprintf "%s. Results:\n:%A" message r)

let runTemplate rootDir kind = async {
    Directory.ensure rootDir
    let dotnet = DotNet.Options.Create().DotNetCliPath
    let proc = 
        Process.getProc (fun p ->
            p.WithWorkingDirectory(rootDir)
             .WithFileName(dotnet)
             .WithArguments(sprintf "new %s --bootstrap %s" templateName (string kind))
             .WithRedirectStandardInput(true)
             .WithRedirectStandardOutput(true)
             .WithRedirectStandardError(true)
        )
    
    proc.Start () |> ignore
    do! Async.Sleep 2000
    proc.StandardInput.WriteLine("Y")
    proc.WaitForExit ()
    Expect.equal proc.ExitCode 0 "should have run the template successfully"
}

let invokeScript dir scriptName =
    let fullScriptPath = Path.Combine(dir, scriptName)
    Process.execWithResult 
        (fun x -> 
            x.WithWorkingDirectory(dir)
             .WithFileName(fullScriptPath)
             .WithArguments "--help" ) (System.TimeSpan.FromSeconds 60.)
    |> shouldSucceed "should invoke the script file"

[<Tests>]
let tests =
    // we need to (uninstall) the template, install the packed version, and then execute that template
    testList "Fake.DotNet.Cli.IntegrationTests.Template tests" [
        testList "can install and run the template" [
            uninstallTemplate () |> shouldSucceed "should clear out preexisting templates"
            let templateNupkg = GlobbingPattern.create "../../template/fake-template/bin/Release/fake-template.*.nupkg" |> Seq.head
            installTemplateFrom templateNupkg |> shouldSucceed "should install new FAKE template"

            let scriptFile =
                if Environment.isUnix
                then "fake.sh"
                else "fake.cmd"

            yield testAsync "can install a project-style template" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                do! runTemplate tempDir Project
                invokeScript tempDir scriptFile
            }

            /// ignored because the .net tool install to a subdirectory is broken: https://github.com/fsharp/FAKE/pull/1989#issuecomment-396057330
            yield ptestAsync "can install a tool-style template" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                do! runTemplate tempDir Tool
                invokeScript tempDir scriptFile
            }
        ]
    ]
