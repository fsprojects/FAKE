// include Fake libs
#I @"..\..\tools\FAKE"
#r "FakeLib.dll"
open Fake 

// *** Define Targets ***
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
"Build"  <== ["Clean"]
"Deploy"  <== ["Build"]

// *** Start Build ***
RunParameterTargetOrDefault "target" "Deploy"