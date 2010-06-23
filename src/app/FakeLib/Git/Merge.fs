[<AutoOpen>]
module Fake.Git.Merge

open Fake

let getMergeMessage() =
    let file = CommandHelper.getWorkingDirGitDir() + "\\MERGE_MSG";
    if System.IO.File.Exists file then
        System.IO.File.ReadAllText file
    else
        ""