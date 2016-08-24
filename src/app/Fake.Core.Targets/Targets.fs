namespace Fake.Core

open System
open System.Collections.Generic
open System.Linq
open Fake.Core

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

module Targets =
    /// [omit]
    //let mutable PrintStackTraceOnError = false
    let private printStackTraceOnErrorVar = "Fake.Core.Targets.PrintStackTraceOnError"
    let private getPrintStackTraceOnError, _, (setPrintStackTraceOnError:bool -> unit) = 
        Fake.Core.Context.fakeVar printStackTraceOnErrorVar
    
    /// [omit]
    //let mutable LastDescription = null
    let private lastDescriptionVar = "Fake.Core.Targets.LastDescription"
    let private getLastDescription, removeLastDescription, setLastDescription = 
        Fake.Core.Context.fakeVar lastDescriptionVar

    /// Sets the Description for the next target.
    /// [omit]
    let Description text =
        match getLastDescription() with
        | Some (v:string) ->
            failwithf "You can't set the description for a target twice. There is already a description: %A" v
        | None ->
           setLastDescription text

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
            Trace.traceError <| sprintf "Target \"%s\" is not defined. Existing targets:" name
            for target in TargetDict do
                Trace.traceError  <| sprintf "  - %s" target.Value.Name
            failwithf "Target \"%s\" is not defined." name

    /// Returns the DependencyString for the given target.
    let dependencyString target =
        if target.Dependencies.IsEmpty then String.Empty else
        target.Dependencies
          |> Seq.map (fun d -> (getTarget d).Name)
          |> String.separated ", "
          |> sprintf "(==> %s)"

    /// Returns the soft  DependencyString for the given target.
    let softDependencyString target =
        if target.SoftDependencies.IsEmpty then String.Empty else
        target.SoftDependencies
          |> Seq.map (fun d -> (getTarget d).Name)
          |> String.separated ", "
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
                   if String.toLower dep = String.toLower targetName then
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
        removeLastDescription()

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
              Description = match getLastDescription() with Some d -> d | None -> null
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

    //let mutable private errors = []
    let private errorsVar = "Fake.Core.Targets.errors"
    let private getErrors, removeErrors, setErrors = 
        Fake.Core.Context.fakeVar errorsVar

    /// Get Errors - Returns the errors that occured during execution
    let GetErrors() = 
      match getErrors () with
      | Some e -> e
      | None -> []

    /// [omit]
    let targetError targetName (exn:System.Exception) =
        Trace.closeAllOpenTags()
        setErrors
            (match exn with
                //| BuildException(msg, errs) ->
                //    let errMsgs = errs |> List.map(fun e -> { Target = targetName; Message = e })
                //    { Target = targetName; Message = msg } :: (errMsgs @ errors)
                | _ -> { Target = targetName; Message = exn.ToString() } :: GetErrors())
        let error e =
            match e with
            //| BuildException(msg, errs) -> msg + (if PrintStackTraceOnError then Environment.NewLine + e.StackTrace.ToString() else "")
            | _ ->
                if exn :? Trace.FAKEException then
                    exn.Message
                else
                    exn.ToString()


        let msg = sprintf "%s%s" (error exn) (if not <| isNull exn.InnerException then "\n" + (exn.InnerException |> error) else "")
        Trace.traceError <| sprintf "Running build failed.\nError:\n%s" msg

        //let isFailedTestsException = exn :? UnitTestCommon.FailedTestsException
        //if not isFailedTestsException  then
        //    sendTeamCityError <| error exn

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
                   Trace.tracefn "Starting FinalTarget: %s" name
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
                   Trace.tracefn "Starting BuildFailureTarget: %s" name
                   (getTarget name).Function()
                   addExecutedTarget name watch.Elapsed
               with
               | exn -> targetError name exn)


    /// Prints all targets.
    let PrintTargets() =
        Trace.log "The following targets are available:"
        for t in TargetDict.Values do
            Trace.logfn "   %s%s" t.Name (if String.isNullOrEmpty t.Description then "" else sprintf " - %s" t.Description)


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
        let visited, _ = visit (fun t -> t.Dependencies |> withDependencyType DependencyType.Hard) ignore targetName

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
            Trace.logfn "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target.Name

            let logDependency ((t: TargetTemplate<unit>), depType, level, isVisited) =
                if verbose ||  not isVisited then
                    let indent = (String(' ', level * 3))
                    if depType = DependencyType.Soft then
                        Trace.log <| sprintf "%s<=? %s" indent t.Name
                    else
                        Trace.log <| sprintf "%s<== %s" indent t.Name

            let _, ordered = visitDependencies logDependency target.Name

            Trace.log ""
            Trace.log "The resulting target order is:"
            Seq.iter (Trace.logfn " - %s") ordered

    /// Writes a summary of errors reported during build.
    let WriteErrors () =
        Trace.traceLine()
        GetErrors()
        |> Seq.mapi(fun i e -> sprintf "%3d) %s" (i + 1) e.Message)
        |> Seq.iter Trace.traceError

    /// <summary>Writes a build time report.</summary>
    /// <param name="total">The total runtime.</param>
    let WriteTaskTimeSummary total =
        Trace.traceHeader "Build Time Report"
        if ExecutedTargets.Count > 0 then
            let width =
                ExecutedTargetTimes
                  |> Seq.map (fun (a,b) -> a.Length)
                  |> Seq.max
                  |> max 8

            let aligned (name:string) duration = Trace.tracefn "%s   %O" (name.PadRight width) duration
            let alignedError (name:string) duration = sprintf "%s   %O" (name.PadRight width) duration |> Trace.traceError

            aligned "Target" "Duration"
            aligned "------" "--------"
            ExecutedTargetTimes
              |> Seq.iter (fun (name,time) ->
                    let t = getTarget name
                    aligned t.Name time)

            aligned "Total:" total
            if List.isEmpty (GetErrors()) then aligned "Status:" "Ok"
            else
                alignedError "Status:" "Failure"
                WriteErrors()
        else
            Trace.traceError "No target was successfully completed"

        Trace.traceLine()

    /// [omit]
    let isListMode = Environment.hasEnvironVar "list"

    /// Prints all available targets.
    let listTargets() =
        Trace.tracefn "Available targets:"
        TargetDict.Values
          |> Seq.iter (fun target ->
                Trace.tracefn "  - %s %s" target.Name (if not <| isNull target.Description then " - " + target.Description else "")
                Trace.tracefn "     Depends on: %A" target.Dependencies)

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
            |> Seq.map (snd >> Seq.map fst >> Seq.distinct >> Seq.map getTarget >> Seq.toArray)
            |> Seq.toList

        // Note that this build order cannot be considered "optimal"
        // since it may introduce order where actually no dependencies
        // exist. However it yields a "good" execution order in practice.
        result

    /// Runs a single target without its dependencies
    let runSingleTarget (target : TargetTemplate<unit>) =
        try
            if List.isEmpty (GetErrors()) then
                use t = Trace.traceTarget target.Name target.Description (dependencyString target)
                let watch = new System.Diagnostics.Stopwatch()
                watch.Start()
                target.Function()
                addExecutedTarget target.Name watch.Elapsed
        with exn ->
            targetError target.Name exn

    /// Runs the given array of targets in parallel using count tasks
    let runTargetsParallel (count : int) (targets : Target[]) =
        targets
        |> Array.map (fun t -> async { runSingleTarget t })
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously
        //    .AsParallel()
        //    .WithDegreeOfParallelism(count)
        //    .Select()
        //    .ToArray()
        |> ignore

    //let mutable CurrentTargetOrder = []
    
    let private currentTargetOrderVar = "Fake.Core.Targets.CurrentTargetOrder"
    let private getCurrentTargetOrder, removeCurrentTargetOrder, setCurrentTargetOrder = 
        Fake.Core.Context.fakeVar currentTargetOrderVar

    /// Runs a target and its dependencies.
    let run targetName =
        if doesTargetMeanListTargets targetName then listTargets() else
        match getLastDescription() with
        | Some d -> failwithf "You set a task description (%A) but didn't specify a task." d
        | None -> ()

        let rec runTargets (targets: TargetTemplate<unit> array) =
            let lastTarget = targets |> Array.last
            if List.isEmpty (GetErrors()) && ExecutedTargets.Contains (lastTarget.Name) |> not then
               let firstTarget = targets |> Array.head
               if Environment.hasEnvironVar "single-target" then
                   Trace.traceImportant "Single target mode ==> Skipping dependencies."
                   runSingleTarget lastTarget
               else
                   targets |> Array.iter runSingleTarget

        printfn "run %s" targetName
        let watch = new System.Diagnostics.Stopwatch()
        watch.Start()
        try
            Trace.tracefn "Building project with version: %s" BuildServer.buildVersion
            let parallelJobs = Environment.environVarOrDefault "parallel-jobs" "1" |> int

            // Figure out the order in in which targets can be run, and which can be run in parallel.
            if parallelJobs > 1 then
                Trace.tracefn "Running parallel build with %d workers" parallelJobs

                // determine a parallel build order
                let order = determineBuildOrder targetName

                order
                |> List.map (Array.map (fun t -> t.Name) >> Array.toList)
                |> setCurrentTargetOrder

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

                ordered
                |> Seq.map (fun t -> [t]) 
                |> Seq.toList
                |> setCurrentTargetOrder

                runTargets (ordered |> Seq.map getTarget |> Seq.toArray)

        finally
            if (GetErrors()) <> [] then
                runBuildFailureTargets()
            runFinalTargets()
            WriteTaskTimeSummary watch.Elapsed
        
        match GetErrors() with
        | [] -> ()
        | errors -> failwithf "A target failed: %A" errors 

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
