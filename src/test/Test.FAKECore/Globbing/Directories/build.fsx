#r "FakeLib.dll"

open System
open Fake

!! "SampleApp\\bin\\*"
// -- "SampleApp\\bin"
|> Seq.iter (printfn "scanned %s")
