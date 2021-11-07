:: Builds the CMake support examples on Windows.
@echo off

set "currentDir=%CD%"
set "baseDir=%~dp0"
set "fake=..\..\..\build\FAKE.exe"

cd "%baseDir%"
if %errorlevel% neq 0 exit /b %errorlevel%

cd Simple
"%fake%" .\build.fsx
if %errorlevel% neq 0 exit /b %errorlevel%

cd ..

cd Advanced
"%fake%" .\build.fsx
if %errorlevel% neq 0 exit /b %errorlevel%

cd "%currentDir%"
