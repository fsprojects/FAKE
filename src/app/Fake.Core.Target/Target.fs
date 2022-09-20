namespace Fake.Core

open System
open System.Collections.Generic
open System.Reflection
open Fake.Core
open System.Threading.Tasks
open System.Threading
open FSharp.Control.Reactive

module internal TargetCli =
    let targetCli =
        """
Usage:
  fake-run --list
  fake-run --write-info <file>
  fake-run --version
  fake-run --help | -h
  fake-run [target_opts] [target <target>] [--] [<targetargs>...]

Target Module options [target_opts]:
    -t, --target <target>    Run the given target (ignored if a target is already provided with '[target <target>]')
    -e, --environment-variable <keyval> [*]
                             Set an environment variable. Use 'key=val'.
                             Consider using regular arguments, see https://fake.build/guide/core-targets.html
    -s, --single-target      Run only the specified target.
    -p, --parallel <num>     Run parallel with the given number of tasks.
        """
    let doc = Docopt(targetCli)
    let parseArgs args = doc.Parse args
    
/// [omit]
type TargetDescription = string

[<NoComparison>]
[<NoEquality>]
type TargetResult =
    { Error : exn option; Time : TimeSpan; Target : Target; WasSkipped : bool }

and [<NoComparison>] [<NoEquality>] TargetContext =
    { PreviousTargets : TargetResult list
      AllExecutingTargets : Target list
      FinalTarget : string
      Arguments : string list
      IsRunningFinalTargets : bool
      CancellationToken : CancellationToken }
    static member Create ft all args token = {
        FinalTarget = ft
        AllExecutingTargets = all
        PreviousTargets = []
        Arguments = args
        IsRunningFinalTargets = false
        CancellationToken = token }
    member x.HasError =
        x.PreviousTargets
        |> List.exists (fun t -> t.Error.IsSome)
    member x.TryFindPrevious name =
        x.PreviousTargets |> List.tryFind (fun t -> t.Target.Name = name)
    member x.TryFindTarget name =
        x.AllExecutingTargets |> List.tryFind (fun t -> t.Name = name)
    member x.ErrorTargets = 
        x.PreviousTargets |> List.choose (fun tres -> match tres.Error with
                                                      | Some er -> Some (er, tres.Target)
                                                      | None -> None)    

and [<NoComparison>] [<NoEquality>] TargetParameter =
    { TargetInfo : Target
      Context : TargetContext }

/// [omit]
and [<NoComparison>] [<NoEquality>] Target =
    { Name: string
      Dependencies: string list
      SoftDependencies: string list
      Description: TargetDescription option
      Function : TargetParameter -> unit}
    member x.DescriptionAsString =
        match x.Description with
        | Some d -> d
        | _ -> null

type internal DeclarationInfo =
    { File: string; Line: int; Column: int; ErrorDetail: string }
type internal Dependency =
    { Name: string; Declaration: DeclarationInfo }
type [<NoComparison>] [<NoEquality>] internal InternalTarget =
    { Name: string
      Dependencies: Dependency list
      SoftDependencies: Dependency list
      Description: TargetDescription option
      DefinitionOrder : int
      Declaration : DeclarationInfo
      Function : TargetParameter -> unit }
    member x.DescriptionAsString =
        match x.Description with
        | Some d -> d
        | _ -> null
    member x.AsTarget =
        { Name = x.Name
          Dependencies = x.Dependencies |> List.map (fun d -> d.Name)
          SoftDependencies = x.SoftDependencies |> List.map (fun d -> d.Name)
          Description = x.Description
          Function = x.Function }

/// <summary>
/// Exception for request errors
/// </summary>
#if !NETSTANDARD1_6
[<Serializable>]
#endif
type BuildFailedException =
    val private info : TargetContext option
    inherit Exception
    new (msg:string, inner:exn) = {
      inherit Exception(msg, inner)
      info = None }
    new (info:TargetContext, msg:string, inner:exn) = {
      inherit Exception(msg, inner)
      info = Some info }
#if !NETSTANDARD1_6
    new (info:System.Runtime.Serialization.SerializationInfo, context:System.Runtime.Serialization.StreamingContext) = {
      inherit Exception(info, context)
      info = None
    }
#endif
    member x.Info with get () = x.info
    member x.Wrap() =
        match x.info with
        | Some info ->
            BuildFailedException(info, x.Message, x:>exn)
        | None ->
            BuildFailedException(x.Message, x:>exn)

