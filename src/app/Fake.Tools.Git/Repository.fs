namespace Fake.Tools.Git

open System
open Fake.Core
open System.IO
open Fake.IO

/// <summary>
/// Contains functions which allow basic operations on git repositories.
/// All operations assume that the CommandHelper can find <c>git.exe</c>.
/// </summary>
[<RequireQualifiedAccess>]
module Repository =

    /// <summary>
    /// Clones a git repository.
    /// </summary>
    ///
    /// <param name="workingDir">The working directory.</param>
    /// <param name="repoUrl">The URL to the origin.</param>
    /// <param name="toPath">Specifies the new target subfolder.</param>
    let clone workingDir repoUrl toPath =
        CommandHelper.gitCommand workingDir (sprintf "clone %s %s" repoUrl toPath)

    /// <summary>
    /// Clones a single branch of a git repository.
    /// </summary>
    ///
    /// <param name="workingDir">The working directory.</param>
    /// <param name="repoUrl">The URL to the origin.</param>
    /// <param name="branchName">Specifies the target branch.</param>
    /// <param name="toPath">Specifies the new target subfolder.</param>
    let cloneSingleBranch workingDir repoUrl branchName toPath =
        sprintf "clone -b %s --single-branch %s %s" branchName repoUrl toPath
        |> CommandHelper.runSimpleGitCommand workingDir
        |> Trace.trace

    /// <summary>
    /// Inits a git repository.
    /// </summary>
    ///
    /// <param name="repositoryDir">The path of the target directory.</param>
    /// <param name="bare">If the new directory is a bare directory.</param>
    /// <param name="shared">Specifies that the git repository is to be shared amongst several users.
    /// This allows users belonging to the same group to push into that repository.</param>
    let init repositoryDir bare shared =
        match bare, shared with
        | true, true -> CommandHelper.gitCommand repositoryDir "init --bare --shared=all"
        | true, false -> CommandHelper.gitCommand repositoryDir "init --bare"
        | false, true -> CommandHelper.gitCommand repositoryDir "init --shared=all"
        | _ -> CommandHelper.gitCommand repositoryDir "init"

    /// <summary>
    /// Cleans a directory by removing all files and sub-directories.
    /// </summary>
    ///
    /// <param name="repositoryDir">The path of the directory to clean.</param>
    let fullClean repositoryDir =
        let di = DirectoryInfo.ofPath repositoryDir

        if di.Exists then
            Trace.logfn "Deleting contents of %s" repositoryDir
            // delete all files
            Directory.GetFiles(repositoryDir, "*.*", SearchOption.TopDirectoryOnly)
            |> Seq.iter (fun file ->
                let fi = FileInfo.ofPath file
                fi.IsReadOnly <- false
                fi.Delete())

            // deletes all subdirectories
            let rec deleteDirs actDir =
                let di = DirectoryInfo.ofPath actDir

                if di.Name = ".git" then
                    ()
                else
                    try
                        Directory.GetDirectories(actDir) |> Seq.iter deleteDirs
                        Directory.Delete(actDir, true)
                    with exn ->
                        ()

            Directory.GetDirectories repositoryDir |> Seq.iter deleteDirs
        else
            Directory.create repositoryDir

        // set writeable
        File.SetAttributes(repositoryDir, FileAttributes.Normal)

    /// <summary>
    /// Cleans a directory by removing all files and sub-directories.
    /// </summary>
    ///
    /// <param name="repositoryDir">The path of the directory to clean.</param>
    [<Obsolete("Please use fullClean instead. This method will be removed in FAKE next major release")>]
    let fullclean repositoryDir =
        fullClean repositoryDir
