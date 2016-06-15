[<AutoOpen>]
/// Contains infrastructure code and helper functions for FAKE's target feature.
module Fake.TargetHelper

open System
open System.Collections.Generic
open System.Linq

/// [omit]
type TargetDescription = string

/// [omit]
type 'a TargetTemplate =
    { Name: string;
      Dependencies: string list;
      SoftDependencies: string list;
      Description: TargetDescription;
      Function : 'a -> unit}

/// A Target can be run during the build
type Target = unit TargetTemplate

type private DependencyType =
    | Hard = 1
    | Soft = 2

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
let TargetDict = new Dictionary<_,_>(StringComparer.OrdinalIgnoreCase)

/// Final Targets - stores final targets and if they are activated.
let FinalTargets = new Dictionary<_,_>(StringComparer.OrdinalIgnoreCase)

/// BuildFailureTargets - stores build failure targets and if they are activated.
let BuildFailureTargets = new Dictionary<_,_>(StringComparer.OrdinalIgnoreCase)

/// The executed targets.
let ExecutedTargets = new HashSet<_>(StringComparer.OrdinalIgnoreCase)

/// The executed target time.
/// [omit]
let ExecutedTargetTimes = new List<_>()

/// Resets the state so that a deployment can be invoked multiple times
/// [omit]
let reset() =
    TargetDict.Clear()
    ExecutedTargets.Clear()
    BuildFailureTargets.Clear()
    ExecutedTargetTimes.Clear()
    FinalTargets.Clear()

/// Returns a list with all target names.
let getAllTargetsNames() = TargetDict |> Seq.map (fun t -> t.Key) |> Seq.toList

/// Gets a target with the given name from the target dictionary.
let getTarget name =
    match TargetDict.TryGetValue (name) with
    | true, target -> target
    | _  ->
        traceError <| sprintf "Target \"%s\" is not defined. Existing targets:" name
        for target in TargetDict do
            traceError  <| sprintf "  - %s" target.Value.Name
        failwithf "Target \"%s\" is not defined." name

/// Returns the DependencyString for the given target.
let dependencyString target =
    if target.Dependencies.IsEmpty then String.Empty else
    target.Dependencies
      |> Seq.map (fun d -> (getTarget d).Name)
      |> separated ", "
      |> sprintf "(==> %s)"

/// Returns the soft  DependencyString for the given target.
let softDependencyString target =
    if target.SoftDependencies.IsEmpty then String.Empty else
    target.SoftDependencies
      |> Seq.map (fun d -> (getTarget d).Name)
      |> separated ", "
      |> sprintf "(?=> %s)"

/// Do nothing - fun () -> () - Can be used to define empty targets.
let DoNothing = (fun () -> ())

/// Checks whether the dependency (soft or normal) can be added.
/// [omit]
let checkIfDependencyCanBeAddedCore fGetDependencies targetName dependentTargetName =
    let target = getTarget targetName
    let dependentTarget = getTarget dependentTargetName

    let rec checkDependencies dependentTarget =
          fGetDependencies dependentTarget
          |> List.iter (fun dep ->
               if toLower dep = toLower targetName then
                  failwithf "Cyclic dependency between %s and %s" targetName dependentTarget.Name
               checkDependencies (getTarget dep))

    checkDependencies dependentTarget
    target,dependentTarget

/// Checks whether the dependency can be added.
/// [omit]
let checkIfDependencyCanBeAdded targetName dependentTargetName =
   checkIfDependencyCanBeAddedCore (fun target -> target.Dependencies) targetName dependentTargetName

/// Checks whether the soft dependency can be added.
/// [omit]
let checkIfSoftDependencyCanBeAdded targetName dependentTargetName =
   checkIfDependencyCanBeAddedCore (fun target -> target.SoftDependencies) targetName dependentTargetName

/// Adds the dependency to the front of the list of dependencies.
/// [omit]
let dependencyAtFront targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName

    TargetDict.[targetName] <- { target with Dependencies = dependentTargetName :: target.Dependencies }

/// Appends the dependency to the list of dependencies.
/// [omit]
let dependencyAtEnd targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName

    TargetDict.[targetName] <- { target with Dependencies = target.Dependencies @ [dependentTargetName] }


/// Appends the dependency to the list of soft dependencies.
/// [omit]
let softDependencyAtEnd targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName

    TargetDict.[targetName] <- { target with SoftDependencies = target.SoftDependencies @ [dependentTargetName] }

/// Adds the dependency to the list of dependencies.
/// [omit]
let dependency targetName dependentTargetName = dependencyAtEnd targetName dependentTargetName

/// Adds the dependency to the list of soft dependencies.
/// [omit]
let softDependency targetName dependentTargetName = softDependencyAtEnd targetName dependentTargetName

/// Adds the dependencies to the list of dependencies.
/// [omit]
let Dependencies targetName dependentTargetNames = dependentTargetNames |> List.iter (dependency targetName)

/// Adds the dependencies to the list of soft dependencies.
/// [omit]
let SoftDependencies targetName dependentTargetNames = dependentTargetNames |> List.iter (softDependency targetName)

/// Backwards dependencies operator - x is dependent on ys.
let inline (<==) x ys = Dependencies x ys

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
    TargetDict.Add(name,
      { Name = name;
        Dependencies = [];
        SoftDependencies = [];
        Description = template.Description;
        Function = fun () ->
          // Don't run function now
          template.Function parameters })

    name <== template.Dependencies
    LastDescription <- null

/// Creates a TargetTemplate with dependencies.
///
/// ## Sample
///
/// The following sample creates 4 targets using TargetTemplateWithDependencies and hooks them into the build pipeline.
///
///     // Create target creation functions
///     let createCompileTarget name strategy =
///     TargetTemplateWithDependencies
///         ["Clean"; "ResolveDependencies"] // dependencies to other targets
///         (fun targetParameter ->
///           tracefn "--- start compile product..."
///           if targetParameter = "a" then
///             tracefn "    ---- Strategy A"
///           else
///             tracefn "    ---- Strategy B"
///           tracefn "--- finish compile product ..."
///         ) name strategy
///
///     let createTestTarget name dependencies filePattern =
///       TargetTemplateWithDependencies
///         dependencies
///         (fun filePattern ->
///           tracefn "--- start compile tests ..."
///           !! filePattern
///           |> RunTests
///           tracefn "--- finish compile tests ...")
///         name filePattern
///
///     // create some targets
///     createCompileTarget "C1" "a"
///     createCompileTarget "C2" "b"
///
///     createTestTarget "T1" ["C1"] "**/C1/*.*"
///     createTestTarget "T2" ["C1"; "C2"] "**/C?/*.*"
///
///     // hook targets to normal build pipeline
///     "T1" ==> "T2" ==> "Test"
///
let TargetTemplateWithDependencies dependencies body name parameters =
    let template =
        { Name = String.Empty
          Dependencies = dependencies
          SoftDependencies = []
          Description = LastDescription
          Function = body }
    targetFromTemplate template name parameters

[<Obsolete("Use TargetTemplateWithDependencies")>]
let TargetTemplateWithDependecies dependencies = TargetTemplateWithDependencies dependencies

/// Creates a TargetTemplate.
let TargetTemplate body = TargetTemplateWithDependencies [] body

/// Creates a Target.
let Target name body = TargetTemplate body name ()

/// Represents build errors
type BuildError = {
    Target : string
    Message : string }

let mutable private errors = []

/// Get Errors - Returns the errors that occured during execution
let GetErrors() = errors

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
        | BuildException(msg, errs) -> msg + (if PrintStackTraceOnError then Environment.NewLine + e.StackTrace.ToString() else "")
        | _ ->
            if exn :? FAKEException then
                exn.Message
            else
                exn.ToString()


    let msg = sprintf "%s%s" (error exn) (if exn.InnerException <> null then "\n" + (exn.InnerException |> error) else "")
    traceError <| sprintf "Running build failed.\nError:\n%s" msg

    let isFailedTestsException = exn :? UnitTestCommon.FailedTestsException
    if not isFailedTestsException  then
        sendTeamCityError <| error exn

let addExecutedTarget target time =
    lock ExecutedTargets (fun () ->
        ExecutedTargets.Add (target) |> ignore
        ExecutedTargetTimes.Add(target,time) |> ignore
    )

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
               tracefn "Starting FinalTarget: %s" name
               (getTarget name).Function()
               addExecutedTarget name watch.Elapsed
           with
           | exn -> targetError name exn)

