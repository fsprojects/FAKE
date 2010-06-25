[<AutoOpen>]
module Fake.Git.Repository

/// Clones a git repository
let clone repoUrl toPath =  gitCommand (sprintf "clone %s %s" repoUrl toPath)

/// Inits a git repository
let init bare shared =
    match bare, shared with
    | true, true -> gitCommand "init --bare --shared=all"
    | true, false -> gitCommand "init --bare"
    | false, true -> gitCommand "init --shared=all"
    | _ -> gitCommand "init"