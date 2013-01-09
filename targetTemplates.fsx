// include Fake libs
#I "tools\FAKE"
#r "FakeLib.dll"
open Fake 

/// createTraceTarget: string -> string*int -> Target
let createTraceTarget = TargetTemplate (fun (s,d) ->
  trace s
  trace <| sprintf "my Int: %d" d
)

createTraceTarget "TraceHello" ("Hello World from FAKE",2)
  
createTraceTarget "Trace2"  ("Trace another text",42)