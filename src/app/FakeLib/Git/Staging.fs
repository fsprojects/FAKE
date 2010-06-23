[<AutoOpen>]
module Fake.Git.Staging

/// Adds a file to the staging area
let StageFile file =
  file 
    |> fixPath
    |> sprintf "update-index --add \"%s\""
    |> runGitCommand