[<AutoOpen>]
/// Contains helper functions which allow to deal with git's staging area.
[<System.Obsolete("Use Fake.Tools.Git.Staging instead")>]
module Fake.Git.Staging

#nowarn "44"
open Fake

/// Adds a file to the staging area
[<System.Obsolete("Use Fake.Tools.Git.Staging instead")>]
let StageFile repositoryDir file =
    file 
    |> fixPath
    |> sprintf "update-index --add \"%s\""
    |> runGitCommand repositoryDir

/// Adds all files to the staging area
[<System.Obsolete("Use Fake.Tools.Git.Staging instead")>]
let StageAll repositoryDir =
    "add . --all"
    |> runSimpleGitCommand repositoryDir
    |> trace