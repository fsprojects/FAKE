module Fake.Git.Rebase

open Fake

/// Performs a rebase on top of the given branch with the current branch
let start repositoryDir onTopOfBranch =
    sprintf "rebase %s" onTopOfBranch
      |> gitCommand repositoryDir

/// Aborts a running rebase
let abort repositoryDir = gitCommand repositoryDir "rebase --abort"