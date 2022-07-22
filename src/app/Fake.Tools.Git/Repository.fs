namespace Fake.Tools.Git

open Fake.Core
open System.IO
open Fake.IO

/// Contains functions which allow basic operations on git repositories.
/// All operations assume that the CommandHelper can find git.exe.
[<RequireQualifiedAccess>]
module Repository =

    /// Clones a git repository.
    /// ## Parameters
    ///  - `workingDir` - The working directory.
    ///  - `repoUrl` - The URL to the origin.
    ///  - `toPath` - Specifies the new target subfolder.
    let clone workingDir repoUrl toPath =
        CommandHelper.gitCommand workingDir (sprintf "clone %s %s" repoUrl toPath)

    /// Clones a single branch of a git repository.
    /// ## Parameters
    ///  - `workingDir` - The working directory.
    ///  - `repoUrl` - The URL to the origin.
    ///  - `branchName` - Specifies the target branch.
    ///  - `toPath` - Specifies the new target subfolder.
    let cloneSingleBranch workingDir repoUrl branchName toPath =
        sprintf "clone -b %s --single-branch %s %s" branchName repoUrl toPath
        |> CommandHelper.runSimpleGitCommand workingDir
        |> Trace.trace

    /// Inits a git repository.
    /// ## Parameters
    ///  - `repositoryDir` - The path of the target directory.
    ///  - `bare` - If the new directory is a bare directory.
    ///  - `shared` - Specifies that the git repository is to be shared amongst several users. This allows users belonging to the same group to push into that repository.
    let init repositoryDir bare shared =
        match bare, shared with
        | true, true -> CommandHelper.gitCommand repositoryDir "init --bare --shared=all"
        | true, false -> CommandHelper.gitCommand repositoryDir "init --bare"
        | false, true -> CommandHelper.gitCommand repositoryDir "init --shared=all"
        | _ -> CommandHelper.gitCommand repositoryDir "init"

    /// Cleans a directory by removing all files and sub-directories.
    /// ## Parameters
    ///  - `repositoryDir` - The path of the directory to clean.
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
