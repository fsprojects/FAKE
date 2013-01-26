@echo off
"tools\FAKE\tools\Fake.exe" "build.fsx" "%1"
exit /b %errorlevel%