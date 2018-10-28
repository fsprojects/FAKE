#!/usr/bin/env bash

set -eu
set -o pipefail

dotnet restore build.proj
dotnet fake build "$@"