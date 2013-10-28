[<AutoOpen>]
/// Contains helper functions which allow to deal with git submodules.
module Fake.Git.Submodule

open Fake

/// This record represents a git submodule binding.
type Submodule =
    { Name: string
      Branch: string
      CurrentCommit: string
      Initialized:bool
      SuperRepositoryDir: string
      UpToDate: bool }
with
    /// Gets the current status.
    member x.Status =
        if not x.Initialized then "Not initialized" else
        if not x.UpToDate then "Modified" else
        "Up-to-date"
    
    /// Gets the remote path from the config.
    member x.GetRemotePath() =
        x.Name.Trim()
          |> sprintf "config -f .gitmodules --get submodule.%s.url"
          |> runSimpleGitCommand x.SuperRepositoryDir

   /// Gets the local path from the config.
    member x.GetLocalPath() =
        x.Name.Trim()
          |> sprintf  "config -f .gitmodules --get submodule.%s.path"
          |> runSimpleGitCommand x.SuperRepositoryDir

let internal trimChars (s:string) = s.Trim [| '('; ')'; ' ' |]

/// Gets all submodules from the given repository directory.
let getSubModules repositoryDir =
    let ok,submodules,errors = runGitCommand repositoryDir "submodule status"

    submodules
      |> Seq.filter (fun submodule -> submodule.Length >= 43)
      |> Set.ofSeq  // remove duplicates
      |> Seq.map (fun submodule ->
            let n = submodule.Substring(42).Trim()
            let name,branch =
                if n.Contains "(" then
                    n.Substring(0, n.IndexOf "(") |> trimChars,
                    n.Substring(n.IndexOf "(") |> trimChars
                else
                    n,null
            {  Branch = branch;
               CurrentCommit = submodule.Substring(1, 40).Trim();
               Initialized = submodule.[0] <> '-';
               Name = name;
               SuperRepositoryDir = repositoryDir;
               UpToDate = submodule.[0] <> '+' })

/// Inits a submodule with the given name in a subfolder of the given super repository.
/// ## Parameters
///
///  - `superRepositoryDir` - The super repository.
///  - `name` - The name of the new repository.
let init superRepositoryDir name =
    if isNullOrEmpty name then "submodule update --init" else "submodule update --init \"" + name.Trim() + "\""
      |> gitCommand superRepositoryDir

/// Adds a submodule to the given super repository.
/// ## Parameters
///
///  - `superRepositoryDir` - The super repository.
///  - `remotePath` - The path to the remote repository of the submodule.
///  - `localPath` - The local path to the submodule.
///  - `branch` - The branch to  clone. (can be null)
let add superRepositoryDir remotePath localPath branch =
    sprintf "submodule add \"%s\" \"%s\" %s"
      (remotePath |> fixPath)
      (localPath |> fixPath)
      (if isNullOrEmpty branch then "" else " \"" + branch.Trim() + "\"")
      |> gitCommand superRepositoryDir