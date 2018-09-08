
module Fake.Core.ProcessTests

open System
open Fake.Core
open Expecto
open FsCheck

let fsCheckConfig = { FsCheckConfig.defaultConfig with maxTest = 1000  }

[<Tests>]
let tests = 
  testList "Fake.Core.Process.Tests" [
    testPropertyWithConfig fsCheckConfig "toWindowsCommandLine is the inverse of fromWindowsCommandLine" <|
        fun (x: NonNull<string> list) ->
            let input = x |> List.map (fun (NonNull s) -> s)
            let escaped = Args.toWindowsCommandLine input
            let backAgain = Args.fromWindowsCommandLine escaped
            Expect.sequenceEqual backAgain input (sprintf "Expect argument lists to be equal, intermediate was '%s'" escaped)

    testCase "Test that we have a nice error message when a file doesn't exist" <| fun _ ->
        try
            Process.start(fun proc ->
                { proc with
                    FileName = "FileDoesntExist.exe"
                    Arguments = "arg1 arg2" })
                |> ignore
            Expect.isTrue false "Expected an exception"
        with e ->
            let s = e.Message.Contains "FileDoesntExist.exe"
            Expect.isTrue s ("Expected file-path as part of the message '" + e.Message + "'")
    
    testCase "Test that we can read messages correctly" <| fun _ ->
        let shell, command =
            if Environment.isWindows then
                "cmd", "/C \"echo 1&& echo 2\""
            else
                "sh", "-c \"echo '1'; echo '2'\""            
        let result =
            Process.execWithResult(fun proc ->
                    { proc with
                        FileName = shell
                        Arguments = command }) (TimeSpan.FromMinutes 1.)
      
        Expect.equal ["1"; "2"] result.Messages "Messages are not read correctly"

  ]
