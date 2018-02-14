namespace Fake.Net.Result

/// [omit]
module internal Result =

    /// [omit]
    type ResultBuilder() =
        member __.Bind(m, f) = 
            match m with
            | Error e -> Error e
            | Ok a -> f a

        member __.Return(x) = 
            Ok x
    
    /// [omit]
    let apply fResult xResult = 
        match fResult,xResult with
        | Ok f, Ok x ->
            Ok (f x)
        | Error errs, Ok x ->
            Error errs
        | Ok f, Error errs ->
            Error errs
        | Error errs1, Error errs2 ->
            // concat both lists of errors
            Error (List.concat [errs1; errs2])
