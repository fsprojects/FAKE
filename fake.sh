#!/usr/bin/env bash

dotnet restore build.proj
dotnet fake $@
