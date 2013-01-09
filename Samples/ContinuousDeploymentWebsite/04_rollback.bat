@echo off
cls
"tools\FAKE\tools\Fake.exe" deploy.fsx target=Rollback
pause