[<AutoOpen>]
module Fake.TargetMonad

open System
open System.Collections.Generic
   
type TargetTemplate<'a> =
  {Name: string;
   Function : 'a -> string list}
   
type Target = TargetTemplate<unit>

/// Final Targets 
/// Stores target and if it is activated
let FinalTargets = new List<Target>()

/// TargetDependencies
let Dependencies = new Dictionary<string,Target list>()

/// Returns the dependecies for a given target
let getDependencies (target:Target) =
    match Dependencies.TryGetValue target.Name with
    | true, d -> d
    | _ -> []

/// The executed targets
let ExecutedTargets = new Dictionary<_,_>() 

let PrintDependencyGraph verbose (target:Target) =
    log <| sprintf "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target.Name 
    let printed = new HashSet<_>()
    let order = new List<_>()
    let rec printDependencies indent (act:Target) =
        let addToOrder = not (printed.Contains act.Name)
        printed.Add act.Name |> ignore
            
        if addToOrder || verbose then log <| (sprintf "<== %s" act.Name).PadLeft(3 * indent)
        getDependencies act 
          |> Seq.iter (printDependencies (indent+1))
        if addToOrder then order.Add act.Name
        
    printDependencies 0 target
    log ""
    log "The resulting target order is:"
    order |> Seq.iter (sprintf " - %s" >> log)    

/// runs all activated final targets (in alphabetically order)
let runFinalTargets() =
  FinalTargets
    |> Seq.fold (fun acc (target:Target) ->
         try             
           sprintf "Starting Finaltarget: %s" target.Name |> trace
           acc @ target.Function()
         with
         | exn -> [sprintf "Error in %s: %s" target.Name exn.Message])   
         []
    
/// Runs a Target and its dependencies
let rec runTarget (target:Target)  = 
    match ExecutedTargets.TryGetValue target.Name with
    | true, result -> result
    | _ ->
        try   
            let dependencies = getDependencies target
            let dependencyString =
                if dependencies.IsEmpty then String.Empty else
                sprintf "(==> %s)"
                  (dependencies
                    |> Seq.map (fun d -> d.Name)
                    |> separated ", ")

            traceStartTarget target.Name dependencyString

            // run dependencies
            let dependencyResult =
                dependencies
                    |> List.fold (fun acc d -> acc @ runTarget d) []

            let output = dependencyResult @ target.Function()
            ExecutedTargets.Add(target.Name,output)
            traceEndTarget target.Name
            output                
        with
        | exn -> [sprintf "Error in %s: %s" target.Name exn.Message]
    

            
let asTarget f =
    { Name = System.Guid.NewGuid().ToString()
      Function = f}   

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

