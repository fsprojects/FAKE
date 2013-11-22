[<AutoOpen>]
/// Contains helper functions which allow to deal with git reset.
module Fake.Git.Reset

open System
open Fake

let internal addArgs commit file =
    sprintf "%s%s"
      (if String.IsNullOrEmpty commit then "" else " \"" + commit + "\"")
      (if String.IsNullOrEmpty file then "" else " -- \"" + file + "\"")

/// Performs a git reset "soft".
/// Does not touch the index file nor the working tree at all.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit` - The commit to which git should perform the reset.
///  - `file` - The file to reset - null means all files.
let soft repositoryDir commit file = "reset --soft" + addArgs commit file |> gitCommand repositoryDir

/// Performs a git reset "mixed".
/// Resets the index but not the working tree and reports what has not been updated. 
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit` - The commit to which git should perform the reset.
///  - `file` - The file to reset - null means all files.
let mixed repositoryDir commit file = "reset --mixed" + addArgs commit file |> gitCommand repositoryDir

/// Performs a git reset "hard".
/// Resets the index and working tree. Any changes to tracked files in the working tree since <commit> are discarded.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit` - The commit to which git should perform the reset.
///  - `file` - The file to reset - null means all files.
let hard repositoryDir commit file = "reset --hard" + addArgs commit file |> gitCommand repositoryDir

/// Performs a git reset "soft" to the current HEAD.
/// Does not touch the index file nor the working tree at all.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
let ResetSoft repositoryDir = soft repositoryDir null null

/// Performs a git reset "mixed" to the current HEAD.
/// Resets the index but not the working tree and reports what has not been updated. 
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
let ResetMixed repositoryDir = mixed repositoryDir null null

/// Performs a git reset "hard" to the current HEAD.
/// Resets the index and working tree. Any changes to tracked files in the working tree since <commit> are discarded.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
let ResetHard repositoryDir = hard repositoryDir null null