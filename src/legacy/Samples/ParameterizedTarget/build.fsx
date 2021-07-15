// include Fake libs
#I @"..\..\tools\FAKE"
#r "FakeLib.dll"
open Fake 

// *** Define Targets ***

Description "Cleans the last build"
Target "Clean" (fun () -> 
    trace " --- Cleaning stuff --- "
)

Target "Build" (fun () -> 
    trace " --- Building the app --- "
)

Target "Deploy" (fun () -> 
    trace " --- Deploying app --- "
)

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Deploy"

// *** Start Build ***
RunParameterTargetOrDefault "target" "Deploy"