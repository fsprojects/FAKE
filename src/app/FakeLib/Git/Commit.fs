/// Contains helper functions which allow to commit to git repositories.
[<AutoOpen>]
[<System.Obsolete("Use Fake.Tools.Git.Commit instead")>]
module Fake.Git.Commit

#nowarn "44"
open Fake

/// Commits all files in the given repository with the given message
[<System.Obsolete("Use Fake.Tools.Git.Commit instead")>]
let Commit repositoryDir message =
    sprintf "commit -m \"%s\"" message
    |> runSimpleGitCommand repositoryDir
    |> trace

