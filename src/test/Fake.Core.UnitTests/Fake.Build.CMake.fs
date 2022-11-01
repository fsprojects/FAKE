module Fake.Build.CMakeTests

open System
open Expecto
open Fake.Build

[<Tests>]
let tests =

    testList
        "Fake.Build.CMake.Tests"
        [ testCase "Test all arguments are mapped correctly"
          <| fun _ ->
              let args: CMake.CMakeGenerateParams =
                  { ToolPath = "cmake"
                    SourceDirectory = "source-directory"
                    BinaryDirectory = "binary-directory"
                    Toolchain = "toolchain"
                    Generator = "generator"
                    Toolset = "toolset"
                    Platform = "platform"
                    Caches = [ "cache1" ]
                    InstallDirectory = "install-directory"
                    Variables =
                      [ { Name = "var1"
                          Value = CMake.CMakeString("v1") } ]
                    CacheEntriesToRemove = [ "cache1" ]
                    Timeout = TimeSpan.MaxValue
                    AdditionalArgs = "additional-arg" }

              let generateArgs = CMake.getGenerateArguments args

              Expect.equal
                  generateArgs
                  "-G \"generator\" \
          -D CMAKE_TOOLCHAIN_FILE:FILEPATH=\"toolchain\" \
          -T \"toolset\" \
          -A \"platform\" \
          -C \"cache1\" \
          -D CMAKE_INSTALL_PREFIX:PATH=\"install-directory\" \
          -D var1:STRING=\"v1\" \
          -U \"cache1\" \
          additional-arg \
          \"source-directory\""
                  "expected proper arguments formatting"

          testCase "Test missing arguments are not mapped"
          <| fun _ ->
              let args: CMake.CMakeGenerateParams =
                  { ToolPath = "cmake"
                    SourceDirectory = "source-directory"
                    BinaryDirectory = "binary-directory"
                    Toolchain = ""
                    Generator = ""
                    Toolset = ""
                    Platform = ""
                    Caches = [ "cache1" ]
                    InstallDirectory = "install-directory"
                    Variables =
                      [ { Name = "var1"
                          Value = CMake.CMakeString("v1") } ]
                    CacheEntriesToRemove = [ "cache1" ]
                    Timeout = TimeSpan.MaxValue
                    AdditionalArgs = "additional-arg" }

              let generateArgs = CMake.getGenerateArguments args

              Expect.equal
                  generateArgs
                  "-C \"cache1\" \
          -D CMAKE_INSTALL_PREFIX:PATH=\"install-directory\" \
          -D var1:STRING=\"v1\" \
          -U \"cache1\" \
          additional-arg \
          \"source-directory\""
                  "expected proper arguments formatting"

          testCase "Test variables arg is mapped correctly"
          <| fun _ ->
              let args: CMake.CMakeGenerateParams =
                  { ToolPath = "cmake"
                    SourceDirectory = "source-directory"
                    BinaryDirectory = "binary-directory"
                    Toolchain = ""
                    Generator = ""
                    Toolset = ""
                    Platform = ""
                    Caches = [ "" ]
                    InstallDirectory = ""
                    Variables =
                      [ { Name = "var1-str"
                          Value = CMake.CMakeString("v1") }
                        { Name = "var2-bool"
                          Value = CMake.CMakeBoolean(true) }
                        { Name = "var3-dirPath"
                          Value = CMake.CMakeDirPath("v3") }
                        { Name = "var4-filePath"
                          Value = CMake.CMakeFilePath("v4") } ]
                    CacheEntriesToRemove = [ "" ]
                    Timeout = TimeSpan.MaxValue
                    AdditionalArgs = "" }

              let generateArgs = CMake.getGenerateArguments args

              Expect.equal
                  generateArgs
                  "-D var1-str:STRING=\"v1\" \
          -D var2-bool:BOOL=ON \
          -D var3-dirPath:PATH=\"v3\" \
          -D var4-filePath:FILEPATH=\"v4\"  \
          \"source-directory\""
                  "expected proper arguments formatting" ]
