// include Fake libs
#I @"tools\FAKE"
#r "FakeLib.dll"
open Fake 


Target "Foo" (fun _ ->
    trace "Hello World from Foo"
)

Target "Bar" (fun _ -> 
    trace "Hello World from Bar"
)

"Bar"
  ==> "Foo"

Run "Foo"