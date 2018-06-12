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

let runTemplate rootDir kind =
    Directory.ensure rootDir
    let dotnet = DotNet.Options.Create().DotNetCliPath
    Process.execWithResult (fun p ->
        p.WithWorkingDirectory(rootDir)
         .WithFileName(dotnet)
         .WithArguments(sprintf "new %s --allow-scripts yes --bootstrap %s" templateName (string kind))) (System.TimeSpan.FromSeconds 60.)
    |> shouldSucceed "should have run the template successfully"

let invokeScript dir scriptName args =
    let fullScriptPath = Path.Combine(dir, scriptName)
    Process.execWithResult 
        (fun x -> 
            x.WithWorkingDirectory(dir)
             .WithFileName(fullScriptPath)
             .WithArguments args ) (System.TimeSpan.FromSeconds 60.)

let missingTarget targetName (r: ProcessResult) = 
    r.Errors |> Seq.exists (fun err -> err.Contains (sprintf "Target \"%s\" is not defined" targetName))

[<Tests>]
let tests =
    // we need to (uninstall) the template, install the packed version, and then execute that template
    testList "Fake.DotNet.Cli.IntegrationTests.Template tests" [
        testList "can install and run the template" [
            uninstallTemplate () |> shouldSucceed "should clear out preexisting templates"
            printfn "%s" Environment.CurrentDirectory
            let templateNupkg = GlobbingPattern.create "../../../nuget/dotnetcore/fake-template.*.nupkg" |> GlobbingPattern.setBaseDir __SOURCE_DIRECTORY__ |> Seq.head
            installTemplateFrom templateNupkg |> shouldSucceed "should install new FAKE template"

            let scriptFile =
                if Environment.isUnix
                then "fake.sh"
                else "fake.cmd"

            yield test "can install a project-style template" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                runTemplate tempDir Project
                invokeScript tempDir scriptFile "--help" |> shouldSucceed "should invoke help"
            }

            yield test "can build with the project-style template" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                runTemplate tempDir Project
                invokeScript tempDir scriptFile "build -t All" |> shouldSucceed "should build successfully"
            }

            yield test "fails to build a target that doesn't exist" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                runTemplate tempDir Project
                let result = invokeScript tempDir scriptFile "build -t Nonexistent"
                Expect.isFalse result.OK "the script should have failed"
                Expect.isTrue (missingTarget "Nonexistent" result) "The script should recognize the target doesn't exist"
            }

            /// ignored because the .net tool install to a subdirectory is broken: https://github.com/fsharp/FAKE/pull/1989#issuecomment-396057330
            yield ptest "can install a tool-style template" {
                let tempDir = Path.Combine(Path.GetTempPath (), Path.GetRandomFileName())
                Directory.ensure tempDir
                runTemplate tempDir Tool
                invokeScript tempDir scriptFile "--help" |> shouldSucceed "should invoke help"
            }
        ]
    ]
