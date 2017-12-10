namespace Fake.Net

module ResultBuilder =
    type ResultBuilder() =
        member __.Bind(m, f) = 
            match m with
            | Error e -> Error e
            | Ok a -> f a

        member __.Return(x) = 
            Ok x