// include Fake libs
#I @"..\..\tools\FAKE\"
#r "FakeLib.dll"

open Fake

// Directories
let deployDir = @".\Publish\"

// Targets
Target "Deploy" (fun _ ->
    !! (deployDir + "*.nupkg") 
        |> Seq.head
        |> HttpClientHelper.PostDeploymentPackage "http://localhost:8085/fake/"

    tracefn "Active Releases:"
    DeploymentHelper.getAllReleases "."
      |> Seq.iter (tracefn "%A")
)

Target "Default" DoNothing

// Dependencies
"Deploy"
  ==> "Default"
 
// start build
RunParameterTargetOrDefault "target" "Default"