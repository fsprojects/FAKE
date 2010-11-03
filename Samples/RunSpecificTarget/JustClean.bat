@echo off
cls
"..\..\tools\FAKE\Fake.exe" build.fsx target=Clean
pause