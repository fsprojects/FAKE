@echo off

:Build
cls

SET TARGET="Default"

IF NOT [%1]==[] (set TARGET="%1")
  
"tools\FAKE\tools\Fake.exe" "build.fsx" "target=%TARGET%"


exit /b %errorlevel%