/// Contains helper functions which allow to commit to git repositories.
[<AutoOpen>]
module Fake.Git.Commit

open Fake

/// Commits all files in the given repository with the given message
let Commit repositoryDir message =
    sprintf "commit -m \"%s\"" message
    |> runSimpleGitCommand repositoryDir
    |> trace