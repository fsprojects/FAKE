# Fake.Core.Trace module

[API-Reference](apidocs/v5/fake-core-trace.html)

## Logging and Tracing

The `Trace` module allows to trace and output stuff into the console or to your custom environment.

```fsharp

#r "paket:
nuget Fake.Core.Trace //"
open Fake.Core

Trace.log "Some Information message"
Trace.logfn "Some formatted message: %s" "parameter"

Trace.trace "Some trace message"

Trace.traceImportant "Some important message"
Trace.traceFAKE "Some important message %s" "with formatting"

Trace.traceError "Trace some error"

try doSomething()
with e -> Trace.traceException e

Trace.traceLine()

Target.create "mytarget" (fun _ ->
    use __ = Trace.traceTask "MyOperation" "Description"
    // do my operation
    __.MarkSuccess()
)

```

## Custom Listeners

You can implement and set custom listeners in your fake script in order to generate custom output suitable for your environment.

```fsharp

#r "paket:
nuget Fake.Core.Trace //"
open Fake.Core

let mylistener =
    { new ITraceListener with
        member x.Write msg =
            match msg with
            | StartMessage -> ()
            | OpenTag _ -> ()
            | CloseTag _ -> ()
            | ImportantMessage text | ErrorMessage text ->
                printfn "IMPORTANT: %s" text
            | LogMessage(text, newLine) | TraceMessage(text, newLine) ->
                printfn "LOG: %s" text
            | FinishedMessage -> ()
    }

CoreTracing.setTraceListener [mylistener]

```

## Protect secrets

The `Trace`-Api will filter out any registered secrets before printing them into the output.

```fsharp
#r "paket:
nuget Fake.Core.Trace //"
open Fake.Core

// Register your secrets at the start
let secret = Environment.environVarOrDefault "nugetkey" ""
TraceSecrets.register "<REPLACEMENT>" secret

// Later FAKE will replace them and not output them (when using the FAKE-Tracing capabilites)
let cmdLine = sprintf "nuget.exe push -ApiKey %s" secret
Trace.log "NuGet failed while executing: %s" cmdLine
```
