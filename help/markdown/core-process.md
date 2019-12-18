# Starting processes in "FAKE - F# Make"

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later.</p>
</div>

[API-Reference CreateProcess](apidocs/v5/fake-core-createprocess.html)
[API-Reference Proc](apidocs/v5/fake-core-proc.html)
[API-Reference Process](apidocs/v5/fake-core-process.html)

## Just start a process

You can either use a list of arguments:

```fsharp

CreateProcess.fromRawCommand "./folder/mytool.exe" ["arg1"; "arg2"]
|> Proc.run // start with the above configuration
|> ignore // ignore exit code

// Or

[ "arg1"; "arg2"
  "arg3" ]
|> CreateProcess.fromRawCommand "./folder/mytool.exe"
|> CreateProcess.withWorkingDirectory "./folder"
|> Proc.run
|> ignore

```

Or a properly escaped command line:

```fsharp

CreateProcess.fromRawCommandLine "./folder/mytool.exe" "arg1 arg2 arg3"
|> Proc.run // start with the above configuration
|> ignore // ignore exit code

```

Or use some FAKE helpers:

```fsharp
open Fake.Core

Arguments.Empty
|> Arguments.appendIf true "-Verbose"
|> Arguments.appendNotEmpty "-Channel" channelParamValue
|> Arguments.appendNotEmpty "-Version" versionParamValue
|> Arguments.appendOption "-Architecture" architectureParamValue
|> Arguments.appendNotEmpty "-InstallDir" (defaultArg param.CustomInstallDir defaultUserInstallDir)
|> Arguments.appendIf param.DebugSymbols "-DebugSymbols"
|> Arguments.appendIf param.DryRun "-DryRun"
|> Arguments.appendIf param.NoPath "-NoPath"
```

Or use helper libraries like [`BlackFox.CommandLine`](https://github.com/vbfox/FoxSharp/tree/master/src/BlackFox.CommandLine):

```fsharp

open BlackFox.CommandLine

CmdLine.empty
|> CmdLine.append "build"
|> CmdLine.appendIf noRestore "--no-restore"
|> CmdLine.appendPrefixIfSome "--framework" framework
|> CmdLine.appendPrefixf "--configuration" "%A" configuration
|> CmdLine.toString
|> CreateProcess.fromRawCommandLine "dotnet.exe"
|> Proc.run
|> ignore
```

## Evaluate exit code

The most obvious way is:

```fsharp
let result =
    [ "arg1"; "arg2"; "arg3" ]
    |> CreateProcess.fromRawCommand "./folder/mytool.exe"
    |> Proc.run

if result.ExitCode <> 0 then failwith "Command failed"

```

To simplify your life you can "embed" the return code check into the `CreateProcess` instance (which allows to pass the instance through your application and fail appropriately):

```fsharp

[ "arg1"; "arg2"; "arg3" ]
|> CreateProcess.fromRawCommand "./folder/mytool.exe"
|> CreateProcess.ensureExitCode // will make sure to throw on error
|> Proc.run
|> ignore

// The above is roughly equivalent to the following (which you can copy and edit to customize):

[ "arg1"; "arg2"; "arg3" ]
|> CreateProcess.fromRawCommand "./folder/mytool.exe"
|> CreateProcess.addOnExited (fun data exitCode ->
    if exitCode <> 0 then
        // TODO: throw your own exception here
        failwithf "Process exit code '%d' <> 0. Command Line: %s" exitCode r.CommandLine
    else
        data)
|> Proc.run
|> ignore

```

## Different ways to "run" or "start"

The basic difference between "start" and "run" is:

- "start" starts the process and returns after the process is started. This usually means the process is still running.
- "run" waits for the started process to exit and returns the result. 

The different ways to start or run a process are documented [here](/apidocs/v5/fake-core-proc.html).

## Running a command and analyse results

Whatever processing option you choose in all cases you need to start process redirection:

```fsharp
let result =
    CreateProcess.fromRawCommand "./folder/mytool.exe" ["arg1"; "arg2"]
    |> CreateProcess.redirectOutput
    |> Proc.run
```

This immediately changes the type of `result`, now you can access the output:

```fsharp
if result.ExitCode <> 0 then
    printfn "%s" result.Result.Output
    failwithf "FAKE Process exited with %d: %s" result.ExitCode result.Result.Error
let output = result.Result.Output
// Parse output?
```

But even better you can make the 'process-call' type safe for others, by parsing the output:

```fsharp

type MySafeOutput = // ...
let parseOutput (r:ProcessResult<ProcessOutput>) : MySafeOutput =
    // use r.Result.Output, r.ExitCode and create MySafeOutput
let startProcess () (*args*)=
    CreateProcess.fromRawCommand "./folder/mytool.exe" ["arg1"; "arg2"]
    |> CreateProcess.redirectOutput
    |> CreateProcess.ensureExitCode // optional, if your parse function can handle output from failures as well
    |> CreateProcess.map parseOutput

// Usage:
let myOutput : MySafeOutput =
    startProcess()
        |> Proc.run

```

Additionally sometimes you need/want asynchronous results to have intermediate results.
For example if you want that the user continuously "sees" the output as the process generates it:

```fsharp

let output =
    CreateProcess.fromRawCommand "./folder/mytool.exe" ["arg1"; "arg2"]
    |> CreateProcess.redirectOutput
    |> CreateProcess.withOutputEventsNotNull Trace.trace Trace.traceError
    |> Proc.run

// "process" output
```

## Advanced usage scenarios


Redirect output from one process `outgen.exe` to `processIn.exe`:


```fsharp

let input = StreamRef.Empty
let p1 =
    CreateProcess.fromRawCommand "processIn.exe" []
    |> CreateProcess.withStandardInput (CreatePipe input)
    |> Proc.start

let p2 =
    CreateProcess.fromRawCommand "outgen.exe" []
    |> CreateProcess.withStandardOutput (UseStream(true, input.Value))
    |> Proc.run

```