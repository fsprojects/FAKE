#!/bin/bash

#getting latest FAKE via NuGet
mono tools/NuGet/NuGet.exe install FAKE -OutputDirectory tools -ExcludeVersion -Prerelease

#build FAKE
mono tools/FAKE/tools/FAKE.exe "$@"
