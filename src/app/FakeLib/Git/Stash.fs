[<AutoOpen>]
/// Contains helper functions which allow to deal with git stash.
[<System.Obsolete("Use Fake.Tools.Git.Stash instead")>]
module Fake.Git.Stash

#nowarn "44"
open Fake

/// Stash the changes in a dirty working directory away.
[<System.Obsolete("Use Fake.Tools.Git.Stash instead")>]
let push repositoryDir message =
    sprintf "stash save %s" message
      |> gitCommand repositoryDir

/// Remove a single stashed state from the stash list and 
/// apply it on top of the current working tree state, 
/// i.e., do the inverse operation of git stash save. 
/// The working directory must match the index. 
[<System.Obsolete("Use Fake.Tools.Git.Stash instead")>]
let pop repositoryDir = gitCommand repositoryDir "stash pop"