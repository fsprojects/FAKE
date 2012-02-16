@echo off
cls
SET FSI=..\..\tools\FSharp\Fsi.exe
"..\..\tools\FAKE\Fake.exe" 03_deploy.fsx
pause