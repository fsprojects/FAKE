# Starting processes in "FAKE - F# Make"

**Note:  This documentation is for FAKE 5! **

[API-Reference](apidocs/fake-core-process.html)

## Running a command and analyse results

```fsharp

let fakeToolPath = "known/path/to/fake.exe"
let directFakeInPath command workingDir target =
    let result =
        Process.execWithResult (fun (info:ProcStartInfo) ->
          { info with
                FileName = fakeToolPath
                WorkingDirectory = workingDir
                Arguments = command }
          |> Process.setEnvironmentVariable "target" target) (System.TimeSpan.FromMinutes 15.)
    if result.ExitCode <> 0 then
        let errors = String.Join(Environment.NewLine,result.Errors)
        printfn "%s" <| String.Join(Environment.NewLine,result.Messages)
        failwithf "FAKE Process exited with %d: %s" result.ExitCode errors
    String.Join(Environment.NewLine,result.Messages)

let output = directFakeInPath "--version" "." "All"

```

