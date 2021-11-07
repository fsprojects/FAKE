@echo off
cls
"..\..\tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "..\..\tools" "-ExcludeVersion"
"..\..\tools\FAKE\tools\Fake.exe" build.fsx target=Clean
pause