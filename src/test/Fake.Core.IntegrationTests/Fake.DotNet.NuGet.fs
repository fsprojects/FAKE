module Fake.DotNet.NuGetIntegrationTests

open Fake.IO
open Fake.Core
open Fake.DotNet
open System.IO
open Expecto

[<Tests>]
let tests =
    testList "Fake.DotNet.NuGetIntegrationTests" [
        testCase "getLastNuGetVersion works - #2294" <| fun _ ->
            // Unlikely to ever get any new updates
            match NuGet.Version.getLastNuGetVersion "https://api.nuget.org/v3/index.json" "Yaaf.AdvancedBuilding" with
            | None -> failwithf "Expected to retrieve version for package but got 'None'"
            | Some v ->
                Expect.equal v (SemVer.parse "0.14.1") "Expected 0.14.1"
        testCase "getLastNuGetVersion works - #2124" <| fun _ ->
            match NuGet.Version.getLastNuGetVersion "https://www.nuget.org/api/v2" "Dotnet.ProjInfo.Matthid" with
            | None -> failwithf "Expected to retrieve version for package but got 'None'"
            | Some v ->
                Expect.equal v (SemVer.parse "1.0.0") "Expected 1.0.0"
        testCase "getLastNuGetVersion works with myget - #2124" <| fun _ ->
            match NuGet.Version.getLastNuGetVersion "https://www.myget.org/F/fake/api/v2" "Dotnet.ProjInfo.Matthid" with
            | None -> failwithf "Expected to retrieve version for package but got 'None'"
            | Some v ->
                Expect.equal v (SemVer.parse "1.0.0") "Expected 1.0.0"
    ]
