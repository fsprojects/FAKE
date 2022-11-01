namespace Fake.Core

module Operators =
    /// <summary>
    /// Apply given parameter to two callbacks and return results from both callbacks
    /// </summary>
    let inline (>!>) func1 func2 x =
        func1 x
        func2 x
