module Fake.DotNet.sdkAssemblyResolverTests

open System.Text.RegularExpressions
open Fake.IO
open Fake.DotNet
open System
open System.IO
open Fake.IO.FileSystemOperators
open Expecto
open Fake.Core.IntegrationTests.TestHelpers

[<Tests>]
let tests =
    testList
        "Fake.DotNet.sdkAssemblyResolverTests"
        [ testCase "Runner run script with NETStandard2.0 SDK assemblies"
          <| fun _ ->
              let result =
                  handleAndFormat
                  <| fun _ ->
                      fakeRunAndCheck
                          Ctx.Verbose
                          "reference_fake-runtime.fsx"
                          "reference_fake-runtime.fsx"
                          "core-runtime-reference-assemblies-netstandard20"

              let stdOut =
                  String.Join("\n", result.Messages).Trim()

              let expectedNetStandardPathPortion =
                  ".nuget"
                  </> "packages"
                  </> "netstandard.library"
                  </> "2.0.0"

              (sprintf "stdout should contain '%s', but was: '%s'" expectedNetStandardPathPortion stdOut)
              |> Expect.isTrue (stdOut.Contains expectedNetStandardPathPortion)

          testCase "Runner run script with .Net6 SDK assemblies"
          <| fun _ ->
              let result =
                  handleAndFormat
                  <| fun _ ->
                      fakeRunAndCheck
                          Ctx.Verbose
                          "reference_fake-runtime.fsx"
                          "reference_fake-runtime.fsx"
                          "core-runtime-reference-assemblies-net60"

              let stdOut =
                  String.Join("\n", result.Messages).Trim()

              // the * is for runtime version
              let expectedNet6PathPortion =
                  "[\s\S]*[\/|\\\\]packs[\/|\\\\]Microsoft[\/.|\\\\.]NETCore[\/.|\\\\.]App[\/.|\\\\.]Ref[\/|\\\\][\s\S]*[\/|\\\\]ref[\/|\\\\]net6[\/.|\\\\.]0[\/|\\\\]*"

              (sprintf
                  "stdout should contain path like '%s', but was: '%s'"
                  "packs/Microsoft.NETCore.App.Ref/*/ref/net6.0"
                  stdOut)
              |> Expect.isTrue (Regex.IsMatch(stdOut, expectedNet6PathPortion)) ]
