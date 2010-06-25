[<AutoOpen>]
module Fake.Git.Staging

/// Adds a file to the staging area
let StageFile repositoryDir file =
  file 
    |> fixPath
    |> sprintf "update-index --add \"%s\""
    |> runGitCommand repositoryDir