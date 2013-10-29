namespace Fake

// Copied from https://github.com/colinbull/FSharp.Enterprise/blob/master/src/FSharp.Enterprise/IO.fs
module IO =
    
    open System
    open System.Runtime.Caching
    open Caching

    type SearchPattern = string
    type Filter = (string -> bool)

    type IIO<'a> =
        abstract member Write : string * 'a -> unit
        abstract member Read : string -> 'a option
        abstract member ReadAll : string * Filter -> seq<'a>
        abstract member ReadAll : seq<SearchPattern> -> seq<'a>
        abstract member Delete : string -> unit
    
    let IO readOp writeOp deleteOp searchOp =
        { new IIO<_> with
            member f.Write(path, payload) = (path,payload) |> writeOp 
            member f.Read(path) = readOp path
            member f.ReadAll(path, filter) = 
                    seq {
                        for file in (searchOp path) |> Seq.filter filter do
                            match f.Read(file) with
                            | Some(a) -> yield a 
                            | None -> ()
                    }
            member f.ReadAll(patterns) = 
                patterns
                |> Seq.collect searchOp
                |> Seq.choose f.Read
            member f.Delete(path) = deleteOp path
        }

    let CachedIO (expiry:TimeSpan) readOp writeOp deleteOp searchOp =
        let onRemoved reason path payload =
            match reason with
            | CacheEntryRemovedReason.Expired -> (path,payload) |> writeOp
            | _ -> ()
        let cache : ICache<string,'a> = Caching.ObjectCache onRemoved MemoryCache.Default
        { new IIO<'a> with
            member f.Write(path, payload) = cache.Set(path, SlidingExpiry(payload,expiry))
            member f.Read(path) =
                match cache.TryGet(path) with
                | Some(v) -> Some v
                | None -> readOp path
            member f.ReadAll(path, filter) = 
                seq {
                    for file in (searchOp path) |> Seq.filter filter do
                        match f.Read(file) with
                        | Some(a) -> yield a 
                        | None -> ()
                }
            member f.ReadAll(patterns) = 
                patterns
                |> Seq.collect (searchOp)
                |> Seq.choose f.Read
            member f.Delete(path) =
               cache.Remove(path) |> ignore
               deleteOp (path) 
        }