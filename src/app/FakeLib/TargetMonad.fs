module TargetMonad

type Target = 
    { Name : string;
      Func: (unit -> string list)}

let targets = System.Collections.Generic.Dictionary<string,string list>()

let dependencies = System.Collections.Generic.Dictionary<string,Target>()

let (<==) comp1 comp2 = dependencies.Add(comp1.Name,comp2)

let rec runTarget (t:Target)  = 
    match targets.TryGetValue t.Name with
    | true, list -> list
    | _ -> 
        let output =
            match dependencies.TryGetValue t.Name with
            | true, target -> runTarget target
            | _ -> []

        let x = output @ t.Func()
        targets.Add(t.Name,x)
        x
        
let asTarget f =
    { Name = System.Guid.NewGuid().ToString()
      Func = f}

let (>>=) (m : Target) (k : unit -> Target) : Target =
    asTarget 
        (fun () ->
          let w = runTarget m
          let w' = runTarget (k())
          in ( w @ w'))



type TargetBuilder() =
 
  member this.Return a = asTarget (fun () -> [])
 
  member this.ReturnFrom(w : Target) = w
 
  member this.Bind(m,k) = m >>= k
    
  member this.Zero<'W>() = this.Return ()
 
  member this.TryWith<'W,'T>(target, handler : exn -> Target) =
    asTarget (fun () ->
      try runTarget target
      with e -> runTarget (handler e))
 
  member this.TryFinally<'W,'T>(target, compensation : unit -> unit) =
    asTarget (fun () ->
      try runTarget target
      finally compensation())
 
  member this.Using<'D,'W,'T when 'D :> System.IDisposable and 'D : null>(resource : 'D, body : 'D -> Target) =
    this.TryFinally(body resource, (fun () -> match resource with null -> () | disp -> disp.Dispose()))
 
  member this.Delay<'W,'T>(f : unit -> Target) =
    this.Bind(this.Return (), f)
 
  member this.Combine<'W,'T>(comp1 : Target, comp2 : Target) =
    this.Bind(comp1, (fun () -> comp2))
 
  member this.While<'W>(pred : unit -> bool, body : Target) =
    match pred() with
    | true -> this.Bind(body, (fun () -> this.While(pred,body)))
    | _ -> this.Return ()
 
  member this.For<'W,'T>(items : seq<'T>, body : 'T -> Target) =
    this.Using(items.GetEnumerator(),
      (fun enum -> this.While((fun () -> enum.MoveNext()), this.Delay(fun () -> body enum.Current))))
  
let target = new TargetBuilder()
 
let tell w = asTarget (fun () -> w)
 
let logMsg (s : string) = tell [s]

