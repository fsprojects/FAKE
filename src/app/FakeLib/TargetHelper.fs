[<AutoOpen>]
/// Contains infrastructure code and helper functions for FAKE's target feature.
module Fake.TargetHelper
    
open System
open System.Collections.Generic

/// [omit]
type TargetDescription = string

/// [omit]
type 'a TargetTemplate =
    { Name: string;
      Dependencies: string list;
      Description: TargetDescription;
      Function : 'a -> unit}
   
/// A Target can be run during the build
type Target = unit TargetTemplate

/// [omit]
let mutable PrintStackTraceOnError = false

/// [omit]
let mutable LastDescription = null
   
/// Sets the Description for the next target.
/// [omit]
let Description text = 
    if LastDescription <> null then 
        failwithf "You can't set the description for a target twice. There is already a description: %A" LastDescription
    LastDescription <- text

/// TargetDictionary
/// [omit]
let TargetDict = new Dictionary<_,_>()

/// Final Targets - stores final targets and if they are activated.
let FinalTargets = new Dictionary<_,_>()

/// The executed targets.
let ExecutedTargets = new HashSet<_>()

/// The executed target time.
/// [omit]
let ExecutedTargetTimes = new List<_>()

/// Gets a target with the given name from the target dictionary.
let getTarget name = 
    match TargetDict.TryGetValue (toLower name) with
    | true, target -> target
    | _  -> failwithf "Target \"%s\" is not defined." name

/// Returns the DependencyString for the given target.
let dependencyString target =
    if target.Dependencies.IsEmpty then String.Empty else
    target.Dependencies 
      |> Seq.map (fun d -> (getTarget d).Name)
      |> separated ", "
      |> sprintf "(==> %s)"

/// Returns a list with all target names.
let getAllTargetsNames() = TargetDict |> Seq.map (fun t -> t.Key) |> Seq.toList
    
/// Do nothing - fun () -> () - Can be used to define empty targets.
let DoNothing = (fun () -> ())

/// Checks whether the dependency can be added.
/// [omit]
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

/// Adds the dependency to the front of the list of dependencies.
/// [omit]
let dependencyAtFront targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName
    
    TargetDict.[toLower targetName] <- { target with Dependencies = dependentTargetName :: target.Dependencies }
  
/// Appends the dependency to the list of dependencies.
/// [omit]
let dependencyAtEnd targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName
    
    TargetDict.[toLower targetName] <- { target with Dependencies = target.Dependencies @ [dependentTargetName] }

/// Adds the dependency to the list of dependencies.
/// [omit]
let dependency = dependencyAtEnd
  
/// Adds the dependencies to the list of dependencies.
/// [omit]
let Dependencies targetName = List.iter (dependency targetName)

/// Backwards dependencies operator - y is dependend on x.
let inline (<==) x y = Dependencies x y

/// Set a dependency for all given targets.
/// [omit]
[<Obsolete("Please use the ==> operator")>]
let TargetsDependOn target targets =
    getAllTargetsNames()
    |> Seq.toList  // work on copy since the dict will be changed
    |> List.filter ((<>) target)
    |> List.filter (fun t -> Seq.exists ((=) t) targets)
    |> List.iter (fun t -> dependencyAtFront t target)

/// Set a dependency for all registered targets.
/// [omit]
[<Obsolete("Please use the ==> operator")>]
let AllTargetsDependOn target = 
    let targets = getAllTargetsNames() 

    targets
    |> Seq.toList  // work on copy since the dict will be changed
    |> List.filter ((<>) target)
    |> List.filter (fun t -> Seq.exists ((=) t) targets)
    |> List.iter (fun t -> dependencyAtFront t target)
  
/// Creates a target from template.
/// [omit]
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

/// Creates a TargetTemplate with dependencies-
let TargetTemplateWithDependecies dependencies body =
    { Name = String.Empty;
      Dependencies = dependencies;
      Description = LastDescription;
      Function = body}     
        |> targetFromTemplate

/// Creates a TargetTemplate.
let TargetTemplate body = TargetTemplateWithDependecies [] body 
  
/// Creates a Target.
let Target name body = TargetTemplate body name ()  

/// Represents build errors
type BuildError = { 
    Target : string
    Message : string }

let mutable private errors = []   
 
/// [omit]
let targetError targetName (exn:System.Exception) =
    closeAllOpenTags()
    errors <- 
        match exn with
            | BuildException(msg, errs) -> 
                let errMsgs = errs |> List.map(fun e -> { Target = targetName; Message = e }) 
                { Target = targetName; Message = msg } :: (errMsgs @ errors)
            | _ -> { Target = targetName; Message = exn.ToString() } :: errors
    let error e =
        match e with
            | BuildException(msg, errs) -> msg, msg + Environment.NewLine + e.StackTrace.ToString()
            | _ -> exn.Message, exn.ToString()
    let msg =
        if PrintStackTraceOnError then error exn |> snd else
        sprintf "%s%s" (error exn |> snd) (if exn.InnerException <> null then "\n" + (exn.InnerException |> error |> snd ) else "")
            
    traceError <| sprintf "Running build failed.\nError:\n%s" msg
    sendTeamCityError (error exn |> snd)        
 
let addExecutedTarget target time =
    ExecutedTargets.Add (toLower target) |> ignore
    ExecutedTargetTimes.Add(toLower target,time) |> ignore

/// Runs all activated final targets (in alphabetically order).
/// [omit]
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

/// Prints all targets.
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

/// Writes a summary of errors reported during build.
let WriteErrors () =
    traceLine()
    errors
    |> Seq.mapi(fun i e -> sprintf "%3d) %s" (i + 1) e.Message)
    |> Seq.iter(fun s -> traceError s)

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
        if errors = [] then aligned "Status:" "Ok" 
        else 
            alignedError "Status:" "Failure"
            WriteErrors()
    else 
        traceError "No target was successfully completed"

    traceLine()

let private changeExitCodeIfErrorOccured() = if errors <> [] then exit 42 

/// [omit]
let isListMode = hasBuildParam "list"

/// Prints all available targets.
let listTargets() =
    tracefn "Available targets:"
    TargetDict.Values
      |> Seq.iter (fun target -> 
            tracefn "  - %s %s" target.Name (if target.Description <> null then " - " + target.Description else "")
            tracefn "     Depends on: %A" target.Dependencies)

/// Runs a target and its dependencies.
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
        killAllCreatedProcesses()
        WriteTaskTimeSummary watch.Elapsed
        changeExitCodeIfErrorOccured()
 
/// Registers a final target (not activated).
let FinalTarget name body = 
    Target name body
    FinalTargets.Add(toLower name,false)

/// Activates the FinalTarget.
let ActivateFinalTarget name = 
    let t = getTarget name // test if target is defined
    FinalTargets.[toLower name] <- true