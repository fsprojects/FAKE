module Fake.Net.SSHTests

open Expecto
open Fake.Net

[<Tests>]
let tests =
  testList "Fake.Net.SSH.Tests" [
    testCase "Test all arguments are mapped correctly" <| fun _ ->
      let args: SSH.SSHParams = 
        { ToolPath = "ssh"
          RemoteUser = "fake-user"
          RemoteHost = "localhost"
          RemotePort = "22"
          PrivateKeyPath = "private-key-path" }
      let sshCommand = "pwd"
      let cmd = SSH.buildArguments args sshCommand

      Expect.equal cmd "-i \"private-key-path\" fake-user@localhost pwd" "expected proper arguments formatting"
      
    testCase "Test ssh target is mapped correctly when a custom port is used" <| fun _ ->
      let args: SSH.SSHParams = 
        { ToolPath = "ssh"
          RemoteUser = "fake-user"
          RemoteHost = "localhost"
          RemotePort = "2222"
          PrivateKeyPath = null }
      let sshCommand = "pwd"
      let cmd = SSH.buildArguments args sshCommand

      Expect.equal cmd "fake-user@localhost:2222 pwd" "expected proper arguments formatting"
    
    testCase "Test PrivateKeyPath is mapped correctly when it's empty" <| fun _ ->
      let args: SSH.SSHParams = 
        { ToolPath = "ssh"
          RemoteUser = "fake-user"
          RemoteHost = "localhost"
          RemotePort = "22"
          PrivateKeyPath = null }
      let sshCommand = "pwd"
      let cmd = SSH.buildArguments args sshCommand

      Expect.equal cmd "fake-user@localhost pwd" "expected proper arguments formatting"
  ]
