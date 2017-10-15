namespace Fake.Core

module Operators =
    let inline (>!>) func1 func2 x = func1 x; func2 x

