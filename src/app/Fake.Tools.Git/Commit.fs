namespace Fake.Tools.Git

open Fake.Core

/// Contains helper functions which allow to commit to git repositories.
[<RequireQualifiedAccess>]
module Commit =

    /// Commits all files in the given repository with the given message
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `message` - The commit message text.
    let exec repositoryDir message =
        sprintf "commit -m \"%s\"" message
        |> CommandHelper.runSimpleGitCommand repositoryDir
        |> Trace.trace

    /// Commits all files in the given repository with the given short message along with an extended message
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `shortMessage` - The commit short (title) message text.
    ///  - `extendedMessage` - The commit extended (description) message text.
    let execExtended repositoryDir shortMessage extendedMessage =
        sprintf "commit -m \"%s\" -m \"%s\"" shortMessage extendedMessage
        |> CommandHelper.runSimpleGitCommand repositoryDir
        |> Trace.trace
