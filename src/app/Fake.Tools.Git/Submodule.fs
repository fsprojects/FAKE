namespace Fake.Tools.Git

open Fake.Core

/// <summary>
/// Contains helper functions which allow to deal with git submodules.
/// </summary>
[<RequireQualifiedAccess>]
module Submodule =

    /// <summary>
    /// This record represents a git submodule binding.
    /// </summary>
    type Submodule =
        { Name: string
          Branch: string
          CurrentCommit: string
          Initialized: bool
          SuperRepositoryDir: string
          UpToDate: bool }

        /// Gets the current status.
        member x.Status =
            if not x.Initialized then "Not initialized"
            else if not x.UpToDate then "Modified"
            else "Up-to-date"

        /// Gets the remote path from the config.
        member x.GetRemotePath() =
            x.Name.Trim()
            |> sprintf "config -f .gitmodules --get submodule.%s.url"
            |> CommandHelper.runSimpleGitCommand x.SuperRepositoryDir

        /// Gets the local path from the config.
        member x.GetLocalPath() =
            x.Name.Trim()
            |> sprintf "config -f .gitmodules --get submodule.%s.path"
            |> CommandHelper.runSimpleGitCommand x.SuperRepositoryDir

    let internal trimChars (s: string) = s.Trim [| '('; ')'; ' ' |]

    /// <summary>
    /// Gets all submodules from the given repository directory.
    /// </summary>
    ///
    /// <param name="repositoryDir">The path of the target directory.</param>
    let getSubModules repositoryDir =
        let _, submodules, _ = CommandHelper.runGitCommand repositoryDir "submodule status"

        submodules
        |> Seq.filter (fun submodule -> submodule.Length >= 43)
        |> Set.ofSeq // remove duplicates
        |> Seq.map (fun submodule ->
            let n = submodule.Substring(42).Trim()

            let name, branch =
                if n.Contains "(" then
                    n.Substring(0, n.IndexOf "(") |> trimChars, n.Substring(n.IndexOf "(") |> trimChars
                else
                    n, null

            { Branch = branch
              CurrentCommit = submodule.Substring(1, 40).Trim()
              Initialized = submodule[0] <> '-'
              Name = name
              SuperRepositoryDir = repositoryDir
              UpToDate = submodule[0] <> '+' })

    /// <summary>
    /// Inits a submodule with the given name in a subfolder of the given super repository.
    /// </summary>
    ///
    /// <param name="superRepositoryDir">The super repository.</param>
    /// <param name="name">The name of the new repository.</param>
    let init superRepositoryDir name =
        if String.isNullOrEmpty name then
            "submodule update --init"
        else
            "submodule update --init \"" + name.Trim() + "\""
        |> CommandHelper.gitCommand superRepositoryDir

    /// <summary>
    /// Adds a submodule to the given super repository.
    /// </summary>
    ///
    /// <param name="superRepositoryDir">The super repository.</param>
    /// <param name="remotePath">The path to the remote repository of the submodule.</param>
    /// <param name="localPath">The local path to the submodule.</param>
    /// <param name="branch">The branch to  clone. (can be null)</param>
    let add superRepositoryDir remotePath localPath branch =
        sprintf
            "submodule add \"%s\" \"%s\" %s"
            (remotePath |> CommandHelper.fixPath)
            (localPath |> CommandHelper.fixPath)
            (if String.isNullOrEmpty branch then
                 ""
             else
                 " \"" + branch.Trim() + "\"")
        |> CommandHelper.gitCommand superRepositoryDir
