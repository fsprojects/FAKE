[<AutoOpen>]
module Fake.Git.Repository

let init bare shared =
    match bare, shared with
    | true, true -> gitCommand "init --bare --shared=all"
    | true, false -> gitCommand "init --bare"
    | false, true -> gitCommand "init --shared=all"
    | _ -> gitCommand "init"