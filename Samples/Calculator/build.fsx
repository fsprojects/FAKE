// include Fake libs
#I @"tools\FAKE"
#r "FakeLib.dll"
open Fake 

// Default target
let Default = target { trace "Hello World from FAKE" }

// start build
Run Default