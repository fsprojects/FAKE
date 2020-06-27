module Fake.DotNet.NuGetTests

open Fake.Core
open Fake.DotNet.NuGet
open Fake.DotNet.NuGet.Version
open Expecto

[<Tests>]
let tests =
    testList "Fake.DotNet.NuGet.Tests" [
        testCase "Test that the default nuget push arguments returns empty string" <| fun _ ->
            let cli =
                NuGet.NuGetPushParams.Create() 
                |> NuGet.toPushCliArgs
                |> Args.toWindowsCommandLine
                  
            Expect.isEmpty cli "Empty push args."
          
        testCase "Test that the nuget push arguments with all params set returns correct string" <| fun _ ->
            let param =
                { NuGet.NuGetPushParams.Create() with
                    DisableBuffering = true
                    ApiKey = Some "abc123"
                    NoSymbols = true
                    NoServiceEndpoint = true
                    Source = Some "MyNuGetSource"
                    SymbolApiKey = Some "MySymbolApiKey"
                    SymbolSource = Some "MySymbolSource"
                    Timeout = Some <| System.TimeSpan.FromMinutes 6.00001
                    PushTrials = 5 }
          
            let cli =
                param
                |> NuGet.toPushCliArgs
                |> Args.toWindowsCommandLine
    
            let expected = "-ApiKey abc123 -DisableBuffering -NoSymbols -NoServiceEndpoint -Source MyNuGetSource -SymbolApiKey MySymbolApiKey -SymbolSource MySymbolSource -Timeout 360"
                
            Expect.equal cli expected "Push args generated correctly."
        
        test "Incrementing Patch for a SemVerInfo" {
            Expect.equal (SemVer.parse("1.1.0") |> IncPatch |> string) "1.1.1" "Incremented Patch from 1.1.0 should be 1.1.1"
        }

        test "Incrementing Minor for a SemVerInfo" {
            Expect.equal (SemVer.parse("1.1.1") |> IncMinor |> string) "1.2.0" "Incremented Minor from 1.1.1 should be 1.2.0"
        }

        test "Incrementing Major for a SemVerInfo" {
            Expect.equal (SemVer.parse("1.1.1") |> IncMajor |> string) "2.0.0" "Incremented Patch from 1.1.1 should be 2.0.0"
        }
    ]

