@echo off

SET MinimalFAKEVersion=674
SET FAKEVersion=1
cls

if exist tools\FAKE\tools\PatchVersion.txt ( 
    FOR /F "tokens=*" %%i in (tools\FAKE\tools\PatchVersion.txt) DO (SET FAKEVersion=%%i)    
)

if %MinimalFAKEVersion% lss %FAKEVersion% goto Build
if %MinimalFAKEVersion%==%FAKEVersion% goto Build

"tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion" "-Prerelease"

:Build
cls

if not exist tools\FSharp.Formatting\lib\net40\FSharp.CodeFormat.dll ( 
	"tools\nuget\nuget.exe" "install" "FSharp.Formatting" "-OutputDirectory" "tools" "-ExcludeVersion" "-Prerelease"
)

SET TARGET="Default"

IF NOT [%1]==[] (set TARGET="%1")
  
"tools\FAKE\tools\Fake.exe" "build.fsx" "target=%TARGET%"

rem Bail if we're running a TeamCity build.
if defined TEAMCITY_PROJECT_NAME goto Quit

rem Loop the build script.
set CHOICE=nothing
echo (Q)uit, (Enter) runs the build again
set /P CHOICE= 
if /i "%CHOICE%"=="Q" goto :Quit

GOTO Build

:Quit
exit /b %errorlevel%