// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake

let buildDir = "./buildoutput/"
let testDir  = "./test/"

Target "Clean" (fun _ ->
    tracefn "Hello from Clean"
    CleanDir buildDir
)

Target "Restore" (fun _ ->
    tracefn "Hello from Restore"
    RestorePackages()
)

Target "BuildApp" (fun _ ->
    tracefn "Hello from BuildApp"
    MSBuildRelease buildDir "Rebuild" ["./web.sln"]
    |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    tracefn "Hello from BuildTest"
    !! "./VQCore/*.Tests/*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "TestBuild-Output: "
)

Target "RunTests" (fun _ ->
    tracefn "Hello from RunTest"
    !! (testDir + "/*.Tests.dll")
      |> xUnit (fun p ->
          {p with
             OutputDir = testDir 
             Verbose = true
          })
)

// Default target
Target "Default" (fun _ ->
    trace "Hello World from FAKE"
)

// Dependencies
"Clean"
  ==> "Restore"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "RunTests"
  ==> "Default"

// start build
RunTargetOrDefault "Default"