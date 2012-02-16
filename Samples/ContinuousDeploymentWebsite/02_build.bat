@echo off
cls
SET FSI=..\..\tools\FSharp\Fsi.exe
"..\..\tools\FAKE\Fake.exe" 02_build.fsx
pause