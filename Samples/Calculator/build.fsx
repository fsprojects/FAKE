// include Fake libs
#I @"tools\FAKE"
#r "FakeLib.dll"
open Fake 

// Default target
Target? Default <-
    fun _ -> trace "Hello World from FAKE"

// start build
Run? Default