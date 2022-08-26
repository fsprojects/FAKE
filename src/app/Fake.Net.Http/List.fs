namespace Fake.Net.List

open Fake.Net.Async
open Fake.Net.Result

/// <summary>
/// List extensions for traversing Result and Async types
/// Functions from <a href="fsharpforfunandprofit.com">fsharpforfunandprofit.com</a>, please see details
/// <a href="https://fsharpforfunandprofit.com/posts/elevated-world-5/">here</a> 
/// </summary>
module internal List =

    /// <summary>
    /// Map a Async producing function over a list to get a new Async 
    /// using applicative style
    /// <c>('a -&gt; Async&lt;&apos;b&gt;) -> &apos;a list -&gt; Async&lt;&apos;b list&gt;</c>
    /// </summary>
    /// [omit]
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

    /// <summary>
    /// Transform a <c>list&lt;Async&gt;</c> into a <c>Async&lt;list&gt;</c> 
    /// and collect the results using apply.
    /// </summary>
    /// [omit]
    let sequenceAsyncA x = traverseAsyncA id x

    /// <summary>
    /// Map a Result producing function over a list to get a new Result 
    /// using applicative style
    /// <c>('a -&gt; Result&lt;&apos;b&gt;) -> &apos;a list -&gt; Result&lt;&apos;b list&gt;</c>
    /// </summary>
    /// [omit]
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

    /// <summary>
    /// Transform a <c>list&lt;Result&gt;</c> into a <c>Result&lt;list&gt;</c> 
    /// and collect the results using apply.
    /// </summary>
    /// [omit]
    let sequenceResultA x = traverseResultA id x

