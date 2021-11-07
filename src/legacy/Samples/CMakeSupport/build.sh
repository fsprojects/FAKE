#!/bin/bash
# Builds the CMake support examples on Linux.
#
# To execute it on the Vagrant VM, simply execute the following steps:
# 1. Install the `cmake` and `build-essential` packages:
#    sudo apt-get -y install cmake build-essential
# 2. Run this script from any directory:
#    bash /vagrant/src/fake/Samples/CMakeSupport/build.sh

# Exit the script on any error.
set -e

currentDir="$PWD"
baseDir="$(dirname $0)"
fake="../../../build/FAKE.exe"

cd "$baseDir"

cd Simple
"$fake" ./build.fsx

cd ..

cd Advanced
"$fake" ./build.fsx

cd "$currentDir"
