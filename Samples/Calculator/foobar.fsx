// include Fake libs
#I @"tools\FAKE"
#r "FakeLib.dll"
open Fake 


Target? Foo <- fun _ -> log "Hello World from Foo"
Target? Bar <- fun _ -> log "Hello World from Bar"

For? Foo <- Dependency? Bar

Run? Foo