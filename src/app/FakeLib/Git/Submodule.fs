[<AutoOpen>]
module Fake.Git.Submodule

open Fake

type Submodule =
    {  Name: string;
       Branch: string;
       CurrentCommit: string;
       Initialized:bool;       
       SuperRepositoryDir: string;
       UpToDate: bool }
with 
    member x.Status =
        if not x.Initialized then "Not initialized" else
        if not x.UpToDate then "Modified" else
        "Up-to-date"
        
    member x.GetRemotePath() =
        x.Name.Trim()
          |> sprintf "config -f .gitmodules --get submodule.%s.url"
          |> runSimpleGitCommand x.SuperRepositoryDir

    member x.GetLocalPath() =
        x.Name.Trim()
          |> sprintf  "config -f .gitmodules --get submodule.%s.path"
          |> runSimpleGitCommand x.SuperRepositoryDir

let internal trimChars (s:string) = s.Trim [| '('; ')'; ' ' |]

/// Gets all submodules
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

/// Inits a submodule
let init superRepositoryDir name =
    if isNullOrEmpty name then "submodule update --init" else "submodule update --init \"" + name.Trim() + "\""
      |> gitCommand superRepositoryDir

/// Adds a submodule to the current repository.
///  params: superRepositoryDir
///  params: remote Path
///  params: local Path
///  params: branch (can be null)
let add superRepositoryDir remotePath localPath branch =
    sprintf "submodule add \"%s\" \"%s\" %s"
      (remotePath |> fixPath)
      (localPath |> fixPath)
      (if isNullOrEmpty branch then "" else " \"" + branch.Trim() + "\"")
      |> gitCommand superRepositoryDir