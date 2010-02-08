
#load "TargetMonad.fs"

open TargetMonad

let r = new System.Random()
let f = target { do! logMsg ("f() = " + (r.Next().ToString()))}
let p = target { do! logMsg "p()" }
let g = target { do! logMsg "g()" }

p <== f
g <== p

g
   |> runTarget
   |> printfn "Result: %A"