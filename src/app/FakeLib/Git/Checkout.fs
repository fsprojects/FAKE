[<AutoOpen>]
module Fake.Git.Checkout

/// Checks a branch out
let checkout create trackBranch branch =
    match trackBranch with
    | Some track -> CommandHelper.gitCommandf "checkout --track -b %s %s" branch track
    | None -> 
        CommandHelper.gitCommandf "checkout %s %s"
          (if create then "-b" else "")
          branch