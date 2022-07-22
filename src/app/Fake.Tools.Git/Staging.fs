namespace Fake.Tools.Git

open Fake.Core

/// Contains helper functions which allow to deal with git's staging area.
[<RequireQualifiedAccess>]
module Staging =

    /// Adds a file to the staging area
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `file` - The file to stage
    let stageFile repositoryDir file =
        file
        |> CommandHelper.fixPath
        |> sprintf "update-index --add \"%s\""
        |> CommandHelper.runGitCommand repositoryDir

    /// Adds all files to the staging area
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let stageAll repositoryDir =
        "add . --all" |> CommandHelper.runSimpleGitCommand repositoryDir |> Trace.trace
