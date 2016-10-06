@echo off
SETLOCAL

cls

.paket\paket.bootstrapper.exe
if errorlevel 1 (
  exit /b %errorlevel%
)

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
