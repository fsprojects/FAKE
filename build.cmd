@echo off

cls

if not exist tools\FAKE\tools\Fake.exe (
    "tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion" "-Prerelease"
)

if not exist tools\FSharp.Formatting.CommandTool\tools\fsformatting.exe ( 
    "tools\nuget\nuget.exe" "install" "FSharp.Formatting.CommandTool" "-OutputDirectory" "tools" "-ExcludeVersion" "-Prerelease"
)

if not exist tools\SourceLink.Fake\tools\SourceLink.fsx (
    "tools\nuget\nuget.exe" "install" "SourceLink.Fake" "-OutputDirectory" "tools" "-ExcludeVersion"
)

SET TARGET="Default"

IF NOT [%1]==[] (set TARGET="%1")

"tools\FAKE\tools\Fake.exe" "build.fsx" "target=%TARGET%"