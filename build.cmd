@echo off
SETLOCAL

REM We use this to tell FAKE to not use the current latest version to build the netcore version, 
REM but instead use the current NON dotnetcore version
SET NO_DOTNETCORE_BOOTSTRAP=true
SET PAKET_VERSION=5.0.0-alpha006

.paket\paket.bootstrapper.exe %PAKET_VERSION%
.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

SET FAKE_PATH=packages\build\FAKE\tools\Fake.exe
SET Platform=

IF [%1]==[] (
    "%FAKE_PATH%" "build.fsx" "Default" 
) ELSE (
    "%FAKE_PATH%" "build.fsx" %* 
) 
