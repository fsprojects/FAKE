[<AutoOpen>]
module Fake.Git.Merge

open Fake
open System.IO

/// Gets the current merge message
let getMergeMessage repositoryDir =
    let file = (findGitDir repositoryDir).FullName + "\\MERGE_MSG"
    if File.Exists file then File.ReadAllText file else ""