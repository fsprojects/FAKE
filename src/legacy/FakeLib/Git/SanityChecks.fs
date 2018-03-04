[<AutoOpen>]
/// Contains helper function which can be used for sanity checks.
[<System.Obsolete("Use Fake.Tools.Git.SanityChecks instead")>]
module Fake.Git.SanityChecks
#nowarn "44"

open Fake

/// Checks if the given branch exists.
[<System.Obsolete("Use Fake.Tools.Git.SanityChecks instead")>]
let checkRevisionExists repositoryDir revision1 =
  let ok1,_,errors1 = runGitCommand repositoryDir <| sprintf "log %s" revision1
  
  if not ok1 || errors1 <> "" then
      failwithf "Revision %s is not found in the current repository." revision1


/// Checks if the given branch exists.
[<System.Obsolete("Use Fake.Tools.Git.SanityChecks instead")>]
let checkIfBranchExists repositoryDir branch = 
    if not (getAllBranches repositoryDir |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists." branch

/// Checks if the given branch is absent.
[<System.Obsolete("Use Fake.Tools.Git.SanityChecks instead")>]
let checkIfBranchIsAbsent repositoryDir branch = 
    if getAllBranches repositoryDir |> List.exists ((=) branch) then
        failwithf "Branch %s exists but should be absent." branch

/// Checks if the given branch is a local branch.
[<System.Obsolete("Use Fake.Tools.Git.SanityChecks instead")>]
let checkIsLocalBranch repositoryDir branch = 
    if not (getLocalBranches repositoryDir |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists locally." branch

/// Checks if the given branch is a remote branch.
[<System.Obsolete("Use Fake.Tools.Git.SanityChecks instead")>]
let checkIsRemoteBranch repositoryDir branch = 
    if not (getRemoteBranches repositoryDir |> List.exists ((=) branch)) then
        failwithf "Branch %s doesn't exists remotely." branch