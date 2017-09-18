[<AutoOpen>]
/// Contains infrastructure code and helper functions for FAKE's target feature.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - module: Fake.Core.Target)")>]
module Fake.TargetHelper

#nowarn "44"
open System
open System.Text
open System.Collections.Generic
open System.Linq

/// [omit]
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - type: Fake.Core.Target.TargetDescription)")>]
type TargetDescription = string

/// [omit]
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - type: Fake.Core.Target.TargetTemplate)")>]
type 'a TargetTemplate =
    { Name: string;
      Dependencies: string list;
      SoftDependencies: string list;
      Description: TargetDescription;
      Function : 'a -> unit}

/// A Target can be run during the build
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - type: Fake.Core.Target.Target)")>]
type Target = unit TargetTemplate

type private DependencyType =
    | Hard = 1
    | Soft = 2

type private DependencyLevel =
    {
        level:int;
        dependants: string list;
    }

/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let mutable PrintStackTraceOnError = false

/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let mutable LastDescription = null

/// Sets the Description for the next target.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let Description text =
    if LastDescription <> null then
        failwithf "You can't set the description for a target twice. There is already a description: %A" LastDescription
    LastDescription <- text

/// TargetDictionary
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let TargetDict = new Dictionary<_,_>(StringComparer.OrdinalIgnoreCase)

/// Final Targets - stores final targets and if they are activated.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let FinalTargets = new Dictionary<_,_>(StringComparer.OrdinalIgnoreCase)

/// BuildFailureTargets - stores build failure targets and if they are activated.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let BuildFailureTargets = new Dictionary<_,_>(StringComparer.OrdinalIgnoreCase)

/// The executed targets.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let ExecutedTargets = new HashSet<_>(StringComparer.OrdinalIgnoreCase)

/// The executed target time.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let ExecutedTargetTimes = new List<_>()

/// Resets the state so that a deployment can be invoked multiple times
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let reset() =
    TargetDict.Clear()
    ExecutedTargets.Clear()
    BuildFailureTargets.Clear()
    ExecutedTargetTimes.Clear()
    FinalTargets.Clear()

