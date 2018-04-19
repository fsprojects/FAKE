# Debugging of FAKE 5 build scripts

Currently debugging support (and documentation around it) is limited. Please help to improve the situation by improving the code and the docs!

> Currently debugging via the `chocolatey` installation is not possible. This is because we currently do not distribute the x64 version on x64 versions of windows and the .NET Core debugger currently only supports x64!

## General considerations

- Run with more verbose logging `-v`
- If an error happens while restoring packages (before even running the script), consider using `-vv` or `-v -v` to increase the logging even more.

## Visual Studio Code && portable.zip

Debugging works (on windows) in the following way:

- Download the portable fake distribution `fake-dotnetcore-portable.zip` and extract it somewhere (for example `E:\fake-dotnetcore-portable`)
- Open Visual Studio Code
- Open "The Debugger" view and add the following configuration

  ```json
        {
            "name": "Debug My Build Script",
            "type": "coreclr",
            "request": "launch",
            "program": "E:\\fake-dotnetcore-portable\\fake.dll",
            "args": ["run", "--fsiargs", "--debug:portable --optimize-", "build.fsx"],
            "cwd": "${workspaceRoot}",
            "stopAtEntry": false,
            "console": "internalConsole"
        }
  ```

  > It is important to specify `--debug:portable --optimize-`<br>
  > To get debugging support for .NET Core you need [C# for Visual Studio Code](https://github.com/OmniSharp/omnisharp-vscode) 

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

## Run script without fake.exe (via fsi)

FAKE 5 scripts are not hardwired to the new FAKE 5 runner (even if they look like with the new header). To make them work with FSI all you need to do is to setup a "FAKE-Context":

```fsharp
// Regular header and `#load ".fake/build.fsx/intellisense.fsx"`

#if !FAKE
let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
#endif

// Your Script code...
```

With these lines you can just run `fsi build.fsx`

You can use this "trick" to run the script via the regular IDE buttons.

To start a new context or cleanup the old one you can use (execute interactively):

```fsharp
execContext.Dispose()
let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
```
