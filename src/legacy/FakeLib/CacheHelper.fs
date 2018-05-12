[<AutoOpen>]
/// Contains functions which allows to deal with a cache.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.CacheHelper

open System.Collections.Generic

/// Looks for a key in the cache.
/// If it is not found the newValueF function is executed and the result is stored in the cache.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let lookup key newValueF (cache : IDictionary<_, _>) = 
    match cache.TryGetValue key with
    | true, value -> value
    | false, _ -> 
        let newValue = newValueF()
        cache.[key] <- newValue
        newValue
