module Fake.DotNet.NuGetTests

open Fake.Core
open Fake.DotNet.NuGet
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
    ]

