module Fake.DotNet.CliTests

open Fake.Core
open Fake.DotNet
open Expecto

[<Tests>]
let tests =
    testList "Fake.DotNet.Cli.Tests" [
        testCase "Test that we can use Process-Helpers on Cli Paramters" <| fun _ ->
            let cli =
                DotNet.Options.Create()
                |> Process.setEnvironmentVariable "Somevar" "someval"

            Expect.equal cli.Environment.["Somevar"] "someval" "Retrieving the correct environment variable failed."

        testCase "Test that the default dotnet nuget push arguments returns empty string" <| fun _ ->
            let cli =
                DotNet.NuGetPushOptions.Create().PushParams
                |> DotNet.buildNugetPushArgs
                |> Args.toWindowsCommandLine
    
                  
            Expect.equal cli "" "Empty push args."
          
        testCase "Test that the dotnet nuget push arguments with all params setreturns correct string" <| fun _ ->
            let param =
                { DotNet.NuGetPushOptions.Create().PushParams with
                    DisableBuffering = true
                    ApiKey = Some "abc123"
                    NoSymbols = true
                    NoServiceEndpoint = true
                    Source = Some "MyNuGetSource"
                    SymbolApiKey = Some "MySymbolApiKey"
                    SymbolSource = Some "MySymbolSource"
                    Timeout = Some <| System.TimeSpan.FromMinutes 5.0 }
          
            let cli =
                param
                |> DotNet.buildNugetPushArgs
                |> Args.toWindowsCommandLine
    
            let expected = "--disable-buffering --api-key abc123 --no-symbols --no-service-endpoint --source MyNuGetSource --symbol-api-key MySymbolApiKey --symbol-source MySymbolSource --timeout 300"
                
            Expect.equal cli expected "Push args generated correctly."
    ]
