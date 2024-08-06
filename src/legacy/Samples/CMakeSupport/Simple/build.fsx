#r "../../../build/FakeLib.dll"

open Fake
open Fake.CMakeSupport

// Out of source build: CMake separates the source code from what's being generated.
let binaryDir = "./build"

// Clean both build directories to ensure a reproducible output between each run.
Target "Clean" (fun _ -> CleanDir binaryDir)

// Generate the platform-specific Makefiles.
Target "Configure" (fun _ ->
    ensureDirectory binaryDir

    CMake.Generate(fun p ->
        { p with
            Generator = if isUnix then "Unix Makefiles" else "Visual Studio 12 2013"
            Variables =
                if not isUnix then
                    p.Variables
                else
                    [ { Name = "CMAKE_BUILD_TYPE"
                        Value = CMakeString("Release") } ] }))

// Build the binaries by calling the `cmake --build` wrapper.
Target "Build" (fun _ -> CMake.Build(fun p -> { p with Config = if isUnix then p.Config else "Release" }))

"Clean" ==> "Configure" ==> "Build"

RunTargetOrDefault "Build"
