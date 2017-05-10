module Fake.Runtime.LegacyApiHelper

type NewApiMutableHelper<'a> = { Set : 'a -> unit; Get : unit -> 'a }

let ofRef r = { Set = (fun v -> r := v); Get = (fun () -> !r) }
let toGetSet f = 
  (fun () ->
    let { Get = g } = f()
    g()),
  (fun v ->
    let { Set = s } = f()
    s v)

