namespace Fake.Net.Async

module Async =
    let result = async.Return
    let map f value = async {
        let! v = value
        return f v
    }
    
    let bind f xAsync = async {
        let! x = xAsync
        return! f x
    }

    let apply fAsync xAsync = async {
        // start the two asyncs in parallel
        let! fChild = Async.StartChild fAsync
        let! xChild = Async.StartChild xAsync

        // wait for the results
        let! f = fChild
        let! x = xChild

        // apply the function to the results
        return f x
    }