# Debugging of FAKE 5 build scripts

Currently debugging support (and documentation around it) is limited. Please help to improve the situation by improving the code and the docs!

## General considerations

- Run with more verbose logging `-v`
- If an error happens while restoring packages (before even running the script), consider using `-vv` or `-v -v` to increase the logging even more.

## Visual Studio Code && portable.zip

Debugging works (on windows) in the following way:

- Download the portable fake distribution `fake-portable.zip` and extract it somewhere (for example `E:\fake-portable`)
- Open Visual Studio Code
- Open "The Debugger" view and add the following configuration

  ```json
        {
            "name": "Debug My Build Script",
            "type": "coreclr",
            "request": "launch",
            "program": "E:\\fake-portable\\fake.dll",
            "args": ["run", "build.fsx", "--fsiargs", "--debug:portable --optimize-"],
            "cwd": "${workspaceRoot}",
            "stopAtEntry": false,
            "console": "internalConsole"
        }
  ```

  > It is important to specify `--debug:portable --optimize-`

- Delete the `.fake` directory
- Set a breakpoint in your script and run the new configuration

## Visual Studio Code && `dotnet fake`

Add the following lines to your build script:

```fsharp
printfn "Press any key to continue..."
System.Console.ReadKey() |> ignore
```

- Delete `.fake` directory
- Start your build script via `dotnet fake run build.fsx --fsiargs "--debug:portable --optimize-"` and wait for the `Press any key to continue...` Message
- Select ".NET Core Attach" in the Visual Studio Code Debugger View
- Press play and select the `dotnet exec --depsfile ".../fake..."` process.

