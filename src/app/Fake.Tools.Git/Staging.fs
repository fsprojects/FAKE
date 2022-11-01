namespace Fake.Tools.Git

open Fake.Core

/// <summary>
/// Contains helper functions which allow to deal with git's staging area.
/// </summary>
[<RequireQualifiedAccess>]
module Staging =

    /// <summary>
    /// Adds a file to the staging area
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="file">The file to stage</param>
    let stageFile repositoryDir file =
        file
        |> CommandHelper.fixPath
        |> sprintf "update-index --add \"%s\""
        |> CommandHelper.runGitCommand repositoryDir

    /// <summary>
    /// Adds all files to the staging area
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let stageAll repositoryDir =
        "add . --all" |> CommandHelper.runSimpleGitCommand repositoryDir |> Trace.trace
