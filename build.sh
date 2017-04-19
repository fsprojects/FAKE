#!/usr/bin/env bash

# We use this to tell FAKE to not use the current latest version to build the netcore version, 
# but instead use the current NON dotnetcore version
export NO_DOTNETCORE_BOOTSTRAP=true
export PAKET_VERSION=4.4.0
if test "$OS" = "Windows_NT"
then
  # use .Net

  .paket/paket.bootstrapper.exe $PAKET_VERSION --max-file-age=120
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

  .paket/paket.exe restore
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

  [ ! -e build.fsx ] && .paket/paket.exe update
  [ ! -e build.fsx ] && packages/build/FAKE/tools/FAKE.exe init.fsx
  packages/build/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO build.fsx
else
  # use mono
  mono .paket/paket.bootstrapper.exe $PAKET_VERSION --max-file-age=120
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

  mono .paket/paket.exe restore
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

  [ ! -e build.fsx ] && mono .paket/paket.exe update
  [ ! -e build.fsx ] && mono packages/build/FAKE/tools/FAKE.exe init.fsx
  mono packages/build/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO build.fsx
fi
