module FSharp.Nullable 

open System 

module Option =
    /// Converts a nullable object into an F# option
    let fromNullable (n: _ Nullable) = 
        if n.HasValue
            then Some n.Value
            else None

    /// Converts an F# option into a nullable object
    let toNullable =
        function
        | None -> Nullable()
        | Some x -> Nullable(x)