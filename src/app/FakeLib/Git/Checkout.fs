[<AutoOpen>]
module Fake.Git.Checkout

/// Checks a branch out
let checkoutTracked repositoryDir create trackBranch branch =
    gitCommandf repositoryDir "checkout --track -b %s %s" branch trackBranch


/// Checks a branch out
let checkout repositoryDir create branch =
    gitCommandf repositoryDir "checkout %s %s"
        (if create then "-b" else "")
        branch

/// Push all
let push repositoryDir = directRunGitCommand repositoryDir "push" |> ignore