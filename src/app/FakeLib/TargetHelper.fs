[<AutoOpen>]
module Fake.TargetHelper
    
open System
open System.Collections.Generic

type TargetTemplate<'a> =
  {Name: string;
   Dependencies: string list;
   Function : 'a -> unit}     
   
type Target = TargetTemplate<unit>
   
/// TargetDictionary  
let TargetDict = new Dictionary<_,_>()

/// Final Targets 
/// Stores target and if it is activated
let FinalTargets = new Dictionary<_,_>()

/// The executed targets
let ExecutedTargets = new HashSet<_>()

/// Gets a Target from the Target dictionary
let getTarget x = 
  match TargetDict.TryGetValue x with
  | true, target -> target
  | _  -> failwith <| sprintf "Target \"%s\" is not defined." x   
    
/// Do nothing -  fun () -> ()   
let DoNothing = (fun () -> ())

/// Adds the dependency to the list of dependencies
let dependency targetName dependentTargetName =
  let target,dependentTarget = getTarget targetName,getTarget dependentTargetName
  let rec checkDependencies dependentTarget =
    dependentTarget.Dependencies |> List.iter (fun dep ->
      if dep = targetName then 
        failwith <| sprintf "Cyclic dependency between %s and %s" targetName dependentTarget.Name
      checkDependencies (getTarget dep))
      
  checkDependencies dependentTarget
    
  TargetDict.[targetName] <- 
    {target with 
       Dependencies = target.Dependencies @ [dependentTargetName]}
  
/// Adds the dependencies to the list of dependencies  
let Dependencies targetName = List.iter (dependency targetName)

/// Dependencies operator
let inline (<==) x y = Dependencies x y       
  
/// Creates a target from template
let targetFromTemplate template name parameters =    
  TargetDict.Add(name,
    {Name = name; 
     Dependencies = [];
     Function = fun () ->
       // Don't run function now
       template.Function parameters })

  name <== template.Dependencies
  
/// Creates a TargetTemplate with dependencies
let TargetTemplateWithDependecies dependencies body =
  { Name = String.Empty;
    Dependencies = dependencies;
    Function = body}     
      |> targetFromTemplate

/// Creates a TargetTemplate      
let TargetTemplate body = TargetTemplateWithDependecies [] body 
  
/// Creates a Target
let Target name body = TargetTemplate body name ()  

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
 
/// runs all activated final targets (in alphabetically order)
let runFinalTargets() =
  FinalTargets
    |> Seq.filter (fun kv -> kv.Value)     // only if activated
    |> Seq.map (fun kv -> kv.Key)
    |> Seq.iter (fun name ->
         try             
           sprintf "Starting Finaltarget: %s" name |> trace
           TargetDict.[name].Function()
         with
         | exn -> targetError name exn.Message)                     
              
let PrintDependencyGraph verbose target =
  log <| sprintf "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target 
  let printed = new HashSet<_>()
  let order = new List<_>()
  let rec printDependencies indent act =
    let target = TargetDict.[act]
    let addToOrder = not (printed.Contains act)
    printed.Add act |> ignore
        
    if addToOrder || verbose then log <| (sprintf "<== %s" act).PadLeft(3 * indent)
    target.Dependencies 
      |> Seq.iter (printDependencies (indent+1))
    if addToOrder then order.Add act
        
  printDependencies 0 target
  log ""
  log "The resulting target order is:"
  order |> Seq.iter (sprintf " - %s" >> log)     

/// Runs a Target and its dependencies        
let run targetName =
  let rec runTarget targetName =
    try      
      if ExecutedTargets.Contains targetName || errors <> [] then () else
      let target = getTarget targetName    
      let dependencyString =
        if target.Dependencies.IsEmpty then String.Empty else
        sprintf "(==> %s)"
          (target.Dependencies 
            |> Seq.map (fun d -> (getTarget d).Name)
            |> separated ", ")
      
      traceStartTarget target.Name dependencyString
      
      // run dependencies
      target.Dependencies |> List.iter runTarget
      
      if errors = [] then
        target.Function()
        ExecutedTargets.Add targetName |> ignore
        traceEndTarget target.Name                
    with
    | exn -> targetError targetName exn.Message        
         
  try    
    PrintDependencyGraph false targetName
    runTarget targetName
  finally
    runFinalTargets()
    errors |> List.iter raise    
 
/// Registers a final target (not activated)
let FinalTarget name body = 
    Target name body
    FinalTargets.Add(name,false)

/// Activates the FinalTarget
let ActivateFinalTarget name = 
    FinalTargets.[name] <- true

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
let Run = run