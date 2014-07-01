#r @"../../app/FAKE/bin/Debug/FakeLib.dll"

open Fake


if hasBuildParam "test" |> not then
    failwith "test param is missing"
