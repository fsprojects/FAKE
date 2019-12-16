module OtherFile

open Fake.Core
[<System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>]
let createTargets () =

    Target.create "OtherFileTarget" (fun _ ->
        printfn "Doing Something."
    )

    // required because otherwise JIT is still inlining this :(
    printfn "test"