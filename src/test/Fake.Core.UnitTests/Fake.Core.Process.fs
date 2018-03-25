
module Fake.Core.ProcessTests

open System
open Fake.Core
open Expecto
open Expecto.Flip


[<Tests>]
let tests = 
  testList "Fake.Core.Process.Tests" [
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
      
        Expect.equal "Messages are not read correctly" ["1"; "2"] result.Messages

  ]