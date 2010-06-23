[<AutoOpen>]
module Fake.Git.Checkout

/// Checks a branch out
let checkout create trackBranch branch =
    match trackBranch with
    | Some track -> gitCommandf "checkout --track -b %s %s" branch track
    | None -> 
        gitCommandf "checkout %s %s"
          (if create then "-b" else "")
          branch