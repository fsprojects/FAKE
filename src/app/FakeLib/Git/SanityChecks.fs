[<AutoOpen>]
/// Contains helper function which can be used for sanity checks.
module Fake.Git.SanityChecks

open Fake

/// Checks if the given branch exists.
let checkRevisionExists repositoryDir revision1 =
  let ok1,_,errors1 = runGitCommand repositoryDir <| sprintf "log %s" revision1
  
  if not ok1 || errors1 <> "" then
      failwithf "Revision %s is not found in the current repository." revision1


/// Checks if the given branch exists.
let checkIfBranchExists repositoryDir branch = 
    if not (getAllBranches repositoryDir |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists." branch

/// Checks if the given branch is absent.
let checkIfBranchIsAbsent repositoryDir branch = 
    if getAllBranches repositoryDir |> List.exists ((=) branch) then
        failwithf "Branch %s exists but should be absent." branch

/// Checks if the given branch is a local branch.
let checkIsLocalBranch repositoryDir branch = 
    if not (getLocalBranches repositoryDir |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists locally." branch

/// Checks if the given branch is a remote branch.
let checkIsRemoteBranch repositoryDir branch = 
    if not (getRemoteBranches repositoryDir |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists remotely." branch