// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"
open Fake 

// Default target
Target "Default" (fun _ ->
    trace "Hello World from FAKE"
)

// start build
Run "Default"