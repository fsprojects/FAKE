[<AutoOpen>]
module Fake.ComputationExpressions

type TargetBuilder (name) =
    member x.Zero () = ()
    member x.Delay f = f
    member x.Run f = Target name f

    member x.While (guard, body) : unit =
        if not (guard()) 
        then x.Zero() 
        else 
            body()
            x.While (guard, body)  
    
    member x.TryWith (body, handler) = try body() with e -> handler e
    member x.TryFinally(body, compensation) = try body() finally compensation() 

    member x.Using (disposable: #System.IDisposable, body) =
        let body' = fun () -> body disposable
        x.TryFinally(body', fun () -> 
            match disposable with 
                | null -> () 
                | disp -> disp.Dispose())

    member x.For(sequence: seq<_>, body) =
      x.Using(sequence.GetEnumerator(), fun enum -> 
            x.While(enum.MoveNext, 
                x.Delay(fun () -> body enum.Current)))

let Target name = TargetBuilder(name)

type FileSetBuilder() =
    member x.Delay f = f
    member x.Run f = f()
    member x.Yield (()) = { Include "" with Includes = [] }
    
    [<CustomOperation ("add", MaintainsVariableSpace = true)>]
    member x.Add (fs, pattern: string) = fs ++ pattern
 
    [<CustomOperation ("addMany", MaintainsVariableSpace = true)>]
    member x.AddMany (fs, patterns: string list) = { fs with Includes = fs.Includes @ patterns }

    [<CustomOperation ("exclude", MaintainsVariableSpace = true)>]
    member x.Exclude (fs: FileIncludes, pattern: string) = fs -- pattern

    [<CustomOperation ("excludeMany", MaintainsVariableSpace = true)>]
    member x.ExcludeMany (fs: FileIncludes, patterns: string list) = { fs with Excludes = fs.Excludes @ patterns }
 
let files = FileSetBuilder()
