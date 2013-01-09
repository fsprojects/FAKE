@echo off
cls
"tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion" "-Prerelease"
"tools\FAKE\tools\Fake.exe" build.fsx
pause
