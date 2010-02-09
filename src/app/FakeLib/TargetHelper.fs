[<AutoOpen>]
module Fake.TargetHelper
    
open System
open System.Collections.Generic
  
/// Do nothing -  fun () -> ()   
let DoNothing = target { () }

/// Adds the dependency to the list of dependencies
let dependency target dependentTarget =
  let rec checkDependencies dependentTarget =
    getDependencies dependentTarget |> List.iter (fun dep ->
      if dep.Name = target.Name then 
        failwith <| sprintf "Cyclic dependency between %s and %s" target.Name dependentTarget.Name
      checkDependencies dep)
      
  checkDependencies dependentTarget
  Dependencies.[target.Name] <- getDependencies target @ [dependentTarget]
  
/// Adds the dependencies to the list of dependencies  
let Dependencies targetName = List.iter (dependency targetName)

/// Dependencies operator
let inline (<==) x y = Dependencies x y       
  
type BuildError(target, msg) =
  inherit System.Exception(sprintf "Stopped build! Error occured in target \"%s\".\r\nMessage: %s" target msg)
  member e.Target = target
  new (msg : string) = BuildError("[Unknown]", msg)

let mutable errors = []   

let targetError targetName msg =
  closeAllOpenTags()
  errors <- BuildError(targetName, msg) :: errors
  traceError <| sprintf "Running build failed.\nError:\n%s" msg
  sendTeamCityError msg                                       

/// Allows to use Tokens instead of strings
let (?) f s = f s

/// Allows to use Tokens instead of strings for TargetNames
let (?<-) f str action = f str action

/// Allows to use For? syntax for Dependencies
let For x y = x <== y

/// Converts a dependency into a list
let Dependency x = [x]

/// Appends the dependency to the list of dependencies
let And x y = y @ [x]

/// Runs a Target and its dependencies
let Run target = 
    let output = ref []
    try    
        PrintDependencyGraph false target
        output := runTarget target
    finally 
        output := !output @ runFinalTargets()
        // TODO: errors |> List.iter raise   
    !output