[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let mutable CurrentTargetOrder = []
[<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - package: Fake.Core.Target - no longer supported)")>]
let mutable CurrentTarget = ""

/// Returns a list with all target names.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let getAllTargetsNames() = TargetDict |> Seq.map (fun t -> t.Key) |> Seq.toList

/// Gets a target with the given name from the target dictionary.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.Get)")>]
let getTarget name =
    match TargetDict.TryGetValue (name) with
    | true, target -> target
    | _  ->
        traceError <| sprintf "Target \"%s\" is not defined. Existing targets:" name
        for target in TargetDict do
            traceError  <| sprintf "  - %s" target.Value.Name
        failwithf "Target \"%s\" is not defined." name

/// Returns the DependencyString for the given target.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let dependencyString target =
    if target.Dependencies.IsEmpty then String.Empty else
    target.Dependencies
      |> Seq.map (fun d -> (getTarget d).Name)
      |> separated ", "
      |> sprintf "(==> %s)"

/// Returns the soft  DependencyString for the given target.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let softDependencyString target =
    if target.SoftDependencies.IsEmpty then String.Empty else
    target.SoftDependencies
      |> Seq.map (fun d -> (getTarget d).Name)
      |> separated ", "
      |> sprintf "(?=> %s)"

/// Do nothing - fun () -> () - Can be used to define empty targets.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.DoNothing)")>]
let DoNothing = (fun () -> ())

/// Checks whether the dependency (soft or normal) can be added.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
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
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let checkIfDependencyCanBeAdded targetName dependentTargetName =
   checkIfDependencyCanBeAddedCore (fun target -> target.Dependencies) targetName dependentTargetName

/// Checks whether the soft dependency can be added.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let checkIfSoftDependencyCanBeAdded targetName dependentTargetName =
   checkIfDependencyCanBeAddedCore (fun target -> target.SoftDependencies) targetName dependentTargetName

/// Adds the dependency to the front of the list of dependencies.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let dependencyAtFront targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName

    TargetDict.[targetName] <- { target with Dependencies = dependentTargetName :: target.Dependencies }

/// Appends the dependency to the list of dependencies.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let dependencyAtEnd targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName

    TargetDict.[targetName] <- { target with Dependencies = target.Dependencies @ [dependentTargetName] }


/// Appends the dependency to the list of soft dependencies.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let softDependencyAtEnd targetName dependentTargetName =
    let target,dependentTarget = checkIfDependencyCanBeAdded targetName dependentTargetName

    TargetDict.[targetName] <- { target with SoftDependencies = target.SoftDependencies @ [dependentTargetName] }

/// Adds the dependency to the list of dependencies.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let dependency targetName dependentTargetName = dependencyAtEnd targetName dependentTargetName

/// Adds the dependency to the list of soft dependencies.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let softDependency targetName dependentTargetName = softDependencyAtEnd targetName dependentTargetName

/// Adds the dependencies to the list of dependencies.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let Dependencies targetName dependentTargetNames = dependentTargetNames |> List.iter (dependency targetName)

/// Adds the dependencies to the list of soft dependencies.
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let SoftDependencies targetName dependentTargetNames = dependentTargetNames |> List.iter (softDependency targetName)

/// Backwards dependencies operator - x is dependent on ys.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - open Fake.Core.TargetOperators)")>]
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
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case. Note you can create Template with functions that Target.Create using a closure on function parameters + defines dependencies with TargetOperators.")>]
let targetFromTemplate template name parameters =
    match TargetDict.ContainsKey name with
    | true -> 
        failwithf "Duplicate target name %s" name
    | false ->
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

[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case. Note you can create Template with functions that Target.Create using a closure on function parameters + defines dependencies with TargetOperators.")>]
let TargetTemplateWithDependencies dependencies body name parameters =
    let template =
        { Name = String.Empty
          Dependencies = dependencies
          SoftDependencies = []
          Description = LastDescription
          Function = body }
    targetFromTemplate template name parameters

[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case. Note you can create Template with functions that Target.Create using a closure on function parameters + defines dependencies with TargetOperators.")>]
let TargetTemplateWithDependecies dependencies = TargetTemplateWithDependencies dependencies

/// Creates a TargetTemplate.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case. Note you can create Template with functions that Target.Create using a closure on function parameters + defines dependencies with TargetOperators.")>]
let TargetTemplate body = TargetTemplateWithDependencies [] body

/// Creates a Target.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.Create)")>]
let Target name body = TargetTemplate body name ()

/// Represents build errors
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.BuildError)")>]
type BuildError = {
    Target : string
    Message : string }

let mutable private errors = []

/// Get Errors - Returns the errors that occured during execution
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let GetErrors() = errors

/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
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

[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let addExecutedTarget target time =
    lock ExecutedTargets (fun () ->
        ExecutedTargets.Add (target) |> ignore
        ExecutedTargetTimes.Add(target,time) |> ignore
    )

/// Runs all activated final targets (in alphabetically order).
/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
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
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
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
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.ListAvailable)")>]
let PrintTargets() =
    let sb = StringBuilder()
    let appendfn fmt = Printf.ksprintf (sb.AppendLine >> ignore) fmt

    appendfn "The following targets are available:"
    for t in TargetDict.Values do
        appendfn "   %s%s" t.Name (if isNullOrEmpty t.Description then "" else sprintf " - %s" t.Description)

    sb.Length <- sb.Length - Environment.NewLine.Length

    log <| sb.ToString()

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
        let rec visitDependenciesAux level (dependentTarget:option<TargetTemplate<unit>>) (depType,targetName) =
            let target = getTarget targetName
            let isVisited = visited.Contains targetName
            visited.Add targetName |> ignore
            fVisit (dependentTarget, target, depType, level, isVisited)
            
            (fGetDependencies target) |> Seq.iter (visitDependenciesAux (level + 1) (Some target))                
            
            if not isVisited then ordered.Add targetName
        visitDependenciesAux 0 None (DependencyType.Hard, targetName)
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
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.PrintDependencyGraph)")>]
let PrintDependencyGraph verbose target =
    match TargetDict.TryGetValue (target) with
    | false,_ -> PrintTargets()
    | true,target ->
        let sb = StringBuilder()
        let appendfn fmt = Printf.ksprintf (sb.AppendLine >> ignore) fmt

        appendfn "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target.Name

        let logDependency (_, (t: TargetTemplate<unit>), depType, level, isVisited) =
            if verbose ||  not isVisited then
                let indent = (String(' ', level * 3))
                if depType = DependencyType.Soft then
                    appendfn "%s<=? %s" indent t.Name
                else
                    appendfn "%s<== %s" indent t.Name

        let _, ordered = visitDependencies logDependency target.Name
        appendfn ""
        sb.Length <- sb.Length - Environment.NewLine.Length
        log <| sb.ToString()
        
[<System.Obsolete("Not yet migrated, waiting for your contribution ;) (FAKE0004 - package: Fake.Core.Target - member: Fake.Core.Target.PrintRunningOrder to be created)")>]
let PrintRunningOrder() = 
    let sb = StringBuilder()
    let appendfn fmt = Printf.ksprintf (sb.AppendLine >> ignore) fmt
    appendfn "The running order is:"
    CurrentTargetOrder
    |> List.iteri (fun index x ->  
                            if (environVarOrDefault "parallel-jobs" "1" |> int > 1) then                               
                                appendfn "Group - %d" (index + 1)
                            Seq.iter (appendfn "  - %s") x)

    sb.Length <- sb.Length - Environment.NewLine.Length
    log <| sb.ToString()

/// <summary>Writes a dependency graph of all targets in the DOT format.</summary>
[<System.Obsolete("Not yet migrated, waiting for your contribution ;) (FAKE0004 - package: Fake.Core.Target - member: Fake.Core.Target.PrintDotDependencyGraph to be created)")>]
let PrintDotDependencyGraph () =
    let sb = StringBuilder()
    let appendfn fmt = Printf.ksprintf (sb.AppendLine >> ignore) fmt

    appendfn "digraph G {"
    appendfn "  rankdir=TB;"
    appendfn "  node [shape=box];"
    for target in TargetDict.Values do
        appendfn "  \"%s\";" target.Name
        let deps = target.Dependencies
        for d in target.Dependencies do
            appendfn "  \"%s\" -> \"%s\"" target.Name d
        for d in target.SoftDependencies do
            appendfn "  \"%s\" -> \"%s\" [style=dotted];" target.Name d
    sb.Append "}" |> ignore

    log <| sb.ToString()

/// Writes a summary of errors reported during build.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let WriteErrors () =
    traceLine()
    errors
    |> Seq.mapi(fun i e -> sprintf "%3d) %s" (i + 1) e.Message)
    |> Seq.iter(fun s -> traceError s)

/// <summary>Writes a build time report.</summary>
/// <param name="total">The total runtime.</param>
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.WriteTaskTimeSummary)")>]
let WriteTaskTimeSummary total =
    traceHeader "Build Time Report"

    let width = ExecutedTargetTimes
                |> Seq.map (fun (a,b) -> a.Length)
                |> Seq.append([CurrentTarget.Length])
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
        
    if errors = [] && ExecutedTargetTimes.Count > 0 then 
        aligned "Total:" total
        traceLine()
        aligned "Status:" "Ok"
    else if ExecutedTargetTimes.Count > 0 then
        let failedTarget = getTarget CurrentTarget
        alignedError failedTarget.Name "Failure"
        aligned "Total:" total
        traceLine()
        alignedError "Status:" "Failure"
        traceLine()
        WriteErrors()
    else
        let failedTarget = getTarget CurrentTarget
        alignedError failedTarget.Name "Failure"
        traceLine()
        alignedError "Status:" "Failure"

    traceLine()

