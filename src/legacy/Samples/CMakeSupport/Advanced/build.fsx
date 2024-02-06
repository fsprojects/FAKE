#r "../../../build/FakeLib.dll"

open Fake
open Fake.CMakeSupport

// Out of source build: CMake separates the source code from what's being generated.

let communicatorSourceDir = currentDirectory @@ "Communicator"
let communicatorBinaryDir = communicatorSourceDir + "-build"
let communicatorInstallDir = communicatorSourceDir + "-install"
let helloSourceDir = currentDirectory @@ "Hello"
let helloBinaryDir = helloSourceDir + "-build"

// As both CMake projects will use the same compilers, we define the common parameters only once.

let commonGenerateParams (parameters: CMakeGenerateParams) =
    { parameters with
        Generator = if isUnix then "Unix Makefiles" else "Visual Studio 12 2013"
        Variables =
            if not isUnix then
                parameters.Variables
            else
                [ { Name = "CMAKE_BUILD_TYPE"
                    Value = CMakeString("Release") } ] }

let commonBuildParams (parameters: CMakeBuildParams) =
    { parameters with
        Config = if isUnix then parameters.Config else "Release" }

// Clean both build directories to ensure a reproducible output between each run.
Target "Clean" (fun _ -> CleanDirs [ communicatorBinaryDir; communicatorInstallDir; helloBinaryDir ])

Target "Configure-Communicator" (fun _ ->
    ensureDirectory communicatorBinaryDir

    CMake.Generate(fun p ->
        { commonGenerateParams p with
            SourceDirectory = communicatorSourceDir
            BinaryDirectory = communicatorBinaryDir
            InstallDirectory = communicatorInstallDir }))

Target "Build-Communicator" (fun _ ->
    CMake.Build(fun p ->
        { commonBuildParams p with
            BinaryDirectory = communicatorBinaryDir }))

Target "Install-Communicator" (fun _ ->
    CMake.Build(fun p ->
        { commonBuildParams p with
            BinaryDirectory = communicatorBinaryDir
            Target = if isUnix then "install" else "INSTALL" }))

Target "Configure-Hello" (fun _ ->
    ensureDirectory helloBinaryDir

    CMake.Generate(fun p ->
        { commonGenerateParams p with
            SourceDirectory = helloSourceDir
            BinaryDirectory = helloBinaryDir
            Variables =
                [ { Name = "COMMUNICATOR_INCLUDE_DIR"
                    Value = CMakeDirPath(communicatorInstallDir @@ "include") }
                  { Name = "COMMUNICATOR_LIBRARY"
                    Value = CMakeFilePath(communicatorInstallDir @@ "lib" @@ "Communicator.lib") } ] }))

Target "Build-Hello" (fun _ ->
    CMake.Build(fun p ->
        { commonBuildParams p with
            BinaryDirectory = helloBinaryDir }))

Target "All" (fun _ -> ())

"Clean"
==> "Configure-Communicator"
==> "Build-Communicator"
==> "Install-Communicator"
==> "Configure-Hello"
==> "Build-Hello"

"Build-Hello" ==> "All"

RunTargetOrDefault "All"
