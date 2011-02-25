@echo off
cls

SET TARGET="Default"

IF NOT [%1]==[] (set TARGET="%1")
  
"tools\FAKE\Fake.exe" "build.fsx" "target=%TARGET%"

pause