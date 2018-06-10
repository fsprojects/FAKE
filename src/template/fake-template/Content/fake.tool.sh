#!/usr/bin/env bash

set -eu
set -o pipefail

TOOL_PATH=(ToolPath)

if ! [ -e $TOOL_PATH/fake ] 
then
  dotnet tool install fake-cli --tool-path $TOOL_PATH --version (version)
fi
$TOOL_PATH/fake "$@"