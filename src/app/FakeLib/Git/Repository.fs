[<AutoOpen>]
module Fake.Git.Repository

/// Clones a git repository
let clone workingDir repoUrl toPath =  gitCommand workingDir (sprintf "clone %s %s" repoUrl toPath)

/// Inits a git repository
let init repositoryDir bare shared =
    match bare, shared with
    | true, true -> gitCommand repositoryDir "init --bare --shared=all"
    | true, false -> gitCommand repositoryDir "init --bare"
    | false, true -> gitCommand repositoryDir "init --shared=all"
    | _ -> gitCommand repositoryDir "init"