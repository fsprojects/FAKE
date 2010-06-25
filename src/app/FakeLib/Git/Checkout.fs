[<AutoOpen>]
module Fake.Git.Checkout

/// Checks a branch out
let checkout repositoryDir create trackBranch branch =
    match trackBranch with
    | Some track -> gitCommandf repositoryDir "checkout --track -b %s %s" branch track
    | None -> 
        gitCommandf repositoryDir "checkout %s %s"
          (if create then "-b" else "")
          branch

/// Push all
let push repositoryDir = directRunGitCommand repositoryDir "push" |> ignore