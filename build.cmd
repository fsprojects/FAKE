@echo off
cls

SET TARGET="Deploy"

IF NOT [%1]==[] (set TARGET="%1")
  
"tools\FAKE\Fake.exe" "build.fsx" "target=%TARGET%"

pause