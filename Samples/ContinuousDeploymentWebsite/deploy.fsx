// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"

open Fake

// Directories
let deployDir = @".\Publish\"
let serverUrl = "http://localhost:8085/fake/"

let traceActiveReleases() =
    tracefn "Active Releases:"
    DeploymentHelper.getAllReleases "."
      |> Seq.iter (tracefn "%A")

// Targets
Target "Deploy" (fun _ ->
    !! (deployDir + "*.nupkg") 
        |> Seq.head
        |> HttpClientHelper.PostDeploymentPackage serverUrl

    traceActiveReleases()
)

Target "Rollback" (fun _ ->
    HttpClientHelper.RollbackPackage serverUrl "Fake_Website" "HEAD~1"

    traceActiveReleases()
)

// start build
RunParameterTargetOrDefault "target" "Deploy"