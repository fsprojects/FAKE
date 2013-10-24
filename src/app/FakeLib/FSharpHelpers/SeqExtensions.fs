[<AutoOpen>]
/// Contains extensions of the defaul Seq module.
module Fake.Seq

/// Returns if the given element exists in the sequence
let contains element = Seq.exists ((=) element)

/// Tries to execute the given function on the given item and ignores possible exceptions.
let performSafe f item =
    try
      f item
    with
    | exn -> ()

/// Tries to execute the given function on all items in the collection and ignores possible exceptions.
let performSafeOnEveryItem f = Seq.iter (performSafe f)
          