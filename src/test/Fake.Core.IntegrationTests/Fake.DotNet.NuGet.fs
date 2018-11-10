module Fake.DotNet.NuGetIntegrationTests

open Fake.IO
open Fake.Core
open Fake.DotNet
open System.IO
open Expecto

[<Tests>]
let tests =
    testList "Fake.DotNet.NuGetIntegrationTests" [
        testCase "getLastNuGetVersion works - #2124" <| fun _ ->
            match NuGet.Version.getLastNuGetVersion "https://www.nuget.org/api/v2" "Dotnet.ProjInfo.Matthid" with
            | None -> failwithf "Expected to retrieve version for package but got 'None'"
            | Some v ->
                Expect.equal (SemVer.parse "1.0.0") v "Expected 1.0.0"
        testCase "getLastNuGetVersion works with myget - #2124" <| fun _ ->
            match NuGet.Version.getLastNuGetVersion "https://www.myget.org/F/fake/api/v2" "Dotnet.ProjInfo.Matthid" with
            | None -> failwithf "Expected to retrieve version for package but got 'None'"
            | Some v ->
                Expect.equal (SemVer.parse "1.0.0") v "Expected 1.0.0"
    ]
