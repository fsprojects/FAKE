namespace Fake

// Copied from https://github.com/colinbull/FSharp.Enterprise/blob/master/src/FSharp.Enterprise/Caching.fs
module Caching = 
     
     open System
     open System.Collections.Generic
     open System.Runtime.Caching


     type Key<'a> = 'a
     type Item<'a> =
         | SlidingExpiry of 'a * TimeSpan
         | AbsoluteExpiry of 'a * DateTimeOffset
         | NoExpiry of 'a

     type ICache<'a,'b> =
         abstract member Set : Key<'a> * Item<'b> -> unit
         abstract member TryGet : Key<'a> -> 'b option
         abstract member Remove : Key<'a> -> bool

     let DictionaryCache (dict:IDictionary<_,_>) = 
         { new ICache<_,_> with 
             member x.Set(key,item) = 
                 match item with
                 | SlidingExpiry(item,_) -> dict.[key] <- item
                 | AbsoluteExpiry(item,_) -> dict.[key] <- item
                 | NoExpiry(item) -> dict.[key] <- item
             member x.TryGet(key) =
                 match dict.TryGetValue(key) with
                 | (true, v) -> Some v
                 | (false, _) -> None
             member x.Remove(key) = dict.Remove(key)
         }

     let ObjectCache onRemoved (c:ObjectCache) = 
         let itemRemovedHandler = new CacheEntryRemovedCallback(fun x -> onRemoved x.RemovedReason x.CacheItem.Key (unbox<'a> x.CacheItem.Value))
         { new ICache<string,'a> with 
             member x.Set(key,item) = 
                 match item with
                 | SlidingExpiry(item,ex) -> c.Set(key, item, new CacheItemPolicy(SlidingExpiration = ex, RemovedCallback = itemRemovedHandler))
                 | AbsoluteExpiry(item,ex) -> c.Set(key, item, new CacheItemPolicy(AbsoluteExpiration = ex, RemovedCallback = itemRemovedHandler))
                 | NoExpiry(item) -> c.Set(key, item, new CacheItemPolicy(RemovedCallback = itemRemovedHandler))
             member x.TryGet(key) =
                 match c.Get(key) with
                 | null -> None
                 | ci -> Some (unbox<'a> ci)
             member x.Remove(key) =
                 if c.Contains(key)
                 then c.Remove(key) |> ignore; true
                 else false
         }

     let memoizeWithKeyAndExpiry f keyf expiryf (cache:ICache<_,_>) = 
         fun n ->
             let key = keyf n
             match cache.TryGet(key) with
             | Some(v) -> v
             | _ ->
                 let temp = f(n)
                 cache.Set(key, expiryf(temp))
                 temp

     let memoizeWithExpiry f expiryf (cache:ICache<_,_>) = 
         fun n ->
             match cache.TryGet(n) with
             | Some(v) -> v
             | _ ->
                 let temp = f(n)
                 cache.Set(n, expiryf(temp))
                 temp
     
     let memoize f (cache:ICache<_,_>) =
         memoizeWithExpiry f (NoExpiry) cache