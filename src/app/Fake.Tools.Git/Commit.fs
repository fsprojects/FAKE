/// Contains helper functions which allow to commit to git repositories.
module Fake.Tools.Git.Commit

open Fake.Tools.Git.CommandHelper
open Fake.Core

/// Commits all files in the given repository with the given message
let Commit repositoryDir message =
    sprintf "commit -m \"%s\"" message
    |> runSimpleGitCommand repositoryDir
    |> Trace.trace

