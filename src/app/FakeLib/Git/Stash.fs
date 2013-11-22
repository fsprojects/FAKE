[<AutoOpen>]
/// Contains helper functions which allow to deal with git stash.
module Fake.Git.Stash

open Fake

/// Stash the changes in a dirty working directory away.
let push repositoryDir message =
    sprintf "stash save %s" message
      |> gitCommand repositoryDir

/// Remove a single stashed state from the stash list and 
/// apply it on top of the current working tree state, 
/// i.e., do the inverse operation of git stash save. 
/// The working directory must match the index. 
let pop repositoryDir = gitCommand repositoryDir "stash pop"