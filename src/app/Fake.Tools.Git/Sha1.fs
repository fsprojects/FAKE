namespace Fake.Tools.Git

open Fake.Core
open System.Security.Cryptography


/// Contains functions which allow the SHA1 of a file with git and without it.
[<RequireQualifiedAccess>]
module SHA1 =

    /// Calculates the SHA1 for a given string.
    ///
    /// ## Parameters
    ///  - `text` - The string to calculate SHA1 for
    let calcSHA1 (text: string) =
        Environment.getDefaultEncoding().GetBytes text
        |> SHA1.Create().ComputeHash
        |> Array.fold
            (fun acc e ->
                let t = System.Convert.ToString(e, 16)
                if t.Length = 1 then acc + "0" + t else acc + t)
            ""

    /// Calculates the SHA1 for a given string like git.
    ///
    /// ## Parameters
    ///  - `text` - The string to calculate SHA1 for
    let calcGitSHA1 (text: string) =
        let s = text.Replace("\r\n", "\n")
        sprintf "blob %d%c%s" s.Length (char 0) s |> calcSHA1

    /// Shows the SHA1 calculated by git.
    /// Assumes that the CommandHelper module can find git.exe.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The repository directory to execute command in
    ///  - `fileName` - The file name to show SHA1 for
    let showObjectHash repositoryDir fileName =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir (sprintf "hash-object %s" fileName)
        msg |> Seq.head
