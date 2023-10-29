// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"
#r @"MyCustomTask.dll"

open Fake
open MyCustomTask

RandomNumberTask.RandomNumber(1, 10) |> tracefn "Random Number: %d"
