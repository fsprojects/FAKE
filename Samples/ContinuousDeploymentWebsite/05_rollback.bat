@echo off
cls
SET FSI=..\..\tools\FSharp\Fsi.exe
"..\..\tools\FAKE\Fake.exe" 04_deploy.fsx target=Rollback
pause