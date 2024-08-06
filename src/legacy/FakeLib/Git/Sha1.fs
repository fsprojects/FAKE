﻿[<AutoOpen>]
/// Contains functions which allow the SHA1 of a file with git and without it.
[<System.Obsolete("Use Fake.Tools.Git.Sha1 instead")>]
module Fake.Git.SHA1

#nowarn "44"

open System.Security.Cryptography
open System.Text

/// Calculates the SHA1 for a given string.
[<System.Obsolete("Use Fake.Tools.Git.Sha1 instead")>]
let calcSHA1 (text: string) =
    Fake.EnvironmentHelper.encoding.GetBytes text
    |> (new SHA1CryptoServiceProvider()).ComputeHash
    |> Array.fold
        (fun acc e ->
            let t = System.Convert.ToString(e, 16)
            if t.Length = 1 then acc + "0" + t else acc + t)
        ""

/// Calculates the SHA1 for a given string like git.
[<System.Obsolete("Use Fake.Tools.Git.Sha1 instead")>]
let calcGitSHA1 (text: string) =
    let s = text.Replace("\r\n", "\n")
    sprintf "blob %d%c%s" s.Length (char 0) s |> calcSHA1

/// Shows the SHA1 calculated by git.
/// Assumes that the CommandHelper module can find git.exe.
[<System.Obsolete("Use Fake.Tools.Git.Sha1 instead")>]
let showObjectHash repositoryDir fileName =
    let _, msg, _ = runGitCommand repositoryDir (sprintf "hash-object %s" fileName)
    msg |> Seq.head
