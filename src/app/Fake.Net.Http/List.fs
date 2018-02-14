namespace Fake.Net.List

open Fake.Net.Async
open Fake.Net.Result

/// [omit]
// List extensions for traversing Result and Async types
// Functions from fsharpforfunandprofit.com, please see details here:
// https://fsharpforfunandprofit.com/posts/elevated-world-5/
module internal List =

    /// [omit]
    /// Map a Async producing function over a list to get a new Async 
    /// using applicative style
    /// ('a -> Async<'b>) -> 'a list -> Async<'b list>
    let rec traverseAsyncA f list =

        // define the applicative functions
        let (<*>) = Async.apply
        let retn = Async.result

        // define a "cons" function
        let cons head tail = head :: tail

        // right fold over the list
        let initState = retn []
        let folder head tail = 
            retn cons <*> (f head) <*> tail

        List.foldBack folder list initState 

    /// [omit]
    /// Transform a "list<Async>" into a "Async<list>" 
    /// and collect the results using apply.
    let sequenceAsyncA x = traverseAsyncA id x

    /// [omit]
    /// Map a Result producing function over a list to get a new Result 
    /// using applicative style
    /// ('a -> Result<'b>) -> 'a list -> Result<'b list>
    let rec traverseResultA f list =

        // define the applicative functions
        let (<*>) = Result.apply
        let retn = Ok

        // define a "cons" function
        let cons head tail = head :: tail

        // right fold over the list
        let initState = retn []
        let folder head tail = 
            retn cons <*> (f head) <*> tail

        List.foldBack folder list initState 

    /// [omit]
    /// Transform a "list<Result>" into a "Result<list>" 
    /// and collect the results using apply.
    let sequenceResultA x = traverseResultA id x

