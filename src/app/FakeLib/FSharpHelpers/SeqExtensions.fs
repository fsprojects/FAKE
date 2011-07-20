[<AutoOpen>]
module Fake.Seq

/// Returns true if the given element exists in the sequence
let contains element = Seq.exists ((=) element)

let performSafe f item =
    try
      f item
    with
    | exn -> ()

let performSafeOnEveryItem f = Seq.iter (performSafe f)
          