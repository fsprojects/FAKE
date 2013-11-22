#!/bin/bash

#getting latest FAKE via NuGet
mono tools/NuGet/nuget.exe install FAKE -OutputDirectory tools -ExcludeVersion -Prerelease
mono tools/NuGet/nuget.exe install FSharp.Formatting -OutputDirectory tools -ExcludeVersion

#build FAKE
mono tools/FAKE/tools/FAKE.exe build.fsx
