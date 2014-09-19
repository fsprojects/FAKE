#!/bin/bash

mono --runtime=v4.0 .nuget/nuget.exe install Paket -OutputDirectory packages -Prerelease -ExcludeVersion

mono --runtime=v4.0 packages/Paket/tools/Paket.exe install

mono --runtime=v4.0 packages/FAKE/tools/FAKE.exe build.fsx $@