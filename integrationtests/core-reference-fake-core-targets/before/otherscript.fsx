open Fake.Core

Target.initEnvironment()
Target.create "OtherScriptTarget" (fun _ ->
    printfn "Doing Something."
)