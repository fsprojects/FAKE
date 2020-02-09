module Fake.DotNet.Cli.IntegrationTests.TemplateTests

open Expecto
open System
open System.IO

open Fake.Core
open Fake.DotNet
open Fake.IO

let templateProj = "fake-template.fsproj"
let templatePackageName = "fake-template"
let templateName = "fake"

//TODO: add DotNetCli helpers for the `new` command

let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

let inline opts () = DotNet.Options.lift dotnetSdk.Value

let inline dtntWorkDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd
    
let inline redirect () =
    DotNet.Options.lift (fun opts -> { opts with RedirectOutput = true })

let uninstallTemplate () =
    DotNet.exec (opts() >> redirect()) "new" (sprintf "-u %s" templatePackageName)

let installTemplateFrom pathToNupkg =
    DotNet.exec (opts() >> redirect()) "new" (sprintf "-i %s" pathToNupkg)

type BootstrapKind =
| Tool
| Local
| None
with override x.ToString () = match x with | Tool -> "tool"  | Local -> "local" | None -> "none"

type DslKind =
| Fake
| BuildTask
with override x.ToString () = match x with | Fake -> "fake" | BuildTask -> "buildtask"

type DependenciesKind =
| File
| Inline
| None
with override x.ToString () = match x with | File -> "file" | Inline -> "inline" | None -> "none"

let shouldSucceed message (r: ProcessResult) =
    let errorStr =
        r.Results
        |> Seq.map (fun r -> sprintf "%s: %s" (if r.IsError then "stderr" else "stdout") r.Message)
        |> fun s -> String.Join("\n", s)
    Expect.isTrue
        r.OK
        (sprintf 
            "%s. Exit code '%d'.\nDOTNET_ROOT: %s\nPATH: %s\n Results:\n%s\n"
            message r.ExitCode (Environment.GetEnvironmentVariable("DOTNET_ROOT"))
            (Environment.GetEnvironmentVariable "PATH") errorStr)

let timeout = (System.TimeSpan.FromMinutes 10.)

let runTemplate rootDir kind dependencies dsl =
    Directory.ensure rootDir
    try
        DotNet.exec (dtntWorkDir rootDir >> redirect()) "new" (sprintf "%s --allow-scripts yes --version 5.16.2-alpha.1304 --bootstrap %s --dependencies %s --dsl %s" templateName (string kind) (string dependencies) (string dsl))   
        |> shouldSucceed "should have run the template successfully"
    with e ->
        if e.Message.Contains "Command succeeded" && 
           e.Message.Contains "was created successfully" then
           printfn "Ignoring exit-code while template creation: %O" e
        else reraise()       


let invokeScript dir scriptName args =
    let fullScriptPath = Path.Combine(dir, scriptName)
    
    Process.execWithResult 
        (fun x -> 
            x.WithWorkingDirectory(dir)
             .WithFileName(fullScriptPath)
             .WithArguments args) timeout

let fileContainsText dir fileName text =
    let filePath = Path.Combine(dir, fileName)
    let content = File.ReadAllText(filePath)
    content.Contains(text: string)

let missingTarget targetName (r: ProcessResult) = 
    r.Errors |> Seq.exists (fun err -> err.Contains (sprintf "Target \"%s\" is not defined" targetName))

let tempDir() = Path.Combine("../../../test/fake-template", Path.GetRandomFileName())

let fileExists dir fileName = File.Exists(Path.Combine(dir, fileName))

[<Tests>]
let tests =
    // we need to (uninstall) the template, install the packed version, and then execute that template
    testList "Fake.DotNet.Cli.IntegrationTests.Template tests" [
        testList "can install and run the template" [
            Process.setEnableProcessTracing true            
            uninstallTemplate () |> shouldSucceed "should clear out preexisting templates"
            printfn "%s" Environment.CurrentDirectory
            
            DotNet.setupEnv dotnetSdk.Value
            let templateNupkg =
                GlobbingPattern.create "../../../release/dotnetcore/fake-template.*.nupkg"
                |> GlobbingPattern.setBaseDir __SOURCE_DIRECTORY__
                |> Seq.toList
                |> List.rev
                |> List.tryHead
            let installArgument =
                match templateNupkg with
                | Some t -> t
                | Option.None -> "fake-template"   
            installTemplateFrom installArgument |> shouldSucceed "should install new FAKE template"

            let scriptFile =
                if Environment.isUnix
                then "fake.sh"
                else "fake.cmd"

            let buildFile = "build.fsx"
            let dependenciesFile = "paket.dependencies"

            yield test "fails to build a target that doesn't exist" {
                let tempDir = tempDir()
                runTemplate tempDir Tool File Fake
                let result = invokeScript tempDir scriptFile "build -t Nonexistent"
                Expect.isFalse result.OK "the script should have failed"
                Expect.isTrue (missingTarget "Nonexistent" result) "The script should recognize the target doesn't exist"
            }            

            yield test "can install a inline-dependencies template" {
                let tempDir = tempDir()
                runTemplate tempDir Tool Inline Fake
                Expect.isTrue (fileContainsText tempDir buildFile "#r \"paket:") "the build file should contain inline dependencies"
                Expect.isFalse (fileExists tempDir dependenciesFile) "the dependencies file should not exist"
            }

            yield test "can install a buildtask-dsl file-dependencies template" {
                let tempDir = tempDir()
                runTemplate tempDir Tool File BuildTask
                Expect.isTrue (fileContainsText tempDir buildFile "open BlackFox.Fake") "the build file should contain blackfox"
                Expect.isTrue (fileContainsText tempDir dependenciesFile "nuget BlackFox.Fake.BuildTask") "the dependencies file should contain blackfox"
            }

            yield test "can build a buildtask-dsl file-dependencies template" {
                let tempDir = tempDir()
                runTemplate tempDir Tool File BuildTask
                invokeScript tempDir scriptFile "build -t All" |> shouldSucceed "should build successfully"
            }

            yield test "can install a buildtask-dsl inline-dependencies template" {
                let tempDir = tempDir()
                runTemplate tempDir Tool Inline BuildTask
                Expect.isTrue (fileContainsText tempDir buildFile "nuget BlackFox.Fake.BuildTask") "the build file should contain blackfox dependency"
            }

            yield test "can build a buildtask-dsl inline-dependencies template" {
                let tempDir = tempDir()
                runTemplate tempDir Tool Inline BuildTask
                invokeScript tempDir scriptFile "build -t All" |> shouldSucceed "should build successfully"
            }

            // Enable after https://github.com/fsharp/FAKE/pull/2403
            //yield test "can build with the local-style template" {
            //    let tempDir = tempDir()
            //    runTemplate tempDir Local Inline BuildTask
            //    invokeScript tempDir scriptFile "build -t All" |> shouldSucceed "should build successfully"
            //}

            /// ignored because the .net tool install to a subdirectory is broken: https://github.com/fsharp/FAKE/pull/1989#issuecomment-396057330
            yield ptest "can install a tool-style template" {
                let tempDir = tempDir()
                runTemplate tempDir Tool File Fake
                invokeScript tempDir scriptFile "--help" |> shouldSucceed "should invoke help"
            }
        ]
    ]
