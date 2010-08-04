module Fake.Git.Rebase

open Fake

/// Performs a rebase on top of the given branch with the current branch
let start repositoryDir onTopOfBranch =
    sprintf "rebase %s" onTopOfBranch
      |> gitCommand repositoryDir

/// Restore the original branch and abort the rebase operation. 
let abort repositoryDir = gitCommand repositoryDir "rebase --abort"

/// Restart the rebasing process after having resolved a merge conflict. 
let continueRebase repositoryDir = gitCommand repositoryDir "rebase --continue"

/// Restart the rebasing process by skipping the current patch. 
let skip repositoryDir = gitCommand repositoryDir "rebase --skip"