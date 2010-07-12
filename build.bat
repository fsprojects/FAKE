@echo off
cls
"tools\FAKE\Fake.exe" "build.fsx" logfile=./output.xml


pause
