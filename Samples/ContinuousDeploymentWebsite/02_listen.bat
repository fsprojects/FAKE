cd %~dp0
@echo off
cls
"tools\FAKE\tools\Fake.Deploy.exe" /listen localhost 8085
pause