/// Runs all build failure targets.
/// [omit]
let runBuildFailureTargets() =
    BuildFailureTargets
      |> Seq.filter (fun kv -> kv.Value)     // only if activated
      |> Seq.map (fun kv -> kv.Key)
      |> Seq.iter (fun name ->
           try
               let watch = new System.Diagnostics.Stopwatch()
               watch.Start()
               tracefn "Starting BuildFailureTarget: %s" name
               (getTarget name).Function()
               addExecutedTarget name watch.Elapsed
           with
           | exn -> targetError name exn)


/// Prints all targets.
let PrintTargets() =
    log "The following targets are available:"
    for t in TargetDict.Values do
        logfn "   %s%s" t.Name (if isNullOrEmpty t.Description then "" else sprintf " - %s" t.Description)


// Maps the specified dependency type into the list of targets
let private withDependencyType (depType:DependencyType) targets =
    targets |> List.map (fun t -> depType, t)

// Helper function for visiting targets in a dependency tree. Returns a set containing the names of the all the
// visited targets, and a list containing the targets visited ordered such that dependencies of a target appear earlier
// in the list than the target.
let private visitDependencies fVisit targetName =
    let visit fGetDependencies fVisit targetName =
        let visited = new HashSet<_>()
        let ordered = new List<_>()
        let rec visitDependenciesAux level (depType,targetName) =
            let target = getTarget targetName
            let isVisited = visited.Contains targetName
            visited.Add targetName |> ignore
            fVisit (target, depType, level, isVisited)
            (fGetDependencies target) |> Seq.iter (visitDependenciesAux (level + 1))
            if not isVisited then ordered.Add targetName
        visitDependenciesAux 0 (DependencyType.Hard, targetName)
        visited, ordered

    // First pass is to accumulate targets in (hard) dependency graph
    let visited, _ = visit (fun t -> t.Dependencies |> withDependencyType DependencyType.Hard) (fun _ -> ()) targetName

    let getAllDependencies (t: TargetTemplate<unit>) =
         (t.Dependencies |> withDependencyType DependencyType.Hard) @
         // Note that we only include the soft dependency if it is present in the set of targets that were
         // visited.
         (t.SoftDependencies |> List.filter visited.Contains |> withDependencyType DependencyType.Soft)

    // Now make second pass, adding in soft depencencies if appropriate
    visit getAllDependencies fVisit targetName



