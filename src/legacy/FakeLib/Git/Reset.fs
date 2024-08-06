﻿[<AutoOpen>]
/// Contains helper functions which allow to deal with git reset.
[<System.Obsolete("Use Fake.Tools.Git.Reset instead")>]
module Fake.Git.Reset

#nowarn "44"

open System
open Fake.Git.CommandHelper

let internal addArgs commit file =
    sprintf
        "%s%s"
        (if String.IsNullOrEmpty commit then
             ""
         else
             " \"" + commit + "\"")
        (if String.IsNullOrEmpty file then
             ""
         else
             " -- \"" + file + "\"")

/// the intent of the 'reset' helper is to either set a repo to a certain point, or set a file to a certain point.  Git reset doesn't take file paths in the hard/mixed/soft modes, and so you have to use checkout instead for that.
/// This function encapsulates caring about that so you don't have to.
let internal resetOrCheckout file mode =
    match file |> String.IsNullOrEmpty with
    | true -> sprintf "reset --%s" mode
    | false -> "checkout"

/// Performs a git reset "soft".
/// Does not touch the index file nor the working tree at all.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit` - The commit to which git should perform the reset.
///  - `file` - The file to reset - null means all files.
[<System.Obsolete("Use Fake.Tools.Git.Reset instead")>]
let soft repositoryDir commit file =
    resetOrCheckout file "soft" + addArgs commit file |> gitCommand repositoryDir

/// Performs a git reset "mixed".
/// Resets the index but not the working tree and reports what has not been updated.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit` - The commit to which git should perform the reset.
///  - `file` - The file to reset - null means all files.
[<System.Obsolete("Use Fake.Tools.Git.Reset instead")>]
let mixed repositoryDir commit file =
    resetOrCheckout file "mixed" + addArgs commit file |> gitCommand repositoryDir

/// Performs a git reset "hard".
/// Resets the index and working tree. Any changes to tracked files in the working tree since <commit> are discarded.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit` - The commit to which git should perform the reset.
///  - `file` - The file to reset - null means all files.
[<System.Obsolete("Use Fake.Tools.Git.Reset instead")>]
let hard repositoryDir commit file =
    resetOrCheckout file "hard" + addArgs commit file |> gitCommand repositoryDir

/// Performs a git reset "soft" to the current HEAD.
/// Does not touch the index file nor the working tree at all.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
[<System.Obsolete("Use Fake.Tools.Git.Reset instead")>]
let ResetSoft repositoryDir = soft repositoryDir null null

/// Performs a git reset "mixed" to the current HEAD.
/// Resets the index but not the working tree and reports what has not been updated.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
[<System.Obsolete("Use Fake.Tools.Git.Reset instead")>]
let ResetMixed repositoryDir = mixed repositoryDir null null

/// Performs a git reset "hard" to the current HEAD.
/// Resets the index and working tree. Any changes to tracked files in the working tree since <commit> are discarded.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
[<System.Obsolete("Use Fake.Tools.Git.Reset instead")>]
let ResetHard repositoryDir = hard repositoryDir null null
