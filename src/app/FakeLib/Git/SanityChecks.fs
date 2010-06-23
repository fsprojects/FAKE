[<AutoOpen>]
module Fake.Git.SanityChecks

open Fake

let checkRevisionExists revision1 =
  let ok1,_,errors1 = runGitCommand <| sprintf "log %s" revision1
  
  if not ok1 || errors1 <> "" then
      failwithf "Revision %s is not found in the current repository." revision1

/// Checks if the file exists on disk.
let checkFileExists fileName =
    let fi = new System.IO.FileInfo(fileName)
    if not fi.Exists then
        failwithf "File %s does not exist." fileName

/// Checks if the given branch exists.
let checkIfBranchExists branch = 
    if not (getAllBranches() |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists." branch

/// Checks if the given branch is absent.
let checkIfBranchIsAbsent branch = 
    if getAllBranches() |> List.exists ((=) branch) then
        failwithf "Branch %s exists but should be absent." branch

/// Checks if the given branch is a local branch.
let checkIsLocalBranch branch = 
    if not (getLocalBranches() |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists locally." branch

/// Checks if the given branch is a remote branch.
let checkIsRemoteBranch branch = 
    if not (getRemoteBranches() |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists remotely." branch