/// <namespacedoc>
/// <summary>
/// Core namespace contains FAKE's core tasks, like Target, Process, BuildeServer, etc.. modules.
/// </summary>
/// </namespacedoc>
/// 
/// <summary>
/// FAKE Target module contains tasks to define and run targets.
/// </summary> 
[<RequireQualifiedAccess>]
module Target =

    type private DependencyType =
        | Hard = 1
        | Soft = 2

    let private printStackTraceOnErrorVar = "Fake.Core.Target.PrintStackTraceOnError"
    let private getPrintStackTraceOnError, _, (setPrintStackTraceOnError:bool -> unit) =
        FakeVar.define printStackTraceOnErrorVar

    let private lastDescriptionVar = "Fake.Core.Target.LastDescription"
    let private getLastDescription, removeLastDescription, setLastDescription =
        FakeVar.define lastDescriptionVar
    
    let private collectStackVar = "Fake.Core.Target.CollectStack"
    let private getCollectStack, removeCollectStack, (setCollectStack : bool -> unit) =
        FakeVar.define collectStackVar

    let private shouldCollectStack() =
        match getCollectStack () with
        | Some b -> b
        | None -> false

    let internal isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
    let internal getNormalizedFileName (fileName: string) =
        let fn = System.IO.Path.GetFileName fileName
        if isWindows then if isNull fn then null else fn.ToLowerInvariant()
        else fn

    let internal getDeclaration () =
        if shouldCollectStack () then
            let ctx = Context.forceFakeContext ()
            let st1 = System.Diagnostics.StackTrace(1, true)
            let frames =
                [ 0 .. st1.FrameCount - 1 ]
                |> Seq.map (fun idx -> st1.GetFrame idx)
                |> Seq.map (fun sf -> sf.GetFileName(), sf)
                |> Seq.cache
            let normalizedScriptFile = getNormalizedFileName(ctx.ScriptFile)
            let fn =
                let compiledAssembly =
                    frames
                    |> Seq.tryFind (fun (fn, _) ->
                        // Find a frame where we are quite positive it belongs to us
                        let scriptName = getNormalizedFileName fn
                        not (String.IsNullOrEmpty fn) && (scriptName = normalizedScriptFile))
                    |> Option.map (fun (_, frame) -> frame.GetMethod().DeclaringType.Assembly)

                compiledAssembly
                // First try to find the first frame with the correct assembly
                |> Option.bind (fun ass ->
                    let fullName = ass.FullName
                    frames
                    |> Seq.tryFind (fun (fn, sf) ->
                        not (String.IsNullOrEmpty fn) && sf.GetMethod().DeclaringType.Assembly.FullName = fullName))
                // if not found fallback to any script
                |> Option.orElseWith (fun _ ->
                    frames
                    |> Seq.tryFind (fun (fn, _) ->
                        let scriptName = getNormalizedFileName fn
                        not (String.IsNullOrEmpty fn) && (fn.EndsWith ".fsx" || scriptName = normalizedScriptFile)))
                // if not found fallback to any information we might have
                |> Option.orElseWith (fun _ ->
                    frames
                    |> Seq.tryFind (fun (fn, _) ->
                        // fallback to any information we might have...
                        not (String.IsNullOrEmpty fn)))
            fn
            |> Option.map (fun (fn, sf) ->
                { File = fn; Line = sf.GetFileLineNumber(); Column = sf.GetFileColumnNumber(); ErrorDetail = null })
            |> Option.defaultWith (fun _ ->
                let framesString =
                    if frames |> Seq.isEmpty then
                        " no frames available"
                    else
                        frames
                        |> Seq.map (fun (f, frame) ->
                            let mt = frame.GetMethod()
                            let tbase = mt.DeclaringType.FullName
                            let line = frame.GetFileLineNumber()
                            let column = frame.GetFileColumnNumber()
                            sprintf "%s.%s in %s:%d:%d" tbase mt.Name f line column)
                        |> fun s -> "\n - " + String.Join("\n - ", s)                                                
                { File = null; Line = 0; Column = 0; ErrorDetail = sprintf "stackframe not found, available:%s" framesString })
        else { File = null; Line = 0; Column = 0; ErrorDetail = "not requested" }

    /// <summary>
    /// Sets the Description for the next target.
    /// </summary>
    ///
    /// <param name="text">The description of the next target</param>
    let description text =
        match getLastDescription() with
        | Some (v:string) ->
            failwithf "You can't set the description for a target twice. There is already a description: %A" v
        | None ->
           setLastDescription text

    let internal getVarWithInit name f =
        let varName = sprintf "Fake.Core.Target.%s" name
        let getVar, _, setVar =
            FakeVar.define varName
        fun () ->
            match getVar() with
            | Some d -> d
            | None ->
                let d = f () // new Dictionary<_,_>(StringComparer.OrdinalIgnoreCase)
                setVar d
                d
    
    let internal getTargetDict =
        getVarWithInit "TargetDict" (fun () -> Dictionary<string,InternalTarget>(StringComparer.OrdinalIgnoreCase))

    /// <summary>
    /// Final Targets - stores final targets and if they are activated.
    /// </summary>
    let internal getFinalTargets =
        getVarWithInit "FinalTargets" (fun () -> Dictionary<_,_>(StringComparer.OrdinalIgnoreCase))

    /// <summary>
    /// BuildFailureTargets - stores build failure targets and if they are activated.
    /// </summary>
    let internal getBuildFailureTargets =
        getVarWithInit "BuildFailureTargets" (fun () -> Dictionary<_,_>(StringComparer.OrdinalIgnoreCase))


    /// <summary>
    /// Resets the state so that a deployment can be invoked multiple times
    /// </summary>
    let internal reset() =
        getTargetDict().Clear()
        getBuildFailureTargets().Clear()
        getFinalTargets().Clear()

    /// <summary>
    /// Returns a list with all target names.
    /// </summary>
    let internal getAllTargetsNames() = getTargetDict() |> Seq.map (fun t -> t.Key) |> Seq.toList

    /// <summary>
    /// Gets a target with the given name from the target dictionary.
    /// </summary>
    let internal getInternal name : InternalTarget =
        let d = getTargetDict()
        match d.TryGetValue name with
        | true, target -> target
        | _  ->
            Trace.traceError <| sprintf "Target \"%s\" is not defined. Existing targets:" name
            for target in d do
                Trace.traceError  <| sprintf "  - %s" target.Value.Name
            failwithf "Target \"%s\" is not defined." name
    let get name : Target =
        (getInternal name).AsTarget

    /// <summary>
    /// Returns the DependencyString for the given target.
    /// </summary>
    let internal dependencyString (target :Target) =
        if target.Dependencies.IsEmpty then String.Empty else
        target.Dependencies
          |> Seq.map (fun d -> (getInternal d).Name)
          |> String.separated ", "
          |> sprintf "(==> %s)"

    let internal runSimpleInternal context (target:Target) =
        let watch = System.Diagnostics.Stopwatch.StartNew()
        let error =
            try
                if not context.IsRunningFinalTargets then
                    context.CancellationToken.ThrowIfCancellationRequested()
                target.Function { TargetInfo = target; Context = context }
                None
            with e -> Some e
        watch.Stop()
        { Error = error; Time = watch.Elapsed; Target = target; WasSkipped = false }
    
    let internal runSimpleContextInternal (traceStart: string -> string -> string -> Trace.ISafeDisposable) context (target:Target) =
        use t = traceStart target.Name target.DescriptionAsString (dependencyString target)
        let result = runSimpleInternal context target
        if result.Error.IsSome then 
            t.MarkFailed()
        else 
            t.MarkSuccess()
        { context with PreviousTargets = context.PreviousTargets @ [result] }

    /// <summary>
    /// This simply runs the function of a target without doing anything (like tracing, stop watching or adding
    /// it to the results at the end)
    /// </summary>
    ///
    /// <param name="name">The final target name</param>
    /// <param name="args">The target arguments</param>
    let runSimple name args =
        let target = get name
        target
        |> runSimpleInternal (TargetContext.Create name [target] args CancellationToken.None)

    /// <summary>
    /// This simply runs the function of a target without doing anything (like tracing, stop watching or
    /// adding it to the results at the end)
    /// </summary>
    ///
    /// <param name="name">The target name</param>
    /// <param name="ctx">The context object</param>
    let runSimpleWithContext name ctx =
        let target = get name
        target
        |> runSimpleInternal ctx

    /// <summary>
    /// Returns the soft  DependencyString for the given target.
    /// </summary>
    let internal softDependencyString target =
        if target.SoftDependencies.IsEmpty then String.Empty else
        target.SoftDependencies
          |> Seq.map (fun d -> (get d.Name).Name)
          |> String.separated ", "
          |> sprintf "(?=> %s)"

    /// <summary>
    /// Checks whether the dependency (soft or normal) can be added.
    /// </summary>
    let internal checkIfDependencyCanBeAddedCore fGetDependencies targetName dependentTargetName =
        let target = getInternal targetName
        let dependentTarget = getInternal dependentTargetName
        let visited = HashSet<string>(StringComparer.OrdinalIgnoreCase)

        let rec checkDependencies (dependentTarget:InternalTarget) =
            if visited.Add dependentTarget.Name then
                fGetDependencies dependentTarget
                |> List.iter (fun (dep : Dependency) ->
                    if String.Equals(dep.Name, targetName, StringComparison.OrdinalIgnoreCase) then
                        failwithf "Cyclic dependency between %s and %s" targetName dependentTarget.Name
                    checkDependencies (getInternal dep.Name))

        checkDependencies dependentTarget
        target,dependentTarget

    /// <summary>
    /// Checks whether the dependency can be added.
    /// </summary>
    let internal checkIfDependencyCanBeAdded targetName dependentTargetName =
       checkIfDependencyCanBeAddedCore (fun target -> target.Dependencies) targetName dependentTargetName

    /// <summary>
    /// Checks whether the soft dependency can be added.
    /// </summary>
    let internal checkIfSoftDependencyCanBeAdded targetName dependentTargetName =
       checkIfDependencyCanBeAddedCore (fun target -> target.SoftDependencies) targetName dependentTargetName

    /// <summary>
    /// Adds the dependency to the front of the list of dependencies.
    /// </summary>
    let internal dependencyAtFront targetName dependentTargetName =
        let target,_ = checkIfDependencyCanBeAdded targetName dependentTargetName

        let hasDependency =
           target.Dependencies
           |> Seq.exists (fun d -> String.Equals(d.Name, dependentTargetName, StringComparison.OrdinalIgnoreCase))
        if not hasDependency then
            let decl = getDeclaration()
            getTargetDict().[targetName] <- 
                { target with 
                    Dependencies = { Name = dependentTargetName; Declaration = decl } :: target.Dependencies
                    SoftDependencies =
                        target.SoftDependencies
                        |> List.filter (fun d -> not (String.Equals(d.Name, dependentTargetName, StringComparison.OrdinalIgnoreCase)))
                }

    /// <summary>
    /// Appends the dependency to the list of soft dependencies.
    /// </summary>
    let internal softDependencyAtFront targetName dependentTargetName =
        let target,_ = checkIfDependencyCanBeAdded targetName dependentTargetName

        let hasDependency =
           target.Dependencies
           |> Seq.exists (fun d -> String.Equals(d.Name, dependentTargetName, StringComparison.OrdinalIgnoreCase))
        let hasSoftDependency =
           target.SoftDependencies
           |> Seq.exists (fun d -> String.Equals(d.Name, dependentTargetName, StringComparison.OrdinalIgnoreCase))
        match hasDependency, hasSoftDependency with
        | true, _ -> ()
        | false, true -> ()
        | false, false ->
            let decl = getDeclaration()
            getTargetDict().[targetName] <- { target with SoftDependencies = { Name = dependentTargetName; Declaration = decl } :: target.SoftDependencies }

    /// <summary>
    /// Adds the dependency to the list of dependencies.
    /// </summary>
    let internal dependency targetName dependentTargetName = dependencyAtFront targetName dependentTargetName

    /// <summary>
    /// Adds the dependency to the list of soft dependencies.
    /// </summary>
    let internal softDependency targetName dependentTargetName = softDependencyAtFront targetName dependentTargetName

    /// <summary>
    /// Adds the dependencies to the list of dependencies.
    /// </summary>
    let internal Dependencies targetName dependentTargetNames = dependentTargetNames |> List.iter (dependency targetName)

    /// <summary>
    /// Adds the dependencies to the list of soft dependencies.
    /// </summary>
    let internal SoftDependencies targetName dependentTargetNames = dependentTargetNames |> List.iter (softDependency targetName)

    /// <summary>
    /// Backwards dependencies operator - x is dependent on ys.
    /// </summary>
    let inline internal (<==) x ys = Dependencies x ys

    /// <summary>
    /// Creates a target from template.
    /// </summary>
    let internal addTarget target name =
        getTargetDict().Add(name, target)
        name <== (target.Dependencies |> List.map (fun d -> d.Name))
        removeLastDescription()

    /// <summary>
    /// Add a target with dependencies
    /// </summary>
    let internal addTargetWithDependencies dependencies body name =
        let template =
            { Name = name
              Dependencies = dependencies
              SoftDependencies = []
              Description = getLastDescription()
              DefinitionOrder = getTargetDict().Count
              Declaration = getDeclaration()
              Function = body }
        addTarget template name

    /// <summary>
    /// Creates a Target.
    /// </summary>
    let create name body = addTargetWithDependencies [] body name

    /// <summary>
    /// Runs all activated final targets (in alphabetically order).
    /// </summary>
    let internal runFinalTargets context =
        getFinalTargets()
        |> Seq.filter (fun kv -> kv.Value)     // only if activated
        |> Seq.map (fun kv -> get kv.Key)
        |> Seq.fold (fun context target -> runSimpleContextInternal Trace.traceFinalTarget context target) context                

    /// <summary>
    /// Runs all build failure targets.
    /// </summary>
    let internal runBuildFailureTargets context =
        getBuildFailureTargets()
        |> Seq.filter (fun kv -> kv.Value)     // only if activated
        |> Seq.map (fun kv -> get kv.Key)
        |> Seq.fold (fun context target -> runSimpleContextInternal Trace.traceFailureTarget context target) context     

    /// <summary>
    /// List all targets available.
    /// </summary>
    let listAvailable() =
        Trace.log "The following targets are available:"
        for t in getTargetDict().Values |> Seq.sortBy (fun t -> t.Name) do
            Trace.logfn "   %s%s" t.Name (match t.Description with Some s -> sprintf " - %s" s | _ -> "")

    /// <summary>
    /// List all targets available.
    /// </summary>
    let internal writeInfoFile file =
        let escapeJson (s:string) =
            let sb = System.Text.StringBuilder(s.Length)
            for c in s do
                // https://stackoverflow.com/a/27516892
                match c with
                | '"' -> sb.Append "\\\"" |> ignore
                | '\\' -> sb.Append "\\\\" |> ignore
                | '/' -> sb.Append "\\/" |> ignore
                | '\b' -> sb.Append "\\b" |> ignore
                | '\f' -> sb.Append "\\f" |> ignore
                | '\n' -> sb.Append "\\n" |> ignore
                | '\r' -> sb.Append "\\r" |> ignore
                | '\t' -> sb.Append "\\t" |> ignore
                | _ -> sb.Append c |> ignore
            sb.ToString()
        let createJsonString s =
            match s with
            | null -> "null"
            | s -> "\"" + escapeJson s + "\""
        let createDeclJson (decl:DeclarationInfo) =
            sprintf "{ \"file\": %s, \"line\": %d, \"column\": %d, \"errorDetail\": %s }"
                (createJsonString decl.File)
                decl.Line decl.Column
                (createJsonString decl.ErrorDetail)
        let createDepJson (dep:Dependency) =
            sprintf "{ \"name\": %s, \"declaration\": %s }"
                (createJsonString dep.Name)
                (createDeclJson dep.Declaration)
        let joinJsonObjects createObj (objs:_ seq) =
            objs
            |> Seq.map createObj
            |> fun s -> "[ " + String.Join(", ", s) + " ]"
        let createTargetJson (t:InternalTarget) =
            sprintf
                "{ \"name\": %s, \"hardDependencies\": %s, \"softDependencies\": %s, \"declaration\": %s, \"order\": %d, \"description\": %s }"
                (createJsonString t.Name)
                (joinJsonObjects createDepJson t.Dependencies)
                (joinJsonObjects createDepJson t.SoftDependencies)
                (createDeclJson t.Declaration)
                t.DefinitionOrder
                (createJsonString t.DescriptionAsString)

        getTargetDict().Values
        |> Seq.sortBy (fun t -> t.DefinitionOrder)
        |> joinJsonObjects createTargetJson
        |> fun targets ->
            System.IO.File.WriteAllText(file, sprintf "{ \"targets\": %s }" targets)

    // <summary>
    /// Maps the specified dependency type into the list of targets
    /// </summary>
    let private withDependencyType (depType:DependencyType) targets =
        targets |> List.map (fun t -> depType, t)

    /// <summary>
    /// Helper function for visiting targets in a dependency tree. Returns a set containing the names of the all the
    /// visited targets, and a list containing the targets visited ordered such that dependencies of a target appear
    /// earlier in the list than the target.
    /// </summary>
    let private visitDependencies repeatVisit fVisit targetName =
        let visit fGetDependencies fVisit targetName =
            let visited = HashSet<_>(StringComparer.OrdinalIgnoreCase)
            let rec visitDependenciesAux orderedTargets = function
                // NOTE: should be tail recursive
                | (level, depType, targetName) :: workLeft ->
                    let target = get targetName
                    match visited.Add targetName with
                    | added when added || repeatVisit ->
                        fVisit (target, depType, level)
                        let newLeft = (fGetDependencies target |> Seq.map (fun (depType, targetName) -> (level + 1, depType, targetName)) |> Seq.toList) @ workLeft
                        let newOrdered = if added then (targetName :: orderedTargets) else orderedTargets
                        visitDependenciesAux newOrdered newLeft
                    | _ ->
                        visitDependenciesAux orderedTargets workLeft                        
                | _ -> orderedTargets
            let orderedTargets = visitDependenciesAux [] [(0, DependencyType.Hard, targetName)]
            visited, orderedTargets

        // First pass is to accumulate targets in (hard) dependency graph
        let visited, _ = visit (fun t -> t.Dependencies |> List.rev |> withDependencyType DependencyType.Hard) ignore targetName

        let getAllDependencies (t: Target) =
             (t.Dependencies |> List.rev |> withDependencyType DependencyType.Hard) @
             // Note that we only include the soft dependency if it is present in the set of targets that were
             // visited.
             (t.SoftDependencies |> List.filter visited.Contains |> withDependencyType DependencyType.Soft)

        // Now make second pass, adding in soft dependencies if appropriate
        visit getAllDependencies fVisit targetName

    /// <summary>
    /// Writes a dependency graph.
    /// </summary>
    ///
    /// <param name="verbose">Whether to print verbose output or not.</param>
    /// <param name="target">The target for which the dependencies should be printed.</param>
    let printDependencyGraph verbose target =
        match getTargetDict().TryGetValue target with
        | false,_ -> listAvailable()
        | true,target ->
            let sb = System.Text.StringBuilder()
            let appendfn fmt = Printf.ksprintf (sb.AppendLine >> ignore) fmt

            appendfn "%sDependencyGraph for Target %s:" (if verbose then String.Empty else "Shortened ") target.Name
            let logDependency (t: Target, depType, level) =
                let indent = String(' ', level * 3)
                if depType = DependencyType.Soft then
                    appendfn "%s<=? %s" indent t.Name
                else
                    appendfn "%s<== %s" indent t.Name

            let _ = visitDependencies verbose logDependency target.Name
            //appendfn ""
            //sb.Length <- sb.Length - Environment.NewLine.Length
            Trace.log <| sb.ToString()

    let internal printRunningOrder (targetOrder:Target[] list) =
        let sb = System.Text.StringBuilder()
        let appendfn fmt = Printf.ksprintf (sb.AppendLine >> ignore) fmt
        appendfn "The running order is:"
        targetOrder
        |> List.iteri (fun index x ->
                                //if (environVarOrDefault "parallel-jobs" "1" |> int > 1) then
                                appendfn "Group - %d" (index + 1)
                                Seq.iter (appendfn "  - %s") (x|>Seq.map (fun t -> t.Name)))

        sb.Length <- sb.Length - Environment.NewLine.Length
        Trace.log <| sb.ToString()

    /// <summary>
    /// Writes a build time report.
    /// </summary>
    /// 
    /// <param name="total">The total runtime.</param>
    let internal writeTaskTimeSummary total context =
        Trace.traceHeader "Build Time Report"
        let executedTargets = context.PreviousTargets
        if executedTargets.Length > 0 then
            let width =
                executedTargets
                  |> Seq.map (fun tres -> tres.Target.Name.Length)
                  |> Seq.max
                  |> max 8

            let alignedString (name:string) duration extra =
                let durString = sprintf "%O" duration
                if (String.IsNullOrEmpty extra) then
                    sprintf "%s   %s" (name.PadRight width) durString
                else sprintf "%s   %s   (%s)" (name.PadRight width) (durString.PadRight "00:00:00.0000824".Length) extra
            let aligned (name:string) duration extra = alignedString name duration extra |> Trace.trace
            let alignedWarn (name:string) duration extra = alignedString name duration extra |> Trace.traceFAKE "%s"
            let alignedError (name:string) duration extra = alignedString name duration extra |> Trace.traceError

            aligned "Target" "Duration" null
            aligned "------" "--------" null
            executedTargets
              |> Seq.iter (fun tres ->
                    let name = tres.Target.Name
                    let time = tres.Time
                    match tres.Error with
                    | None when tres.WasSkipped -> alignedWarn name time "skipped" // Yellow
                    | None -> aligned name time null
                    | Some e -> alignedError name time e.Message)

            aligned "Total:" total null
            if not context.HasError then 
                aligned "Status:" "Ok" null
            else
                alignedError "Status:" "Failure" null
        else
            Trace.traceError "No target was successfully completed"

        Trace.traceLine()

    /// <summary>
    /// Determines a parallel build order for the given set of targets
    /// </summary>
    let internal determineBuildOrder (target : string) =
        let visited = HashSet<string>(StringComparer.OrdinalIgnoreCase)
        let visitedTargets = List<Target>()
        let rec visitDependenciesAux = function
            // NOTE: should be tail recursive
            | targetName :: workLeft ->
                if visited.Add(targetName)
                then
                    let target = get targetName
                    visitedTargets.Add(target)
                    let newLeft = target.Dependencies @ workLeft
                    visitDependenciesAux newLeft
                else
                    visitDependenciesAux workLeft                        
            | _ -> visitedTargets |> Seq.toList


        // first find the list of targets we "have" to build
        let targets = visitDependenciesAux [target]

        // Try to build the optimal tree by starting with the targets without dependencies and remove them from the list iteratively
        let targetLeftSet = HashSet<_>(StringComparer.OrdinalIgnoreCase)
        targets |> Seq.map (fun t -> t.Name) |> Seq.iter (targetLeftSet.Add >> ignore)
        let rec findOrder progress (targetLeft:Target list) =
            // NOTE: Should be tail recursive
            let isValidTarget name =
                targetLeftSet.Contains(name)
            
            let canBeExecuted (t:Target) =
                t.Dependencies @ t.SoftDependencies
                |> Seq.filter isValidTarget
                |> Seq.isEmpty
            let map =
                targetLeft
                    |> Seq.groupBy canBeExecuted
                    |> Seq.map (fun (t, g) -> t, Seq.toList g)
                    |> dict
            let execute, left =
                (match map.TryGetValue true with
                | true, ts -> ts
                | _ -> []),
                match map.TryGetValue false with
                | true, ts -> ts
                | _ -> []
            if List.isEmpty execute then failwithf "Could not progress build order in %A" targetLeft
            if List.isEmpty left then
                List.rev (List.toArray execute :: progress)
            else
                execute |> Seq.map (fun t -> t.Name) |> Seq.iter (targetLeftSet.Remove >> ignore)
                findOrder (List.toArray execute :: progress) left      
        findOrder [] targets

    /// <summary>
    /// Runs a single target without its dependencies... only when no error has been detected yet.
    /// </summary>
    let internal runSingleTarget (target : Target) (context:TargetContext) =
        if not context.HasError then
            runSimpleContextInternal Trace.traceTarget context target
        else
            { context with PreviousTargets = context.PreviousTargets @ [{ Error = None; Time = TimeSpan.Zero; Target = target; WasSkipped = true }] }

    module internal ParallelRunner =
        let internal mergeContext (ctx1:TargetContext) (ctx2:TargetContext) =
            let known =
                ctx1.PreviousTargets
                |> Seq.map (fun tres -> String.toLower tres.Target.Name, tres)
                |> dict
            let filterKnown targets =
                targets
                |> List.filter (fun tres -> not (known.ContainsKey (String.toLower tres.Target.Name)))
            { ctx1 with
                PreviousTargets =
                    ctx1.PreviousTargets @ filterKnown ctx2.PreviousTargets
            }

        // Centralized handling of target context and next target logic...
        [<NoComparison>]
        [<NoEquality>]
        type RunnerHelper =
            | GetNextTarget of TargetContext * AsyncReplyChannel<Async<TargetContext * Target option>>
        type IRunnerHelper =
            abstract GetNextTarget : TargetContext -> Async<TargetContext * Target option>
        let createCtxMgr (order:Target[] list) (ctx:TargetContext) =
            let body (inbox:MailboxProcessor<RunnerHelper>) = async {
                let targetCount =
                    order |> Seq.sumBy (fun t -> t.Length)
                let resolution = Set.ofSeq(order |> Seq.concat |> Seq.map (fun t -> String.toLower t.Name))
                let inResolution (t:string) = resolution.Contains (String.toLower t)
                let mutable ctx = ctx
                let mutable waitList = []
                let mutable runningTasks : Target list = []
                //let mutable remainingOrders = order
                try
                    while true do
                        let! msg = inbox.Receive()
                        match msg with
                        | GetNextTarget (newCtx, reply) ->
                            let failwithf pf =
                                // handle reply before throwing.
                                let tcs = TaskCompletionSource<TargetContext * Target option>()
                                waitList <- waitList @ [ tcs ]
                                reply.Reply (tcs.Task |> Async.AwaitTask)
                                failwithf pf
                            // semantic is:
                            // - We never return a target twice!
                            // - we fill up the waitlist first
                            ctx <- mergeContext ctx newCtx
                            let known =
                                ctx.PreviousTargets
                                |> Seq.map (fun tres -> String.toLower tres.Target.Name, tres)
                                |> dict
                            runningTasks <-
                                runningTasks
                                |> List.filter (fun t -> not(known.ContainsKey (String.toLower t.Name)))
                            if known.Count = targetCount then
                                for w:TaskCompletionSource<TargetContext * Target option> in waitList do
                                    w.SetResult (ctx, None)
                                waitList <- []
                                reply.Reply (async.Return(ctx, None))
                            else
                                let calculateOpenTargets() =
                                    let isOpen (t:Target) =
                                        not (known.ContainsKey (String.toLower t.Name)) && // not already finised
                                        not (runningTasks |> Seq.exists (fun r -> String.toLower r.Name = String.toLower t.Name)) // not already running
                                    order
                                    |> Seq.concat
                                    |> Seq.filter isOpen
                                let runnable =
                                    let isRunnable (t:Target) =
                                        t.Dependencies @ List.filter inResolution t.SoftDependencies // all dependencies finished
                                        |> Seq.forall (String.toLower >> known.ContainsKey)
                                    calculateOpenTargets()
                                    |> Seq.filter isRunnable
                                    |> Seq.toList

                                let rec getNextFreeRunableTarget (r : Target list) =
                                    match r with
                                    | t :: rest ->
                                        match waitList with
                                        | h :: restwait ->
                                            // fill some idle worker
                                            runningTasks <- t :: runningTasks
                                            h.SetResult (ctx, Some t)
                                            waitList <- restwait
                                            getNextFreeRunableTarget rest
                                        | [] -> Some t
                                    | [] -> None
                                match getNextFreeRunableTarget runnable with
                                | Some free ->
                                    runningTasks <- free :: runningTasks
                                    reply.Reply (async.Return(ctx, Some free))
                                | None ->
                                    if runningTasks.Length = 0 && resolution.Count > known.Count then
                                        // No running tasks but still open resolution
                                        let resolutionStr = sprintf "[%s]" (String.Join(",", resolution))
                                        let knownStr = sprintf "[%s]" (String.Join(",", known.Keys))
                                        failwithf "Error detected in fake scheduler: resolution '%s', known '%s'" resolutionStr knownStr
                                    // queue work
                                    let tcs = TaskCompletionSource<TargetContext * Target option>()
                                    let running = String.Join(", ", runningTasks |> Seq.map (fun t -> sprintf "'%s'" t.Name))
                                    // recalculate openTargets as getNextFreeRunableTarget could change runningTasks
                                    let openTargets = calculateOpenTargets() |> Seq.toList
                                    let orderedOpen =
                                        let isDependencyResolved =
                                            String.toLower >> known.ContainsKey
                                        let isDependencyRunning t =
                                            runningTasks
                                            |> Seq.exists (fun running -> String.toLower running.Name = t)
                                        let isDependencyResolvedOrRunning t = isDependencyResolved t || isDependencyRunning t
                                        openTargets
                                        |> List.sortBy (fun t ->
                                            t.Dependencies @ List.filter inResolution t.SoftDependencies // Order by unresolved dependencies
                                            |> Seq.filter (isDependencyResolvedOrRunning >> not)
                                            |> Seq.length)
                                    let openList = 
                                        String.Join(", ", orderedOpen :> seq<_> |> (if orderedOpen.Length > 3 then Seq.take 3 else id) |> Seq.map (fun t ->  sprintf "'%s'" t.Name))
                                          + (if orderedOpen.Length > 3 then ", ..." else "")
                                    Trace.tracefn "FAKE worker idle because '%d' targets (%s) are still running and all ('%d') open targets (%s) depend on those. You might improve performance by splitting targets or removing dependencies."
                                        runningTasks.Length running openTargets.Length openList
                                    waitList <- waitList @ [ tcs ]
                                    reply.Reply (tcs.Task |> Async.AwaitTask)
                with e ->
                    for w:TaskCompletionSource<TargetContext * Target option> in waitList do
                        w.SetException (exn("mailbox failed", e))
                    waitList <- []
                    while true do
                        let! msg = inbox.Receive()
                        match msg with
                        | GetNextTarget (_, reply) ->
                            reply.Reply (async { return raise <| exn("mailbox failed", e) })
            }

            let mbox = MailboxProcessor.Start(body)
            { new IRunnerHelper with
                member _.GetNextTarget ctx = async {
                    let! repl = mbox.PostAndAsyncReply(fun reply -> GetNextTarget(ctx, reply))
                    return! repl
                }
            }

        let runOptimal workerNum (order:Target[] list) targetContext =
            let mgr = createCtxMgr order targetContext
            let targetRunner () =
                async {
                    let token = targetContext.CancellationToken
                    let! tctx, tt = mgr.GetNextTarget(targetContext)
                    let mutable ctx = tctx
                    let mutable nextTarget = tt
                    while nextTarget.IsSome && not token.IsCancellationRequested do
                        let newCtx = runSingleTarget nextTarget.Value ctx
                        let! tctx, tt = mgr.GetNextTarget(newCtx)
                        ctx <- tctx
                        nextTarget <- tt
                    return ctx
                } |> Async.StartAsTask
            Array.init workerNum (fun _ -> targetRunner())
            |> Task.WhenAll
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Seq.reduce mergeContext

    let private handleUserCancelEvent (cts:CancellationTokenSource) (e:ConsoleCancelEventArgs)=
        e.Cancel <- true
        printfn "Gracefully shutting down.."
        printfn "Press ctrl+c again to force quit"
        let __ =
            Console.CancelKeyPress
            |> Observable.first
            |> Observable.subscribe (fun _ ->  Environment.Exit 1)
        Process.killAllCreatedProcesses()
        cts.Cancel()
    
    /// <summary>
    /// Optional <c>TargetContext</c>
    /// </summary>
    type OptionalTargetContext = 
        private
            | Set of TargetContext
            | MaybeSet of TargetContext option
        member x.Context =
            match x with
            | Set t -> Some t
            | MaybeSet o -> o

    /// <summary>
    /// Runs a target and its dependencies.
    /// </summary>
    let internal runInternal singleTarget parallelJobs targetName args =
        match getLastDescription() with
        | Some d -> failwithf "You set a task description (%A) but didn't specify a task. Make sure to set the Description above the Target." d
        | None -> ()

        printfn "run %s" targetName
        let watch = System.Diagnostics.Stopwatch()
        watch.Start()

        Trace.tracefn "Building project with version: %s" BuildServer.buildVersion
        printDependencyGraph false targetName

        // determine a build order
        let order = determineBuildOrder targetName
        printRunningOrder order
        if singleTarget
        then Trace.traceImportant "Single target mode ==> Skipping dependencies."
        let allTargets = List.collect Seq.toList order
        use cts = new CancellationTokenSource()
        let context = TargetContext.Create targetName allTargets args cts.Token

        let context =
            let captureContext (f:'a->unit) =
                let ctx = Context.getExecutionContext()
                (fun a ->
                    let nctx = Context.getExecutionContext()
                    if ctx <> nctx then Context.setExecutionContext ctx
                    f a)

            let cancelHandler  = captureContext (handleUserCancelEvent cts)
            use __ =
                Console.CancelKeyPress
                |> Observable.first
                |> Observable.subscribe cancelHandler

            let context =
                // Figure out the order in in which targets can be run, and which can be run in parallel.
                if parallelJobs > 1 && not singleTarget then
                    Trace.tracefn "Running parallel build with %d workers" parallelJobs
                    // always try to keep "parallelJobs" runners busy
                    ParallelRunner.runOptimal parallelJobs order context
                else
                    let targets = order |> Seq.collect id |> Seq.toArray
                    let lastTarget = targets |> Array.last
                    if singleTarget then
                        runSingleTarget lastTarget context
                    else
                        targets |> Array.fold (fun context target -> runSingleTarget target context) context

            if context.HasError && not context.CancellationToken.IsCancellationRequested then
                runBuildFailureTargets context
            else 
                context

        let context = runFinalTargets {context with IsRunningFinalTargets=true}
        writeTaskTimeSummary watch.Elapsed context
        context           

    /// <summary>
    /// Creates a target in case of build failure (not activated).
    /// </summary>
    ///
    /// <param name="name">The target name</param>
    /// <param name="body">The target body</param>
    let createBuildFailure name body =
        create name body
        getBuildFailureTargets().Add(name,false)

    /// <summary>
    /// Activates the build failure target.
    /// </summary>
    ///
    /// <param name="name">The target name</param>
    let activateBuildFailure name =
        let _ = get name // test if target is defined
        getBuildFailureTargets().[name] <- true

    /// <summary>
    /// Deactivates the build failure target.
    /// </summary>
    ///
    /// <param name="name">The target name</param>
    let deactivateBuildFailure name =
        let t = get name // test if target is defined
        getBuildFailureTargets().[name] <- false

    /// <summary>
    /// Creates a final target (not activated).
    /// </summary>
    ///
    /// <param name="name">The target name</param>
    /// <param name="body">The target body</param>
    let createFinal name body =
        create name body
        getFinalTargets().Add(name,false)

    /// <summary>
    /// Activates the final target.
    /// </summary>
    ///
    /// <param name="name">The target name</param>
    let activateFinal name =
        let _ = get name // test if target is defined
        getFinalTargets().[name] <- true

    /// <summary>
    /// Deactivates the final target.
    /// </summary>
    ///
    /// <param name="name">The target name</param>
    let deactivateFinal name =
        let t = get name // test if target is defined
        getFinalTargets().[name] <- false

    let internal getBuildFailedException (context:TargetContext) =
        let targets = context.ErrorTargets |> Seq.map (fun (_er, target) -> target.Name) |> Seq.distinct
        let targetStr = String.Join(", ", targets)
        let errorMsg =
            if context.ErrorTargets.Length = 1 then
                sprintf "Target '%s' failed." targetStr
            else
                sprintf "Targets '%s' failed." targetStr
        let inner = AggregateException(AggregateException().Message, context.ErrorTargets |> Seq.map fst)
        BuildFailedException(context, errorMsg, inner)

    /// <summary>
    /// Updates build status based on <c>OptionalTargetContext</c>
    /// Will not update status if <c>OptionalTargetContext</c> is <c>MaybeSet</c> with value <c>None</c>
    /// </summary>
    ///
    /// <param name="context">The target context</param>
    let updateBuildStatus (context:OptionalTargetContext) =
        match context.Context with
        | Some c when c.PreviousTargets.Length = 0 -> Trace.setBuildState TagStatus.Warning
        | Some c when c.HasError -> let targets = c.ErrorTargets |> Seq.map (fun (_er, target) -> target.Name) |> Seq.distinct
                                    let targetStr = String.Join(", ", targets)
                                    if c.ErrorTargets.Length = 1 then
                                        Trace.setBuildStateWithMessage TagStatus.Failed (sprintf "Target '%s' failed." targetStr)
                                    else
                                        Trace.setBuildStateWithMessage TagStatus.Failed (sprintf "Targets '%s' failed." targetStr)                                    
        | Some _ -> Trace.setBuildState TagStatus.Success
        | _ -> ()

    /// <summary>
    /// If `TargetContext option` is Some and has error, raise it as a BuildFailedException
    /// </summary>
    ///
    /// <param name="context">The target context</param>
    let raiseIfError (context:OptionalTargetContext) =
        let c = context.Context
        if c.IsSome && c.Value.HasError && not c.Value.CancellationToken.IsCancellationRequested then
            getBuildFailedException c.Value
            |> raise
    
    type internal ArgResults =
        | ListTargets
        | WriteInfo of file:string
        | NoAction
        | ExecuteTarget of target:string option * arguments:string list * parallelJobs:int * singleTarget:bool
    
    let private argResultsVar = "Fake.Core.Target.ArgResults"

    let private privGetArgResults, private removeArgResults, private setArgResults =
        FakeVar.define argResultsVar

    let internal parseArgsAndSetEnvironment () =
        let ctx = Context.forceFakeContext ()
        
        let trySplitEnvArg (arg:string) =
            let idx = arg.IndexOf('=')
            if idx < 0 then
                Trace.traceError (sprintf "Argument for -e should contain '=' but was '%s', the argument will be ignored." arg)
                None
            else
                Some (arg.Substring(0, idx), arg.Substring(idx + 1))
        let results =
            try
                let res = TargetCli.parseArgs (ctx.Arguments |> List.toArray)
                res |> Choice1Of2
            with :? DocoptException as e -> Choice2Of2 e
        match results with
        | Choice1Of2 results ->
            let envs =
                match DocoptResult.tryGetArguments "--environment-variable" results with
                | Some args ->
                    args |> List.choose trySplitEnvArg
                | None -> []
            for key, value in envs do Environment.setEnvironVar key value

            if DocoptResult.hasFlag "--list" results then
                ListTargets
            elif DocoptResult.hasFlag "--write-info" results then
                match DocoptResult.tryGetArgument "<file>" results with
                | None -> failwithf "--write-info needs an file argument"
                | Some arg ->
                    setCollectStack true
                    WriteInfo arg
            elif DocoptResult.hasFlag "-h" results || DocoptResult.hasFlag "--help" results then
                printfn "%s" TargetCli.targetCli
                printfn "Hint: Run 'fake run <build.fsx> target <target> --help' to get help from your target."
                NoAction
            elif DocoptResult.hasFlag "--version" results then
                let targetAssembly = Assembly.GetAssembly(typeof<Target>)
                printfn "Target Module Version: %s" (targetAssembly.GetName().Version.ToString())
                NoAction
            else
                let target =
                    match DocoptResult.tryGetArgument "<target>" results with
                    | None ->
                        match DocoptResult.tryGetArgument "--target" results with
                        | None ->
                            match Environment.environVarOrNone "target" with
                            | Some arg ->
                                Trace.log
                                    <| sprintf "Using target '%s' from the 'target' environment variable." arg
                                Some arg
                            | None -> None                                                                
                        | Some arg -> Some arg
                    | Some arg ->
                        match DocoptResult.tryGetArgument "--target" results with
                        | None -> ()
                        | Some innerArg ->
                            Trace.traceImportant
                                <| sprintf "--target '%s' is ignored when 'target %s' is given" innerArg arg
                        Some arg
                let parallelJobs =
                    match DocoptResult.tryGetArgument "--parallel" results with
                    | Some arg ->
                        match Int32.TryParse(arg) with
                        | true, i -> i
                        | _ -> failwithf "--parallel needs an integer argument, could not parse '%s'" arg
                    | None ->
                        Environment.environVarOrDefault "parallel-jobs" "1" |> int
                let singleTarget =
                    match DocoptResult.hasFlag "--single-target" results with
                    | true -> true
                    | false -> Environment.hasEnvironVar "single-target"
                let arguments =
                    match DocoptResult.tryGetArguments "<targetargs>" results with
                    | Some args -> args
                    | None -> []
                ExecuteTarget(target, arguments, parallelJobs, singleTarget)
        | Choice2Of2 e ->
            // To ensure exit code.
            raise <| exn (sprintf "Usage error: %s\n%s" e.Message TargetCli.targetCli, e)

    let internal getArgs () =
        match privGetArgResults () with
        | Some s -> s
        | None -> parseArgsAndSetEnvironment() 

    let internal getRunFunction allowAdditionalArgs defaultTarget =
        match getArgs() with
        | ListTargets ->
            listAvailable()
            None
        | WriteInfo file ->
            writeInfoFile file
            None
        | NoAction ->
            None
        | ExecuteTarget(target, arguments, parallelJobs, singleTarget) ->
            if not allowAdditionalArgs && arguments <> [] then
                failwithf "The following arguments could not be parsed: %A\nTo forward arguments to your targets you need to use \nTarget.runOrDefaultWithArguments instead of Target.runOrDefault" arguments
            match target, defaultTarget with
            | Some t, _ -> Some(fun () -> Some(runInternal singleTarget parallelJobs t arguments))
            | None, Some t -> Some(fun () -> Some(runInternal singleTarget parallelJobs t arguments))
            | None, None -> Some (fun () -> listAvailable()
                                            None)

    let private runFunction (targetFunction:(unit -> TargetContext option) Option) = 
        match targetFunction with
        | Some f -> OptionalTargetContext.MaybeSet(f())
        | _ -> OptionalTargetContext.MaybeSet(None)
        
    /// <summary>
    /// Allows to initialize FAKE, see initEnvironment and getArguments
    /// </summary>
    let internal initAndProcess proc =
        match privGetArgResults () with
        | Some args -> proc args
        | None ->
            let res = parseArgsAndSetEnvironment()
            setArgResults res
            proc res


    /// <summary>
    /// Run functions which don't throw and return the context after all targets have been executed.
    /// </summary>
    module WithContext =
        /// Runs a target and its dependencies and returns an <c>OptionalTargetContext</c>
        let run parallelJobs targetName args = runInternal false parallelJobs targetName args |> OptionalTargetContext.Set

        /// Runs the command given on the command line or the given target when no target is given & get context
        let runOrDefault defaultTarget =
            getRunFunction false (Some(defaultTarget)) |> runFunction

        /// Runs the command given on the command line or the given target when no target is given & get context
        let runOrDefaultWithArguments defaultTarget =
            getRunFunction true (Some(defaultTarget)) |> runFunction

        /// Runs the target given by the target parameter or lists the available targets & get context
        let runOrList() =
            getRunFunction false None |> runFunction

    /// <summary>
    /// Allows to initialize the environment before defining targets
    /// </summary>
    /// <remarks>
    /// This function should be used at the start of your fake script
    /// see <a href="https://github.com/fsharp/FAKE/issues/2283">issues #2283</a>
    /// Alternatively, you can use <c>Target.getArguments()</c> instead
    /// </remarks>
    let initEnvironment () =
        initAndProcess ignore

    /// <summary>
    /// Allows to retrieve the arguments passed into the current execution, 
    /// when <c>Target.run*withArguments</c> overloads are used, see
    /// <a href="guide/core-targets.html#Targets-with-arguments">Targets with arguments</a> 
    /// </summary>
    /// <remarks>
    /// This function should be used at the start of your fake script
    /// Alternatively, you can use <c>Target.initEnvironment()</c> instead,
    /// Note: This function usually returns <c>Some [||]</c>, it will return <c>None</c> when 
    /// No actual execution was requested (for example because of <c>--list</c>),
    /// you shouldn't execute any side effects when <c>None</c> is returned 
    /// (you should never execute side effects but you can use this flag to check if needed)
    /// </remarks>
    let getArguments () =
        initAndProcess (function 
            | ExecuteTarget (_, args, _, _) -> args |> List.toArray |> Some
            | _ -> None)

    /// <summary>
    /// Runs a target and its dependencies
    /// </summary>
    ///
    /// <param name="targetName">The target name</param>
    /// <param name="args">The arguments list</param>
    let run parallelJobs targetName args : unit =
        WithContext.run parallelJobs targetName args |> raiseIfError

    /// <summary>
    /// Runs the command given on the command line or the given target when no target is given
    /// </summary>
    ///
    /// <param name="targetName">The default target name</param>
    let runOrDefault (defaultTarget:string) : unit =
        WithContext.runOrDefault defaultTarget |> raiseIfError

    /// <summary>
    /// Runs the command given on the command line or the given target when no target is given
    /// </summary>
    ///
    /// <param name="targetName">The default target name</param>
    let runOrDefaultWithArguments (defaultTarget:string) : unit =
        WithContext.runOrDefaultWithArguments defaultTarget |> raiseIfError

    /// <summary>
    /// Runs the target given by the target parameter or lists the available targets
    /// </summary>
    let runOrList() : unit =
        WithContext.runOrList() |> raiseIfError
