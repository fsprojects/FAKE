#I @"tools/FAKE/tools/"

#r @"FakeLib.dll"

open Fake
let p = NuGetDefaults()
let q = { p with Version = "1.2.3" }