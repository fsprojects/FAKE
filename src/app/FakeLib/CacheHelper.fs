[<AutoOpen>]
module Fake.CacheHelper

open System.Collections.Generic

/// Looks for a key in the cache.
/// If it is not found the newValue functions is executed and the result is stored in the cache.
let lookup key newValueF (cache:IDictionary<_,_>) =
    match cache.TryGetValue key with
    | true, value -> value
    | false, _ ->      
        let newValue = newValueF()
        cache.[key] <- newValue
        newValue