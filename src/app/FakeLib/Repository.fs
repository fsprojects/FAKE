[<AutoOpen>]
module Fake.Git.Repository

let init bare shared =
    match bare, shared with
    | true, true -> CommandHelper.gitCommand "init --bare --shared=all"
    | true, false -> CommandHelper.gitCommand "init --bare"
    | false, true -> CommandHelper.gitCommand "init --shared=all"
    | _ -> CommandHelper.gitCommand "init"