[<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - no longer supported)")>]
module ExitCode =
    [<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - no longer supported)")>]
    let exitCode = ref 0
    [<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - no longer supported)")>]
    let mutable Value = 42
let private changeExitCodeIfErrorOccured() = if errors <> [] then Environment.ExitCode <- ExitCode.Value; ExitCode.exitCode := ExitCode.Value

/// [omit]
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let isListMode = hasBuildParam "list"

/// List all available targets.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.ListAvailable)")>]
let listTargets() =
    tracefn "Available targets:"
    TargetDict.Values
      |> Seq.iter (fun target ->
            tracefn "  - %s %s" target.Name (if target.Description <> null then " - " + target.Description else "")
            tracefn "     Depends on: %A" target.Dependencies)

// Instead of the target can be used the list dependencies graph parameter.
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let doesTargetMeanListTargets target = target = "--listTargets"  || target = "-lt"

/// <summary>
/// Gets a flag indicating that the user requested to output a DOT-graph
/// of target dependencies instead of building a target.
///</summary>
let private doesTargetMeanPrintDotGraph target = target = "--dotGraph"  || target = "-dg"

/// Determines a parallel build order for the given set of targets
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.determineBuildOrder)")>]
let determineBuildOrder (target : string) (parallelJobs : int) =

    let t = getTarget target

    let targetLevels = new Dictionary<string,DependencyLevel>()

    let appendDepentantOption (currentList:string list) (dependantTarget:option<TargetTemplate<unit>>) = 
        match dependantTarget with
        | None -> currentList
        | Some x -> List.append currentList [x.Name] |> List.distinct 

    let SetDependency dependantTarget target = 
        match targetLevels.TryGetValue target with
        | true, exDependencyLevel -> 
            targetLevels.[target] <- {level = exDependencyLevel.level; dependants = (appendDepentantOption exDependencyLevel.dependants dependantTarget)}
        | _ -> ()

    let rec SetTargetLevel newLevel target  = 
        match targetLevels.TryGetValue target with
        | true, exDependencyLevel -> 
            let minLevel = targetLevels
                           |> Seq.filter(fun x -> x.Value.dependants.Contains target)
                           |> Seq.map(fun x -> x.Value.level)
                           |> fun x -> match x.Any() with
                                       | true -> x |> Seq.min
                                       | _ -> -1
            
            if exDependencyLevel.dependants.Length > 0 then
                if (exDependencyLevel.level < newLevel && (newLevel < minLevel || minLevel = -1)) || (exDependencyLevel.level > newLevel) then
                    targetLevels.[target] <- {level = newLevel; dependants = exDependencyLevel.dependants}
                if exDependencyLevel.level < newLevel then
                    exDependencyLevel.dependants |> List.iter (fun x -> SetTargetLevel (newLevel - 1) x)
        | _ -> ()

    let AddNewTargetLevel dependantTarget level target =
        targetLevels.[target] <- {level = level; dependants=(appendDepentantOption [] dependantTarget)}
        
    let addTargetLevel ((dependantTarget:option<TargetTemplate<unit>>), (target: TargetTemplate<unit>), _, level, _ ) =
        let (|LevelIncreaseWithDependantTarget|_|) = function
        | (true, exDependencyLevel), Some dt when exDependencyLevel.level > level -> Some (exDependencyLevel, dt)
        | _ -> None

        let (|LevelIncreaseWithNoDependantTarget|_|) = function
        | (true, exDependencyLevel), None when exDependencyLevel.level > level -> Some (exDependencyLevel)
        | _ -> None
        
        let (|LevelDecrease|_|) = function
        | (true, exDependencyLevel), _ when exDependencyLevel.level < level -> Some (exDependencyLevel)
        | _ -> None

        let (|AddDependency|_|) = function
        | (true, exDependencyLevel), Some dt when not(exDependencyLevel.dependants.Contains dt.Name) -> Some (exDependencyLevel, dt)
        | _ -> None

        let (|NewTarget|_|) = function
        | (false, _), _ -> Some ()
        | _ -> None

        match targetLevels.TryGetValue target.Name, dependantTarget with
        | LevelIncreaseWithDependantTarget (exDependencyLevel, dt) ->
            SetDependency dependantTarget target.Name
            SetTargetLevel (exDependencyLevel.level - 1) dt.Name
        |  LevelIncreaseWithNoDependantTarget (exDependencyLevel) -> 
            SetDependency dependantTarget target.Name
        |  LevelDecrease (exDependencyLevel) -> 
            SetDependency dependantTarget target.Name
            SetTargetLevel level target.Name
        |  AddDependency (exDependencyLevel, dt) -> 
            SetDependency dependantTarget target.Name
        | NewTarget -> 
            AddNewTargetLevel dependantTarget level target.Name
        | _ -> ()

    if parallelJobs > 1 then
        visitDependencies addTargetLevel target |> ignore

        // the results are grouped by their level, sorted descending (by level) and
        // finally grouped together in a list<TargetTemplate<unit>[]
        targetLevels
        |> Seq.map (fun pair -> pair.Key, pair.Value.level)
        |> Seq.groupBy snd
        |> Seq.sortBy (fun (l,_) -> -l)
        |> Seq.map snd
        |> Seq.map (fun v -> v |> Seq.map fst |> Seq.distinct |> Seq.map getTarget |> Seq.toArray)
        |> Seq.toList
    else
        let _, order = visitDependencies ignore target
        order 
        |> Seq.map (fun t -> [(getTarget t)] |> Seq.toArray) 
        |> Seq.toList

