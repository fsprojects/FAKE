#!/bin/bash
if [ ! -f tools/FAKE/tools/Fake.exe ]; then
  mono --runtime=v4.0 tools/NuGet/nuget.exe install FAKE -OutputDirectory tools -ExcludeVersion  -Prerelease
  mono --runtime=v4.0 tools/NuGet/nuget.exe install FSharp.Formatting.CommandTool -OutputDirectory tools -ExcludeVersion -Prerelease
fi
mono --runtime=v4.0 tools/FAKE/tools/FAKE.exe build.fsx $@