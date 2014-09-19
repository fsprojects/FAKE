@echo off

cls

".nuget\nuget.exe" install Paket -OutputDirectory packages -Prerelease -ExcludeVersion

packages\Paket\tools\Paket.exe install

SET TARGET="Default"

IF NOT [%1]==[] (set TARGET="%1")

"packages\FAKE\tools\Fake.exe" "build.fsx" "target=%TARGET%"