/// <summary>Writes a dependency graph.</summary>
/// <param name="verbose">Whether to print verbose output or not.</param>
/// <param name="target">The target for which the dependencies should be printed.</param>
let PrintDependencyGraph verbose target =
    match TargetDict.TryGetValue (target) with
    | false,_ -> PrintTargets()
    | true,target ->
        logfn "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target.Name

        let logDependency ((t: TargetTemplate<unit>), depType, level, isVisited) =
            if verbose ||  not isVisited then
                let indent = (String(' ', level * 3))
                if depType = DependencyType.Soft then
                    log <| sprintf "%s<=? %s" indent t.Name
                else
                    log <| sprintf "%s<== %s" indent t.Name

        let _, ordered = visitDependencies logDependency target.Name

        log ""
        log "The resulting target order is:"
        Seq.iter (logfn " - %s") ordered

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

module ExitCode =
    let exitCode = ref 0
let private changeExitCodeIfErrorOccured() = if errors <> [] then Environment.ExitCode <- 42; ExitCode.exitCode := 42

/// [omit]
let isListMode = hasBuildParam "list"

/// Prints all available targets.
let listTargets() =
    tracefn "Available targets:"
    TargetDict.Values
      |> Seq.iter (fun target ->
            tracefn "  - %s %s" target.Name (if target.Description <> null then " - " + target.Description else "")
            tracefn "     Depends on: %A" target.Dependencies)

// Instead of the target can be used the list dependencies graph parameter.
let doesTargetMeanListTargets target = target = "--listTargets"  || target = "-lt"


