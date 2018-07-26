/// Contains helper functions which allow to commit to git repositories.
module Fake.Tools.Git.Commit

open Fake.Tools.Git.CommandHelper
open Fake.Core

/// Commits all files in the given repository with the given message
let exec repositoryDir message =
    sprintf "commit -m \"%s\"" message
    |> runSimpleGitCommand repositoryDir
    |> Trace.trace

/// Commits all files in the given repository with the given short message along with an extended message
let execExtended repositoryDir shortMessage extendedMessage = 
    sprintf "commit -m \"%s\" -m \"%s\"" shortMessage extendedMessage
    |> runSimpleGitCommand repositoryDir
    |> Trace.trace
