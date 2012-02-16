@echo off
cls
SET FSI=..\..\tools\FSharp\Fsi.exe
"..\..\tools\FAKE\Fake.exe" 01_build.fsx
pause