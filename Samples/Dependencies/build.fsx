#I @"../../build/"
#r @"FakeLib.dll"

open System
open Fake

let Target name = Target name DoNothing

Target "Clean"

Target "Build DB1"
Target "Deploy DB1_1"
Target "Deploy DB1_2"

Target "Build DB2"
Target "Deploy DB2_1"
Target "Deploy DB2_2"

Target "Build App"
Target "App Unit Tests"
Target "App Integration Tests"
Target "End-to-End Tests"

Target "Create zip"

"Clean"
    ==> "Build DB1" 
       ==> "Deploy DB1_1" <=> "Deploy DB1_2"
//    <=> ("Build DB2" ==> "Deploy DB2_1" <=> "Deploy DB2_2")
//    <=> ("Build App" ==> "App Unit Tests" <=> "App Integration Tests")
    ==> "End-to-End Tests"
    ==> "Create zip"


PrintDependencyGraph true "End-to-End Tests"

//Run "Create zip"