/// Runs a single target without its dependencies
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let runSingleTarget (target : TargetTemplate<unit>) =
    try
        if errors = [] then
            traceStartTarget target.Name target.Description (dependencyString target)
            CurrentTarget <- target.Name
            let watch = new System.Diagnostics.Stopwatch()
            watch.Start()
            target.Function()
            addExecutedTarget target.Name watch.Elapsed
            traceEndTarget target.Name
            CurrentTarget <- ""
    with exn ->
        targetError target.Name exn

/// Runs the given array of targets in parallel using count tasks
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let runTargetsParallel (count : int) (targets : Target[]) =
    targets.AsParallel()
        .WithDegreeOfParallelism(count)
        .Select(runSingleTarget)
        .ToArray()
    |> ignore
    
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let runTargets (targets: TargetTemplate<unit> array) =
    let lastTarget = targets |> Array.last
    if errors = [] && ExecutedTargets.Contains (lastTarget.Name) |> not then
        let firstTarget = targets |> Array.head
        if hasBuildParam "single-target" then
            traceImportant "Single target mode ==> Skipping dependencies."
            runSingleTarget lastTarget
        else
            targets |> Array.iter runSingleTarget

/// Runs a target and its dependencies.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.Run)")>]
let run targetName =
    if doesTargetMeanPrintDotGraph targetName then PrintDotDependencyGraph() else
    if doesTargetMeanListTargets targetName then listTargets() else
    if LastDescription <> null then failwithf "You set a task description (%A) but didn't specify a task." LastDescription
    
    let watch = new System.Diagnostics.Stopwatch()
    watch.Start()
    try
        tracefn "Building project with version: %s" buildVersion
        PrintDependencyGraph false targetName

        let parallelJobs = environVarOrDefault "parallel-jobs" "1" |> int

        // determine a build order
        let order = determineBuildOrder targetName parallelJobs
        CurrentTargetOrder <- order |> List.map (fun targets -> targets |> Array.map (fun t -> t.Name) |> Array.toList)
        PrintRunningOrder()

        // Figure out the order in in which targets can be run, and which can be run in parallel.
        if parallelJobs > 1 then
            tracefn "Running parallel build with %d workers" parallelJobs

            // run every level in parallel
            for level in order do
                runTargetsParallel parallelJobs level
        else
            tracefn "Running build with 1 worker"
            runTargets (order |> Seq.concat |> Seq.toArray)

    finally
        if errors <> [] then
            runBuildFailureTargets()
        runFinalTargets()
        killAllCreatedProcesses()
        WriteTaskTimeSummary watch.Elapsed
        changeExitCodeIfErrorOccured()

/// Registers a BuildFailureTarget (not activated).
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.CreateBuildFailure)")>]
let BuildFailureTarget name body =
    Target name body
    BuildFailureTargets.Add(name,false)

/// Activates the BuildFailureTarget.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.ActivateBuildFailure)")>]
let ActivateBuildFailureTarget name =
    let t = getTarget name // test if target is defined
    BuildFailureTargets.[name] <- true

/// Registers a final target (not activated).
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.CreateFinal)")>]
let FinalTarget name body =
    Target name body
    FinalTargets.Add(name,false)

/// Activates the FinalTarget.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.ActivateFinal)")>]
let ActivateFinalTarget name =
    let t = getTarget name // test if target is defined
    FinalTargets.[name] <- true
