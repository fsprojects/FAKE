[<AutoOpen>]
module Fake.TargetHelper
    
open System
open System.Collections.Generic

type TargetTemplate<'a> =
    { Name: string;
      Dependencies: string list;
      Function : 'a -> unit}
   
type Target = TargetTemplate<unit>

let mutable PrintStackTraceOnError = false
   
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
    match TargetDict.TryGetValue name with
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
let checkIfDependencyCanBeAdd targetName dependentTargetName =
    let target = getTarget targetName
    let dependentTarget = getTarget dependentTargetName

    let rec checkDependencies dependentTarget =
        dependentTarget.Dependencies 
          |> List.iter (fun dep ->
               if dep = targetName then 
                  failwithf "Cyclic dependency between %s and %s" targetName dependentTarget.Name
               checkDependencies (getTarget dep))
      
    checkDependencies dependentTarget
    target,dependentTarget

/// Adds the dependency to the front of the list of dependencies
let dependencyAtFront targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdd targetName dependentTargetName
    
    TargetDict.[targetName] <- { target with Dependencies = dependentTargetName :: target.Dependencies }
  
/// Appends the dependency to the list of dependencies
let dependencyAtEnd targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdd targetName dependentTargetName
    
    TargetDict.[targetName] <- { target with Dependencies = target.Dependencies @ [dependentTargetName] }

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
    TargetDict.Add(name,
      { Name = name; 
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
        sprintf "%s%s" exn.Message (if exn.InnerException <> null then "\n" + exn.InnerException.Message else "")
            
    traceError <| sprintf "Running build failed.\nError:\n%s" msg
    sendTeamCityError msg        
 
let addExecutedTarget target time =
    ExecutedTargets.Add target |> ignore
    ExecutedTargetTimes.Add(target,time) |> ignore

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
               TargetDict.[name].Function()
               addExecutedTarget name watch.Elapsed
           with
           | exn -> targetError name exn)
              
/// <summary>Writes a dependency graph.</summary>
/// <param name="verbose">Whether to print verbose output or not.</param>
/// <param name="target">The target for which the dependencies should be printed.</param>
let PrintDependencyGraph verbose target =
    logfn "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target 
    let printed = new HashSet<_>()
    let order = new List<_>()
    let rec printDependencies indent act =
        let target = TargetDict.[act]
        let addToOrder = not (printed.Contains act)
        printed.Add act |> ignore
    
        if addToOrder || verbose then log <| (sprintf "<== %s" act).PadLeft(3 * indent)
        Seq.iter (printDependencies (indent+1)) target.Dependencies
        if addToOrder then order.Add act
        
    printDependencies 0 target
    log ""
    log "The resulting target order is:"
    Seq.iter (logfn " - %s") order

/// <summary>Writes a build time report.</summary>
/// <param name="total">The total runtime.</param>
let WriteTaskTimeSummary total =    
    traceHeader "Build Time Report"
    let width = 
        ExecutedTargetTimes 
          |> Seq.map (fun (a,b) -> a.Length) 
          |> Seq.max
          |> max 8
    let aligned (name:string) duration = tracefn "%s   %O" (name.PadRight width) duration

    aligned "Target" "Duration"
    aligned "------" "--------"
    ExecutedTargetTimes
      |> Seq.iter (fun (name,time) -> aligned name time)

    aligned "Total:" total
    traceLine()

let private changeExitCodeIfErrorOccured() = if errors <> [] then exit 42 

/// <summary>Runs a target and its dependencies</summary>
/// <param name="targetName">The target to run.</param>
let run targetName =            
    let rec runTarget targetName =
        try      
            if errors = [] && ExecutedTargets.Contains targetName |> not then
                let target = getTarget targetName      
                traceStartTarget target.Name (dependencyString target)
      
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
        WaitUntilEverythingIsPrinted()
        PrintDependencyGraph false targetName
        runTarget targetName
    finally
        runFinalTargets()
        WaitUntilEverythingIsPrinted()
        WriteTaskTimeSummary watch.Elapsed
        WaitUntilEverythingIsPrinted()
        changeExitCodeIfErrorOccured()
 
/// Registers a final target (not activated)
let FinalTarget name body = 
    Target name body
    FinalTargets.Add(name,false)

/// Activates the FinalTarget
let ActivateFinalTarget name = 
    let t = getTarget name // test if target is defined
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

/// Runs the target given by the build script parameter or the given default target
let RunParameterTargetOrDefault parameterName defaultTarget = getBuildParamOrDefault parameterName defaultTarget |> Run

/// Stores which targets are on the same level
let private sameLevels = new Dictionary<_,_>()

let targetsAreOnSameLevel x y =
    match sameLevels.TryGetValue y with
    | true, z -> failwithf "Target %s is already on same level with %s" x z
    | _  -> sameLevels.[y] <- x

let rec addDependenciesOnSameLevel target dependency =
    match sameLevels.TryGetValue dependency with
    | true, x -> 
        addDependenciesOnSameLevel target x
        Dependencies target [x]
    | _  -> ()

/// Defines a dependency - y is dependent on x
let inline (==>) x y =
    addDependenciesOnSameLevel y x 
    Dependencies y [x]

    y

/// Defines that x and y are not dependent on each other but y is dependent on all dependencies of x.
let inline (<=>) x y =   
    let target_x = getTarget x
    Dependencies y target_x.Dependencies
    targetsAreOnSameLevel x y
    y