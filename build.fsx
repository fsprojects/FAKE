// include Fake lib
#r @"tools/FAKE/tools/FakeLib.dll"
open Fake

let buildDir = "./buildoutput/"
let testDir  = "./test/"

Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "Restore" (fun _ ->
    RestorePackages()
)

Target "BuildApp" (fun _ ->
    MSBuildRelease buildDir "Rebuild" ["./web.sln"]
    |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    !! "./VQCore/*.Tests/*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "TestBuild-Output: "
)

Target "RunTests" (fun _ ->
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