/// Determines a parallel build order for the given set of targets
let determineBuildOrder (target : string) =

    let t = getTarget target

    let targetLevels = new Dictionary<_,_>()
    let addTargetLevel ((target: TargetTemplate<unit>), _, level, _ ) =
        match targetLevels.TryGetValue target.Name with
        | true, mapLevel when mapLevel >= level -> ()
        | _ -> targetLevels.[target.Name] <- level

    let visited, ordered = visitDependencies addTargetLevel target

    // the results are grouped by their level, sorted descending (by level) and
    // finally grouped together in a list<TargetTemplate<unit>[]>
    let result =
        targetLevels
        |> Seq.map (fun pair -> pair.Key, pair.Value)
        |> Seq.groupBy snd
        |> Seq.sortBy (fun (l,_) -> -l)
        |> Seq.map snd
        |> Seq.map (fun v -> v |> Seq.map fst |> Seq.distinct |> Seq.map getTarget |> Seq.toArray)
        |> Seq.toList

    // Note that this build order cannot be considered "optimal"
    // since it may introduce order where actually no dependencies
    // exist. However it yields a "good" execution order in practice.
    result

/// Runs a single target without its dependencies
let runSingleTarget (target : TargetTemplate<unit>) =
    try
        if errors = [] then
            traceStartTarget target.Name target.Description (dependencyString target)
            let watch = new System.Diagnostics.Stopwatch()
            watch.Start()
            target.Function()
            addExecutedTarget target.Name watch.Elapsed
            traceEndTarget target.Name
    with exn ->
        targetError target.Name exn

/// Runs the given array of targets in parallel using count tasks
let runTargetsParallel (count : int) (targets : Target[]) =
    targets.AsParallel()
        .WithDegreeOfParallelism(count)
        .Select(runSingleTarget)
        .ToArray()
    |> ignore

let mutable CurrentTargetOrder = []

/// Runs a target and its dependencies.
let run targetName =
    if doesTargetMeanListTargets targetName then listTargets() else
    if LastDescription <> null then failwithf "You set a task description (%A) but didn't specify a task." LastDescription

    let rec runTargets (targets: TargetTemplate<unit> array) =
        let lastTarget = targets |> Array.last
        if errors = [] && ExecutedTargets.Contains (lastTarget.Name) |> not then
           let firstTarget = targets |> Array.head
           if hasBuildParam "single-target" then
               traceImportant "Single target mode ==> Skipping dependencies."
               runSingleTarget lastTarget
           else
               targets |> Array.iter runSingleTarget

    let watch = new System.Diagnostics.Stopwatch()
    watch.Start()
    try
        tracefn "Building project with version: %s" buildVersion
        let parallelJobs = environVarOrDefault "parallel-jobs" "1" |> int

        // Figure out the order in in which targets can be run, and which can be run in parallel.
        if parallelJobs > 1 then
            tracefn "Running parallel build with %d workers" parallelJobs

            // determine a parallel build order
            let order = determineBuildOrder targetName

            CurrentTargetOrder <-
                order
                |> List.map (fun targets -> targets |> Array.map (fun t -> t.Name) |> Array.toList)

            // run every level in parallel
            for par in order do
                runTargetsParallel parallelJobs par

        else
            // single threaded build.
            PrintDependencyGraph false targetName

            // Note: we could use the ordering resulting from flattening the result of determineBuildOrder
            // for a single threaded build (thereby centralizing the algorithm for build order), but that
            // ordering is inconsistent with earlier versions of FAKE (and PrintDependencyGraph).
            let _, ordered = visitDependencies ignore targetName
            CurrentTargetOrder <- ordered |> Seq.map (fun t -> [t]) |> Seq.toList

            runTargets (ordered |> Seq.map getTarget |> Seq.toArray)

    finally
        if errors <> [] then
            runBuildFailureTargets()
        runFinalTargets()
        killAllCreatedProcesses()
        WriteTaskTimeSummary watch.Elapsed
        changeExitCodeIfErrorOccured()

/// Registers a BuildFailureTarget (not activated).
let BuildFailureTarget name body =
    Target name body
    BuildFailureTargets.Add(name,false)

/// Activates the BuildFailureTarget.
let ActivateBuildFailureTarget name =
    let t = getTarget name // test if target is defined
    BuildFailureTargets.[name] <- true

/// Registers a final target (not activated).
let FinalTarget name body =
    Target name body
    FinalTargets.Add(name,false)

/// Activates the FinalTarget.
let ActivateFinalTarget name =
    let t = getTarget name // test if target is defined
    FinalTargets.[name] <- true
