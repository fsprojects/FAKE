[<AutoOpen>]
/// Contains functions which allows to deal with a cache.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.CacheHelper

open System.Collections.Generic

/// Looks for a key in the cache.
/// If it is not found the newValueF function is executed and the result is stored in the cache.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let lookup key newValueF (cache : IDictionary<_, _>) = 
    match cache.TryGetValue key with
    | true, value -> value
    | false, _ -> 
        let newValue = newValueF()
        cache.[key] <- newValue
        newValue
