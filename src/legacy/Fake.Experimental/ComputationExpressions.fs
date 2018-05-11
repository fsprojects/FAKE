[<AutoOpen>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.ComputationExpressions

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type TargetBuilder (name) =
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Zero () = ()
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Delay f = f
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Run f = Target name f

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.While (guard, body) : unit =
        if not (guard()) 
        then x.Zero() 
        else 
            body()
            x.While (guard, body)  
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.TryWith (body, handler) = try body() with e -> handler e

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.TryFinally(body, compensation) = try body() finally compensation() 

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Using (disposable: #System.IDisposable, body) =
        let body' = fun () -> body disposable
        x.TryFinally(body', fun () -> 
            match disposable with 
                | null -> () 
                | disp -> disp.Dispose())

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.For(sequence: seq<_>, body) =
      x.Using(sequence.GetEnumerator(), fun enum -> 
            x.While(enum.MoveNext, 
                x.Delay(fun () -> body enum.Current)))

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Target name = TargetBuilder(name)

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type FileSetBuilder() =
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Delay f = f
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Run f = f()
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Yield (()) = { Include "" with Includes = [] }
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    [<CustomOperation ("add", MaintainsVariableSpace = true)>]
    member x.Add (fs, pattern: string) = fs ++ pattern

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    [<CustomOperation ("addMany", MaintainsVariableSpace = true)>]
    member x.AddMany (fs, patterns: string list) = { fs with Includes = fs.Includes @ patterns }

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    [<CustomOperation ("exclude", MaintainsVariableSpace = true)>]
    member x.Exclude (fs: FileIncludes, pattern: string) = fs -- pattern

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    [<CustomOperation ("excludeMany", MaintainsVariableSpace = true)>]
    member x.ExcludeMany (fs: FileIncludes, patterns: string list) = { fs with Excludes = fs.Excludes @ patterns }
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let files = FileSetBuilder()
