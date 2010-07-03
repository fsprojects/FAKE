[<AutoOpen>]
module Fake.MSBuild.Splicing

open Fake

let removeAssemblyReference project =
    let doc = XMLDoc project
    let node = doc.SelectSingleNode "ItemGroup/Reference"
    node.Value

