@echo off

cls

"tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
"tools\nuget\nuget.exe" "install" "FSharp.Formatting.CommandTool" "-OutputDirectory" "tools" "-ExcludeVersion" "-Prerelease"
"tools\nuget\nuget.exe" "install" "SourceLink.Fake" "-OutputDirectory" "tools" "-ExcludeVersion"

SET TARGET="Default"

IF NOT [%1]==[] (set TARGET="%1")

"tools\FAKE\tools\Fake.exe" "build.fsx" "target=%TARGET%"