#r @"../../app/FAKE/bin/Debug/FakeLib.dll"

open Fake

Target "Default" DoNothing


RunTargetOrDefault "Default"

if hasBuildParam "test" |> not then
    failwith "test param is missing"
else
    trace <|  getBuildParam "test"


System.Console.ReadKey() |> ignore