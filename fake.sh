#!/usr/bin/env bash

VERBOSE=${VERBOSE:-true}
FAKE_VERSION=${FAKE_VERSION:-"core-v1.0-alpha-10"}

# Use this instead of the obtain_fake include.
# Look into the documentation for the latest version.
# p=".fake";f="$p/obtain_fake.sh";if [ ! -f "$f" ];then mkdir -p $p;curl -fLso $f https://raw.githubusercontent.com/matthid/FAKE/coreclr/script/obtain_fake.sh; fi;. $f

. script/obtain_fake.sh

install_fake_packages

exec_fake $*
