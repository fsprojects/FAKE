#r @"../../app/FAKE/bin/Debug/FakeLib.dll"

open Fake

#if MONO
failwith "mono was set"
#else
failwith "mono was not set"
#endif

if hasBuildParam "test" |> not then
    failwith "test param is missing"
