/// Contains helper functions which allow to deal with git's staging area.
module Fake.Tools.Git.Staging

open Fake.Core
open Fake.Tools.Git.CommandHelper

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
    |> Trace.trace