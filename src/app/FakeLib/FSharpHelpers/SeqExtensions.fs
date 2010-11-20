[<AutoOpen>]
module Fake.Seq

/// Returns true if the given element exists in the sequence
let contains element = Seq.exists ((=) element)