module Fake.DotNet.MSBuildTests

open Fake.Core
open Fake.DotNet
open Expecto

[<Tests>]
let tests =
    let flagsTestCase name changeBuildArgs expected =
        testCase name
        <| fun _ ->
            let _, cmdLine = MSBuild.buildArgs changeBuildArgs

            let expected =
                if BuildServer.ansiColorSupport then
                    $"%s{expected} /clp:ForceConsoleColor".Trim()
                else
                    expected.Trim()

            let expected = $"/m /nodeReuse:False {expected} /p:RestorePackages=False".Trim()

            Expect.equal cmdLine expected $"Expected a given cmdLine '{expected}', but got '{cmdLine}'."

    testList
        "Fake.DotNet.MSBuild.Tests"
        [ testCase "Test that we can create simple msbuild cmdline"
          <| fun _ ->
              let _, cmdLine =
                  MSBuild.buildArgs (fun defaults ->
                      { defaults with
                          ConsoleLogParameters = []
                          Properties = [ "OutputPath", "C:\\Test\\" ] })

              let expected =
                  "/m /nodeReuse:False /p:RestorePackages=False /p:OutputPath=C:%5CTest%5C"

              Expect.equal cmdLine expected "Expected a given cmdline."
          testCase "Test that /restore is included #2160"
          <| fun _ ->
              let _, cmdLine =
                  MSBuild.buildArgs (fun defaults ->
                      { defaults with
                          ConsoleLogParameters = []
                          DoRestore = true })

              let expected = "/restore /m /nodeReuse:False /p:RestorePackages=False"

              Expect.equal cmdLine expected "Expected a given cmdline."

          flagsTestCase "/tl:auto doesn't ouput anything (1)" id ""
          flagsTestCase
              "/tl:auto doesn't ouput anything (2)"
              (fun args -> { args with TerminalLogger = MSBuildTerminalLoggerOption.Auto })
              ""
          flagsTestCase
              "/tl:on does ouput"
              (fun args -> { args with TerminalLogger = MSBuildTerminalLoggerOption.On })
              "/tl:on"
          flagsTestCase
              "/tl:off does ouput"
              (fun args -> { args with TerminalLogger = MSBuildTerminalLoggerOption.Off })
              "/tl:off" ]
