module Fake.DotNet.MSBuildTests

open Fake.Core
open Fake.DotNet
open Expecto

[<Tests>]
let tests =
  testList "Fake.DotNet.MSBuild.Tests" [
    testCase "Test that we can create simple msbuild cmdline" <| fun _ ->
      let _, cmdLine =
        MSBuild.buildArgs (fun defaults ->
          { defaults with
              ConsoleLogParameters = []
              Properties = ["OutputPath", "C:\\Test\\"] })
      let expected =
        if Environment.isUnix then "/p:RestorePackages=False /p:OutputPath=C:%5CTest%5C"    
        else "/m /nodeReuse:False /p:RestorePackages=False /p:OutputPath=C:%5CTest%5C"    
      Expect.equal cmdLine expected "Expected a given cmdline."
    testCase "Test that /restore is included #2160" <| fun _ ->
      let _, cmdLine =
        MSBuild.buildArgs (fun defaults ->
          { defaults with
              ConsoleLogParameters = []
              DoRestore = true })
      let expected =
        if Environment.isUnix then "/restore /p:RestorePackages=False"    
        else "/restore /m /nodeReuse:False /p:RestorePackages=False"    
      Expect.equal cmdLine expected "Expected a given cmdline."
  ]
