[<AutoOpen>]
/// Contains helper functions which allow to deal with git's staging area.
module Fake.Git.Staging

open Fake

/// Adds a file to the staging area
let StageFile repositoryDir file =
    file 
    |> fixPath
    |> sprintf "update-index --add \"%s\""
    |> runGitCommand repositoryDir

/// Adds all files to the staging area
let StageAll repositoryDir =
    "add . --all"
    |> runSimpleGitCommand repositoryDir
    |> trace