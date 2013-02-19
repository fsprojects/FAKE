[<AutoOpen>]
module Fake.TargetHelper
    
open System
open System.Collections.Generic

type TargetDescription = string

type 'a TargetTemplate =
    { Name: string;
      Dependencies: string list;
      Description: TargetDescription;
      Function : 'a -> unit}
   
type Target = unit TargetTemplate

let mutable PrintStackTraceOnError = false

let mutable LastDescription = null
   
/// Sets the Description for the next target
let Description text = 
    if LastDescription <> null then 
        failwithf "You can't set the description for a target twice. There is already a description: %A" LastDescription
    LastDescription <- text

/// TargetDictionary  
let TargetDict = new Dictionary<_,_>()

/// Final Targets - stores final target and if it is activated
let FinalTargets = new Dictionary<_,_>()

/// The executed targets
let ExecutedTargets = new HashSet<_>()

/// The executed target time
let ExecutedTargetTimes = new List<_>()

/// Gets a target with the given name from the target dictionary
let getTarget name = 
    match TargetDict.TryGetValue (toLower name) with
    | true, target -> target
    | _  -> failwithf "Target \"%s\" is not defined." name

/// Returns the DependencyString for the given target
let dependencyString target =
    if target.Dependencies.IsEmpty then String.Empty else
    target.Dependencies 
      |> Seq.map (fun d -> (getTarget d).Name)
      |> separated ", "
      |> sprintf "(==> %s)"

/// Returns a list with all targetNames
let getAllTargetsNames() = TargetDict |> Seq.map (fun t -> t.Key) |> Seq.toList
    
/// Do nothing - fun () -> ()   
let DoNothing = (fun () -> ())

/// Checks wether the dependency can be add
let checkIfDependencyCanBeAdded targetName dependentTargetName =
    let target = getTarget targetName
    let dependentTarget = getTarget dependentTargetName

    let rec checkDependencies dependentTarget =
        dependentTarget.Dependencies 
          |> List.iter (fun dep ->
               if toLower dep = toLower targetName then 
                  failwithf "Cyclic dependency between %s and %s" targetName dependentTarget.Name
               checkDependencies (getTarget dep))
      
    checkDependencies dependentTarget
    target,dependentTarget

/// Adds the dependency to the front of the list of dependencies
let dependencyAtFront targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName
    
    TargetDict.[toLower targetName] <- { target with Dependencies = dependentTargetName :: target.Dependencies }
  
/// Appends the dependency to the list of dependencies
let dependencyAtEnd targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName
    
    TargetDict.[toLower targetName] <- { target with Dependencies = target.Dependencies @ [dependentTargetName] }

/// Adds the dependency to the list of dependencies
let dependency = dependencyAtEnd
  
/// Adds the dependencies to the list of dependencies  
let Dependencies targetName = List.iter (dependency targetName)

/// Dependencies operator
let inline (<==) x y = Dependencies x y

/// Set a dependency for all given targets
let TargetsDependOn target targets =
    getAllTargetsNames()
    |> Seq.toList  // work on copy since the dict will be changed
    |> List.filter ((<>) target)
    |> List.filter (fun t -> Seq.contains t targets)
    |> List.iter (fun t -> dependencyAtFront t target)

/// Set a dependency for all registered targets
let AllTargetsDependOn target = getAllTargetsNames() |> TargetsDependOn target
  
/// Creates a target from template
let targetFromTemplate template name parameters =    
    TargetDict.Add(toLower name,
      { Name = name; 
        Dependencies = [];
        Description = template.Description;
        Function = fun () ->
          // Don't run function now
          template.Function parameters })
    
    name <== template.Dependencies
    LastDescription <- null

/// Creates a TargetTemplate with dependencies
let TargetTemplateWithDependecies dependencies body =
    { Name = String.Empty;
      Dependencies = dependencies;
      Description = LastDescription;
      Function = body}     
        |> targetFromTemplate

/// Creates a TargetTemplate      
let TargetTemplate body = TargetTemplateWithDependecies [] body 
  
/// Creates a Target
let Target name body = TargetTemplate body name ()  

type private BuildError(target, msg) =
    inherit System.Exception(sprintf "Stopped build! Error occured in target \"%s\"." target)
    member e.Target = target
    new (msg : string) = BuildError("[Unknown]", msg)

let mutable private errors = []   
 
let targetError targetName (exn:System.Exception) =
    closeAllOpenTags()
    errors <- BuildError(targetName, exn.ToString()) :: errors
    let msg = 
        if PrintStackTraceOnError then exn.ToString() else
        sprintf "%O%s" exn (if exn.InnerException <> null then "\n" + (exn.InnerException.ToString()) else "")
            
    traceError <| sprintf "Running build failed.\nError:\n%s" msg

    let tcMsg = sprintf "%s" exn.Message
    sendTeamCityError tcMsg        
 
let addExecutedTarget target time =
    ExecutedTargets.Add (toLower target) |> ignore
    ExecutedTargetTimes.Add(toLower target,time) |> ignore

/// Runs all activated final targets (in alphabetically order)
let runFinalTargets() =
    FinalTargets
      |> Seq.filter (fun kv -> kv.Value)     // only if activated
      |> Seq.map (fun kv -> kv.Key)
      |> Seq.iter (fun name ->
           try             
               let watch = new System.Diagnostics.Stopwatch()
               watch.Start()
               tracefn "Starting Finaltarget: %s" name
               TargetDict.[toLower name].Function()
               addExecutedTarget name watch.Elapsed
           with
           | exn -> targetError name exn)

/// Prints all targets
let PrintTargets() =
    log "The following targets are available:"
    for t in TargetDict.Values do
        logfn "   %s%s" t.Name (if isNullOrEmpty t.Description then "" else sprintf " - %s" t.Description)
              
/// <summary>Writes a dependency graph.</summary>
/// <param name="verbose">Whether to print verbose output or not.</param>
/// <param name="target">The target for which the dependencies should be printed.</param>
let PrintDependencyGraph verbose target =
    match TargetDict.TryGetValue (toLower target) with
    | false,_ -> PrintTargets()
    | true,target ->
        logfn "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target.Name
        let printed = new HashSet<_>()
        let order = new List<_>()
        let rec printDependencies indent act =
            let target = TargetDict.[toLower act]
            let addToOrder = not (printed.Contains (toLower act))
            printed.Add (toLower act) |> ignore
    
            if addToOrder || verbose then log <| (sprintf "<== %s" act).PadLeft(3 * indent)
            Seq.iter (printDependencies (indent+1)) target.Dependencies
            if addToOrder then order.Add act
        
        printDependencies 0 target.Name
        log ""
        log "The resulting target order is:"
        Seq.iter (logfn " - %s") order

/// <summary>Writes a build time report.</summary>
/// <param name="total">The total runtime.</param>
let WriteTaskTimeSummary total =    
    traceHeader "Build Time Report"
    if ExecutedTargets.Count > 0 then
        let width = 
            ExecutedTargetTimes 
              |> Seq.map (fun (a,b) -> a.Length) 
              |> Seq.max
              |> max 8

        let aligned (name:string) duration = tracefn "%s   %O" (name.PadRight width) duration
        let alignedError (name:string) duration = sprintf "%s   %O" (name.PadRight width) duration |> traceError

        aligned "Target" "Duration"
        aligned "------" "--------"
        ExecutedTargetTimes
          |> Seq.iter (fun (name,time) -> 
                let t = getTarget name
                aligned t.Name time)

        aligned "Total:" total
        if errors = [] then aligned "Status:" "Ok" else alignedError "Status:" "Failure"
    else 
        traceError "No target was successfully completed"

    traceLine()

let private changeExitCodeIfErrorOccured() = if errors <> [] then exit 42 

let isListMode = hasBuildParam "list"

let listTargets() =
    tracefn "Available targets:"
    TargetDict.Values
      |> Seq.iter (fun target -> 
            tracefn "  - %s %s" target.Name (if target.Description <> null then " - " + target.Description else "")
            tracefn "     Depends on: %A" target.Dependencies)

/// <summary>Runs a target and its dependencies</summary>
/// <param name="targetName">The target to run.</param>
let run targetName =            
    if isListMode then listTargets() else
    if LastDescription <> null then failwithf "You set a task description (%A) but didn't specify a task." LastDescription
    let rec runTarget targetName =
        try      
            if errors = [] && ExecutedTargets.Contains (toLower targetName) |> not then
                let target = getTarget targetName      
                traceStartTarget target.Name target.Description (dependencyString target)
      
                List.iter runTarget target.Dependencies
      
                if errors = [] then
                    let watch = new System.Diagnostics.Stopwatch()
                    watch.Start()
                    target.Function()
                    addExecutedTarget targetName watch.Elapsed
                    traceEndTarget target.Name                
        with
        | exn -> targetError targetName exn
      
    let watch = new System.Diagnostics.Stopwatch()
    watch.Start()        
    try
        tracefn "Building project with version: %s" buildVersion
        PrintDependencyGraph false targetName
        runTarget targetName
    finally
        runFinalTargets()
        WriteTaskTimeSummary watch.Elapsed
        changeExitCodeIfErrorOccured()
 
/// Registers a final target (not activated)
let FinalTarget name body = 
    Target name body
    FinalTargets.Add(toLower name,false)

/// Activates the FinalTarget
let ActivateFinalTarget name = 
    let t = getTarget name // test if target is defined
    FinalTargets.[toLower name] <- true