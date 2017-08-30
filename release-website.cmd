@echo off

fake run "build.fsx" -s --target GenerateDocs
fake run "build.fsx" -s --target ReleaseDocs