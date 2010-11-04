#r "FakeLib.dll"

open System
open Fake

!+ "SampleApp\\bin\\*"
// -- "SampleApp\\bin"
|> Scan 
|> Seq.iter (printfn "scanned %s")
