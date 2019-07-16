module Fake.Core.TargetTests

open System
open Fake.Core
open Expecto
open System.Collections
open System.Collections.Generic

let run targetName =
    match (Target.WithContext.run 1 targetName []).Context with
    | Some c -> c
    | None -> failwithf "Expected (Some(context)) but got None!"

let runParallel targetName =
    match (Target.WithContext.run 3 targetName []).Context with
    | Some c -> c
    | None -> failwithf "Expected (Some(context)) but got None!"

open Fake.Core.TargetOperators

let (|Target|) (t : Target) =
    Target t.Name

let (|TargetSet|) (t : seq<Target>) =
    let list = t |> Seq.map (fun t -> t.Name) |> Seq.sort |> Seq.toList
    TargetSet list

let DoNothing = ignore
let determineBuildOrder a _ = Target.determineBuildOrder a
let validateBuildOrder a b = ignore a; ignore b

let testCaseMultipleRuns name f = [
    Fake.ContextHelper.fakeContextTestCase (sprintf "%s - run" name) <| fun c -> f run c 
    Fake.ContextHelper.fakeContextTestCase (sprintf "%s - runParallel" name) <| fun c -> f runParallel c 
]

type TestTarget =
| TestTarget of string
| TestTargetGroup of TestTarget list
    member x.TargetNames = seq {
        match x with
        | TestTarget s -> yield s
        | TestTargetGroup g -> yield! g |> Seq.collect (fun g -> g.TargetNames)
    }

module TestTarget =
    let create name body =
        Target.create name body
        TestTarget name

[<Tests>]
let tests =
  testList "Fake.Core.Target.Tests" (
    [
    Fake.ContextHelper.fakeContextTestCaseAssertTime (TimeSpan.FromSeconds 18.0) "basic performance #2036" <| fun _ ->
        let counter = 2500
        CoreTracing.setTraceListeners [] // silence
        let all_Pipelines = Dictionary<string,TestTarget>(System.StringComparer.OrdinalIgnoreCase :> IEqualityComparer<string>)

        /// The last target is the name of the pipeline
        let SetPipelineRelations (targets:TestTarget list) : unit =
            let targetNames = targets |> Seq.collect (fun t -> t.TargetNames)
            let last = targetNames |> Seq.last
            all_Pipelines.[last] <- TestTargetGroup targets
            for (a,b) in targetNames |> Seq.pairwise do
                a ?=> b |> ignore
                a ==> last |> ignore

        let CreatePipeline name (targets: TestTarget list) : unit =    
            let p = TestTarget.create name ignore
            SetPipelineRelations [ yield (TestTargetGroup targets); yield p ]

        let t_targets =
            TestTargetGroup [
                for i in 0 .. counter ->
                    TestTarget.create (sprintf "Target_%i" i) ignore
            ]
            
        [ t_targets ] |> CreatePipeline "Run.1"
        //Target.printDependencyGraph true "Run.1"
        Target.runOrDefaultWithArguments "Run.1"

    Fake.ContextHelper.fakeContextTestCaseAssertTime (TimeSpan.FromSeconds 13.0) "basic performance (1)" <| fun _ ->
        let counter = 5000
        CoreTracing.setTraceListeners [] // silence
        Target.create "A" ignore
        Target.create "U" ignore
        for i in 1 .. counter do
            let n = sprintf "T%d" i
            Target.create n ignore
            "A" ==> n ==> "U" |> ignore
            
        let context = run "U"
        ignore context

    
    Fake.ContextHelper.fakeContextTestCase "handle casing in target runner" <| fun _ ->
        Target.create "aA" ignore
        Target.create "bB" ignore
        Target.create "cC" ignore

        "bB" ==> "cC" |> ignore
        "aA" ==> "BB" |> ignore
        "Aa" ==> "Cc" |> ignore

        let order = Target.determineBuildOrder "cC"
        let targets = order |> Seq.concat |> Seq.toList
        
        printfn "targets [%s]" (System.String.Join(",", targets |> Seq.map (fun t -> t.Name)))
        let ctx = TargetContext.Create "cc" targets [] System.Threading.CancellationToken.None

        let mgr = Target.ParallelRunner.createCtxMgr order ctx
        let runSyncWithTimeout a =
            let t = a |> Async.StartAsTask
            if not (t.Wait(10000)) then
                failwithf "Task did not finish after a second!"
            t.Result
        let ctx, target = mgr.GetNextTarget ctx |> runSyncWithTimeout
        Expect.isSome target "expected next target"
        Expect.equal target.Value.Name "aA" "Expected target aA"
        let setFinished ctx s =
            let result = { Error = None; Time = System.TimeSpan.FromSeconds 0.; Target = Target.get s; WasSkipped = false }
            { ctx with PreviousTargets = result :: ctx.PreviousTargets }
        let ctx, target = mgr.GetNextTarget (setFinished ctx "Aa") |> runSyncWithTimeout
        Expect.isSome target "expected next target"
        Expect.equal target.Value.Name "bB" "Expected target bB"
        let ctx, target = mgr.GetNextTarget (setFinished ctx "bB") |> runSyncWithTimeout
        Expect.isSome target "expected next target"
        Expect.equal target.Value.Name "cC" "Expected target cC"
        let ctx, target = mgr.GetNextTarget (setFinished ctx "CC") |> runSyncWithTimeout
        Expect.isNone target "expected no next target"


    Fake.ContextHelper.fakeContextTestCase "check simple parallelism" <| fun _ ->
        Target.create "a" ignore
        Target.create "b" ignore
        Target.create "c" ignore

        Target.create "dep" ignore

        "a" ==> "dep" |> ignore
        "b" ==> "dep" |> ignore
        "c" ==> "dep" |> ignore

        let order = Target.determineBuildOrder "dep"
        //validateBuildOrder order "dep"
        match order with
        | [TargetSet ["a"; "b"; "c"]; [|Target "dep"|]] ->
            // as expected
            ()
        | _ ->
            Expect.isTrue false (sprintf "inconsistent order: %A" order)

    Fake.ContextHelper.fakeContextTestCase "issue #1395 example" <| fun _ ->
        Target.create "T1" DoNothing
        Target.create "T2.1" DoNothing
        Target.create "T2.2" DoNothing
        Target.create "T2.3" DoNothing
        Target.create "T3" DoNothing
        Target.create "T4" DoNothing

        // create a graph
        "T1" ==> "T2.1" ==> "T2.2" ==> "T2.3" |> ignore
        "T1" ==> "T3" |> ignore
        "T2.3" ==> "T4" |> ignore
        "T3" ==> "T4" |> ignore

        let order = determineBuildOrder "T4" 2
        validateBuildOrder order "T4"

        match order with
            | [[|Target "T1"|];TargetSet ["T2.1"; "T3"];[|Target "T2.2"|];[|Target "T2.3"|];[|Target "T4"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order


    Fake.ContextHelper.fakeContextTestCase "casing in targets - #2000" <| fun _ ->

        Target.create "CleanUp" DoNothing

        Target.create "RunUnitTests" DoNothing

        Target.create "RunAllTests" DoNothing

        Target.create "Default" DoNothing

        "CleanUp"
            ==> "RunUnitTests"
            ==> "RunAlltests"
            ==> "Default"

        [ "CleanUp";
          "RunUnitTests";
          "RunAlltests";
          "Default" ]
        |> Seq.iter (fun i ->
                        let t = i |> Target.get
                        Trace.tracefn "%s has %A" i t.Dependencies )


        let order = determineBuildOrder "Default" 1
        validateBuildOrder order "Default"
        match order with
            | [[|Target "CleanUp"|];[|Target "RunUnitTests"|];[|Target "RunAllTests"|];[|Target "Default"|]] ->
                // as expected
                ()
            | _ ->
                failwithf "unexpected order: %A" order
        Target.runOrDefault "Default"

    Fake.ContextHelper.fakeContextTestCase "Diamonds are resolved correctly" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c" DoNothing
        Target.create "d" DoNothing

        // create graph
        "a" ==> "b" ==> "d" |> ignore
        "a" ==> "c" ==> "d" |> ignore

        let order = determineBuildOrder "d" 2
        validateBuildOrder order "d"

        match order with
            | [[|Target "a"|];TargetSet ["b"; "c"];[|Target "d"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "Initial Targets Can Run Concurrently" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c1" DoNothing
        Target.create "c2" DoNothing
        Target.create "d" DoNothing

        // create graph
        "a" ==> "b" ==> "d" |> ignore
        "c1" ==> "c2" ==> "d" |> ignore

        let order = determineBuildOrder "d" 2
        validateBuildOrder order "d"

        match order with
            | [TargetSet ["a"; "c1"];TargetSet ["b"; "c2"];[|Target "d"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "BlythMeisters Scenario Of Complex Build Order Is Correct" <| fun _ ->
        Target.create "PrepareBuild" DoNothing
        Target.create "CreateWholeCaboodle" DoNothing
        Target.create "UpdateVersions" DoNothing
        Target.create "PreBuildVerifications" DoNothing
        Target.create "BuildWholeCaboodle" DoNothing
        Target.create "RunUnitTests" DoNothing
        Target.create "RunIntTests" DoNothing
        Target.create "CreateDBNugets" DoNothing
        Target.create "DropIntDatabases" DoNothing
        Target.create "DeployIntDatabases" DoNothing
        Target.create "CreateNugets" DoNothing
        Target.create "PublishNugets" DoNothing

        "PrepareBuild" ==> "CreateWholeCaboodle" |> ignore
        "PrepareBuild" ==> "UpdateVersions" |> ignore
        "CreateWholeCaboodle" ==> "PreBuildVerifications" |> ignore
        "UpdateVersions" ==> "PreBuildVerifications" |> ignore
        "PreBuildVerifications" ==> "BuildWholeCaboodle" |> ignore
        "PreBuildVerifications" ==> "CreateDBNugets" |> ignore
        "PreBuildVerifications" ==> "DropIntDatabases" |> ignore
        "BuildWholeCaboodle" ==> "CreateNugets" |> ignore
        "BuildWholeCaboodle" ==> "RunUnitTests" |> ignore
        "BuildWholeCaboodle" ==> "RunIntTests" |> ignore
        "CreateDBNugets" ==> "DeployIntDatabases" |> ignore
        "DropIntDatabases" ==> "DeployIntDatabases" |> ignore
        "DeployIntDatabases" ==> "RunIntTests" |> ignore
        "CreateNugets" ==> "PublishNugets" |> ignore
        "CreateDBNugets" ==> "PublishNugets" |> ignore
        "RunUnitTests" ==> "PublishNugets" |> ignore
        "RunIntTests" ==> "PublishNugets" |> ignore

        let order = determineBuildOrder "PublishNugets" 2
        validateBuildOrder order "PublishNugets"

        match order with
            | [
               TargetSet ["PrepareBuild"];
               TargetSet ["CreateWholeCaboodle"; "UpdateVersions"];
               TargetSet ["PreBuildVerifications"];
               TargetSet ["BuildWholeCaboodle"; "CreateDBNugets"; "DropIntDatabases"];
               TargetSet ["CreateNugets"; "DeployIntDatabases"; "RunUnitTests"];
               TargetSet ["RunIntTests"];
               TargetSet ["PublishNugets"];
               ] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "BlythMeisters Scenario Of Even More Complex Build Order Is Correct" <| fun _ ->
        Target.create "PrepareBuild" DoNothing
        Target.create "CreateWholeCaboodle" DoNothing
        Target.create "UpdateVersions" DoNothing
        Target.create "PreBuildVerifications" DoNothing
        Target.create "BuildWholeCaboodle" DoNothing
        Target.create "RunUnitTests" DoNothing
        Target.create "RunIntTests" DoNothing
        Target.create "CreateDBNugets" DoNothing
        Target.create "DropIntDatabases" DoNothing
        Target.create "DeployIntDatabases" DoNothing
        Target.create "CreateNugets" DoNothing
        Target.create "PublishNugets" DoNothing

        "PrepareBuild" ==> "CreateWholeCaboodle" ==> "PreBuildVerifications" |> ignore
        "PrepareBuild" ==> "UpdateVersions" ==> "PreBuildVerifications" |> ignore
        "PreBuildVerifications" ==> "CreateDBNugets" ==> "DeployIntDatabases" |> ignore
        "PreBuildVerifications" ==> "DropIntDatabases" ==> "DeployIntDatabases" |> ignore
        "PreBuildVerifications" ==> "BuildWholeCaboodle" |> ignore
        "BuildWholeCaboodle" ==> "RunUnitTests"  |> ignore
        "BuildWholeCaboodle" ==> "RunIntTests" |> ignore
        "DeployIntDatabases" ==> "RunIntTests" |> ignore
        "BuildWholeCaboodle" ==> "CreateNugets" |> ignore
        "RunIntTests" ==> "CreateNugets" |> ignore
        "RunUnitTests" ==> "CreateNugets"  |> ignore
        "RunUnitTests" ==> "PublishNugets" |> ignore
        "RunIntTests" ==> "PublishNugets" |> ignore
        "CreateDBNugets" ==> "PublishNugets" |> ignore
        "CreateNugets" ==> "PublishNugets" |> ignore

        let order = determineBuildOrder "PublishNugets" 2
        validateBuildOrder order "PublishNugets"

        match order with
            | [
               TargetSet ["PrepareBuild"];
               TargetSet ["CreateWholeCaboodle"; "UpdateVersions"];
               TargetSet ["PreBuildVerifications"];
               TargetSet ["BuildWholeCaboodle"; "CreateDBNugets"; "DropIntDatabases"];
               TargetSet ["DeployIntDatabases"; "RunUnitTests"];
               TargetSet ["RunIntTests"];
               TargetSet ["CreateNugets"];
               TargetSet ["PublishNugets"];
               ] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "Spurs run as early as possible" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c1" DoNothing
        Target.create "c2" DoNothing
        Target.create "d" DoNothing

        // create graph
        "a" ==> "b" ==> "d" |> ignore
        "a" ==> "c1" ==> "c2" ==> "d" |> ignore

        let order = determineBuildOrder "d" 2
        validateBuildOrder order "d"

        match order with
            | [[|Target "a"|];TargetSet ["b"; "c1"];[|Target "c2"|];[|Target "d"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "Spurs run as early as possible 3 and 2 length" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b1" DoNothing
        Target.create "b2" DoNothing
        Target.create "c1" DoNothing
        Target.create "c2" DoNothing
        Target.create "c3" DoNothing
        Target.create "d" DoNothing

        // create graph
        "a" ==> "b1" ==> "b2" ==> "d" |> ignore
        "a" ==> "c1" ==> "c2" ==> "c3" ==> "d" |> ignore

        let order = determineBuildOrder "d" 2
        validateBuildOrder order "d"

        match order with
            | [[|Target "a"|];TargetSet ["b1"; "c1"];TargetSet ["b2"; "c2"];[|Target "c3"|];[|Target "d"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "Spurs run as early as possible (reverse definition order)" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c1" DoNothing
        Target.create "c2" DoNothing
        Target.create "d" DoNothing

        // create graph
        "a" ==> "c1" ==> "c2" ==> "d" |> ignore
        "a" ==> "b" ==> "d" |> ignore

        let order = determineBuildOrder "d" 2
        validateBuildOrder order "d"

        match order with
            | [[|Target "a"|];TargetSet ["b"; "c1"];[|Target "c2"|];[|Target "d"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "Spurs run as early as possible split on longer spur" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c1" DoNothing
        Target.create "c21" DoNothing
        Target.create "c22" DoNothing
        Target.create "d" DoNothing

        // create graph
        "a" ==> "b" ==> "d" |> ignore
        "a" ==> "c1" ==> "c21" ==> "d" |> ignore
        "a" ==> "c1" ==> "c22" ==> "d" |> ignore

        let order = determineBuildOrder "d" 2
        validateBuildOrder order "d"

        match order with
            | [[|Target "a"|];TargetSet ["b"; "c1"];TargetSet ["c21"; "c22"];[|Target "d"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "3 way Spurs run as early as possible" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c1" DoNothing
        Target.create "c2" DoNothing
        Target.create "d1" DoNothing
        Target.create "d2" DoNothing
        Target.create "d3" DoNothing
        Target.create "e" DoNothing

        // create graph
        "a" ==> "b" ==> "e" |> ignore
        "a" ==> "c1" ==> "c2" ==> "e" |> ignore
        "a" ==> "d1" ==> "d2" ==> "d3" ==> "e" |> ignore

        let order = determineBuildOrder "e" 2
        validateBuildOrder order "e"

        match order with
            | [[|Target "a"|];TargetSet ["b"; "c1"; "d1"];TargetSet ["c2"; "d2"];[|Target "d3"|];[|Target "e"|]] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "Soft dependencies are respected when dependees are present" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c" DoNothing
        Target.create "d" DoNothing
        Target.create "e" DoNothing
        Target.create "f" DoNothing


        "a" ==> "b" ==> "c" |> ignore
        // d does not depend on c, but if something else forces c to run, then d must come after c.
        "d" <=? "c" |> ignore

        // Running f will run  c, d, and f.  The soft dependency of d on c means that c must run first.
        "d" ==> "f" |> ignore
        "e" ==> "f" |> ignore
        "c" ==> "f" |> ignore

        let order = determineBuildOrder "f" 2

        validateBuildOrder order "f"

        match order with
        | [TargetSet ["a"; "e"];TargetSet ["b";];[|Target "c"|];TargetSet ["d"];[|Target "f"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order
        ()

    Fake.ContextHelper.fakeContextTestCase "Soft dependencies are ignored when dependees are not present" <| fun _ ->
        Target.create "a" DoNothing
        Target.create "b" DoNothing
        Target.create "c" DoNothing
        Target.create "d" DoNothing
        Target.create "e" DoNothing


        "a" ==> "b" ==> "c" |> ignore
        // d does not depend on c, but if something else forces c to run, then d must come after c.
        "c" ?=> "d" |> ignore

        // Running e will not run c, due to soft dependency
        "d" ==> "e" |> ignore
        "b" ==> "e" |> ignore

        let order = determineBuildOrder "e" 2

        validateBuildOrder order "e"

        match order with
        | [TargetSet ["a";"d"];[|Target "b"|];[|Target "e"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order
        ()

    Fake.ContextHelper.fakeContextTestCase "Fsharp.Data Dependencies single worker (broken)" <| fun _ ->
        Target.create "Clean" DoNothing
        Target.create "AssemblyInfo" DoNothing
        Target.create "Build" DoNothing
        Target.create "BuildTests" DoNothing
        Target.create "BuildConsoleTests" DoNothing
        Target.create "RunTests" DoNothing
        Target.create "FSharp.Data.Tests" DoNothing
        Target.create "FSharp.Data.DesignTime.Tests" DoNothing
        Target.create "RunConsoleTests" DoNothing
        Target.create "All" DoNothing

        "FSharp.Data.Tests" ==> "RunTests" |> ignore
        "FSharp.Data.DesignTime.Tests" ==> "RunTests" |> ignore
        "Clean" ==> "AssemblyInfo" ==> "Build" |> ignore
        "Build" ==> "All" |> ignore
        "BuildTests" ==> "All" |> ignore
        "BuildConsoleTests" ==> "All" |> ignore
        "RunTests" ==> "All" |> ignore
        "RunConsoleTests" ==> "All" |> ignore

        let order = determineBuildOrder "All" 1
        validateBuildOrder order "All"

        match order with
            | [
               TargetSet ["BuildConsoleTests"; "BuildTests"; "Clean"; "FSharp.Data.DesignTime.Tests"; "FSharp.Data.Tests"; "RunConsoleTests"];
               TargetSet ["AssemblyInfo"; "RunTests" ];
               TargetSet ["Build"];
               TargetSet ["All"];
               ] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" order

    Fake.ContextHelper.fakeContextTestCase "Test that we run a simple target with dependency" <| fun _ ->
      Target.create "SimpleTest" ignore
      Target.create "Dependency" ignore

      "Dependency" ==> "SimpleTest" |> ignore
      let context = run "SimpleTest"
      Expect.equal false context.HasError "Expected both tasks to succeed"
      Expect.equal 2 context.PreviousTargets.Length "Expected context to contain both targets"

    Fake.ContextHelper.fakeContextTestCase "Test we output targets after failing targets" <| fun _ ->
      Target.create "SimpleTest" ignore
      Target.create "Dependency" (fun _ -> failwith "failed dependency")

      "Dependency" ==> "SimpleTest" |> ignore
      let context = run "SimpleTest"
      Expect.equal true context.HasError "Expected failure"
      Expect.equal 2 context.PreviousTargets.Length "Expected context to contain both targets"  // second one as "skipped"

    // These dependencies are taken from large .NET/COM VS sln. Only the names have been changed.
    // Printing the running order was taking 3 minutes and ~ 12 GB or RAM using FAKE 5.15
    Fake.ContextHelper.fakeContextTestCase "Running order resolves quickly when there are numerous targets with numerous dependencies" <| fun _ ->
        Target.create "Bersil" ignore
        Target.create "komatiks" ignore
        Target.create "Stalk" ignore
        Target.create "swiftest" ignore
        Target.create "horsepower-year" ignore
        Target.create "Phascum" ignore
        Target.create "wen" ignore
        Target.create "comparableness" ignore
        Target.create "outgreen" ignore
        Target.create "Takhaar" ignore
        Target.create "piperidine" ignore
        Target.create "interproximate" ignore
        Target.create "collection" ignore
        Target.create "kaw-" ignore
        Target.create "hydrachnid" ignore
        Target.create "subarytenoidal" ignore
        Target.create "aguamas" ignore
        Target.create "ondogram" ignore
        Target.create "archeal" ignore
        Target.create "hollowroot" ignore
        Target.create "white-bosomed" ignore
        Target.create "nonlicentiousness" ignore
        Target.create "unslashed" ignore
        Target.create "micrometeorite" ignore
        Target.create "carp-" ignore
        Target.create "Merrie" ignore
        Target.create "matriarches" ignore
        Target.create "soundless" ignore
        Target.create "pro-Elizabethan" ignore
        Target.create "coadjute" ignore
        Target.create "hypozoic" ignore
        Target.create "demasculinize" ignore
        Target.create "nonsubjectiveness" ignore
        Target.create "relacing" ignore
        Target.create "pettings" ignore
        Target.create "terroristic" ignore
        Target.create "quincunx" ignore
        Target.create "unquestioningly" ignore
        Target.create "Peosta" ignore
        Target.create "apickpack" ignore
        Target.create "adherer" ignore
        Target.create "injected" ignore
        Target.create "panmixias" ignore
        Target.create "Jenkinson" ignore
        Target.create "orchiocele" ignore
        Target.create "beesting" ignore
        Target.create "baratheas" ignore
        Target.create "hydrolatry" ignore
        Target.create "enormities" ignore
        Target.create "trunkback" ignore
        Target.create "poddish" ignore
        Target.create "nonresponsible" ignore
        Target.create "litotic" ignore
        Target.create "Sims" ignore
        Target.create "Aaru" ignore
        Target.create "potto" ignore
        Target.create "adenocarcinomas" ignore
        Target.create "hyperpersonal" ignore
        Target.create "mesioincisal" ignore
        Target.create "pickler" ignore
        Target.create "viscerate" ignore
        Target.create "isologues" ignore
        Target.create "peptidoglycan" ignore
        Target.create "contrivement" ignore
        Target.create "yawnproof" ignore
        Target.create "draffish" ignore
        Target.create "Vietcong" ignore
        Target.create "self-gratulation" ignore
        Target.create "baronetizing" ignore
        Target.create "surrendry" ignore
        Target.create "rhyacolite" ignore
        Target.create "quicksilvers" ignore
        Target.create "clingstones" ignore
        Target.create "valvelets" ignore
        Target.create "paleologist" ignore
        Target.create "catacoustics" ignore
        Target.create "marlinespike" ignore
        Target.create "reinsertions" ignore
        Target.create "verruculose" ignore
        Target.create "mobilizable" ignore
        Target.create "consonantalize" ignore
        Target.create "withgate" ignore
        Target.create "milage" ignore
        Target.create "splurgiest" ignore
        Target.create "lords-in-waiting" ignore
        Target.create "numbnesses" ignore
        Target.create "grovy" ignore
        Target.create "Nixa" ignore
        Target.create "piques" ignore
        Target.create "snubs" ignore
        Target.create "leered" ignore
        Target.create "Latium" ignore
        Target.create "caranda" ignore
        Target.create "mediatise" ignore
        Target.create "erichtoid" ignore
        Target.create "rangelands" ignore
        Target.create "ichthyol." ignore
        Target.create "Pro-kansan" ignore
        Target.create "noninheritability" ignore
        Target.create "breadroot" ignore
        Target.create "petro-" ignore
        Target.create "Callipus" ignore
        Target.create "begnawn" ignore
        Target.create "sclerodactylia" ignore
        Target.create "ungeuntarium" ignore
        Target.create "muensters" ignore
        Target.create "cometography" ignore
        Target.create "Luigino" ignore
        Target.create "glorified" ignore
        Target.create "earth-nut" ignore
        Target.create "rubine" ignore
        Target.create "FAR" ignore
        Target.create "hagioscopic" ignore
        Target.create "formicarian" ignore
        Target.create "ever-strong" ignore
        Target.create "hyeniform" ignore
        Target.create "appearer" ignore
        Target.create "Danelage" ignore
        Target.create "Nicktown" ignore
        Target.create "exasperating" ignore
        Target.create "Maxa" ignore
        Target.create "overanalyze" ignore
        Target.create "Gohila" ignore
        Target.create "semi-aridity" ignore
        Target.create "Arthrobacter" ignore
        Target.create "weaving" ignore
        Target.create "psychologist's" ignore
        Target.create "hastily" ignore
        Target.create "papyrographic" ignore
        Target.create "unfix" ignore
        Target.create "allegresse" ignore
        Target.create "formats" ignore
        Target.create "hydrosulphocyanic" ignore
        Target.create "parlante" ignore
        Target.create "mnemonicalist" ignore
        Target.create "conspiratorially" ignore
        Target.create "jaspis" ignore
        Target.create "Quezaltenango" ignore
        Target.create "contin" ignore
        Target.create "amorphi" ignore
        Target.create "Chaetopoda" ignore
        Target.create "codline" ignore
        Target.create "Aldermaston" ignore
        Target.create "hypermoralistic" ignore
        Target.create "voter" ignore
        Target.create "cowshed" ignore
        Target.create "explorator" ignore
        Target.create "Kovrov" ignore
        Target.create "Mulvihill" ignore
        Target.create "Esra" ignore
        Target.create "fleecer" ignore
        Target.create "mischio" ignore
        Target.create "synactic" ignore
        Target.create "hepatectomies" ignore
        Target.create "thoracoabdominal" ignore
        Target.create "senachie" ignore
        Target.create "prefearful" ignore
        Target.create "compresent" ignore
        Target.create "shillings" ignore
        Target.create "cubitoplantar" ignore
        Target.create "Shirlie" ignore
        Target.create "Lookeba" ignore
        Target.create "ornithologic" ignore
        Target.create "Billye" ignore
        Target.create "subtetanic" ignore
        Target.create "outargued" ignore
        Target.create "illiberalized" ignore
        Target.create "Heptanesian" ignore
        Target.create "ITT" ignore
        Target.create "uninnocent" ignore
        Target.create "empty-looking" ignore
        Target.create "rough-hackled" ignore
        Target.create "vined" ignore
        Target.create "arthropodan" ignore
        Target.create "purpurean" ignore
        Target.create "hypercholesterolemia" ignore
        Target.create "Georgi" ignore
        Target.create "rain-dropping" ignore
        Target.create "outdoorness" ignore
        Target.create "conflagrating" ignore
        Target.create "brauna" ignore
        Target.create "eelpot" ignore
        Target.create "Heteroousian" ignore
        Target.create "unmarvellously" ignore
        Target.create "flywinch" ignore
        Target.create "Saone" ignore
        Target.create "biocatalytic" ignore
        Target.create "Pannini" ignore
        Target.create "nitta" ignore
        Target.create "refreshener" ignore
        Target.create "beshouts" ignore
        Target.create "diphen-" ignore
        Target.create "Andrej" ignore
        Target.create "druggeries" ignore
        Target.create "Senora" ignore
        Target.create "incapsulation" ignore
        Target.create "All" ignore

        "Aaru" <== ["begnawn"; "draffish"; "ichthyol."; "interproximate"; "relacing"]
        "Aldermaston" <== ["Vietcong"; "exasperating"; "glorified"; "mediatise"; "muensters"; "ungeuntarium"]
        "Andrej" <== ["Kovrov"; "beesting"; "caranda"; "cometography"; "exasperating"; "mediatise"; "ungeuntarium"]
        "Arthrobacter" <== ["Heteroousian"; "Latium"; "Vietcong"; "begnawn"; "ichthyol."; "interproximate"; "jaspis"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"]
        "Bersil" <== ["Vietcong"; "beesting"; "begnawn"; "ichthyol."; "interproximate"; "pettings"; "ungeuntarium"]
        "Billye" <== ["exasperating"; "mediatise"; "pro-Elizabethan"; "weaving"]
        "Callipus" <== ["Latium"; "exasperating"; "glorified"; "horsepower-year"; "mediatise"; "muensters"; "ungeuntarium"]
        "Chaetopoda" <== ["Latium"; "comparableness"; "exasperating"; "matriarches"; "mediatise"]
        "Danelage" <== ["Lookeba"; "begnawn"; "relacing"]
        "Esra" <== ["Latium"; "Vietcong"; "comparableness"; "exasperating"; "glorified"; "mediatise"; "muensters"; "ungeuntarium"]
        "FAR" <== ["Latium"; "exasperating"; "horsepower-year"; "mnemonicalist"; "senachie"; "ungeuntarium"]
        "Georgi" <== ["Latium"; "Vietcong"; "comparableness"; "compresent"; "exasperating"; "glorified"; "horsepower-year"; "hypozoic"; "mediatise"; "muensters"; "shillings"; "ungeuntarium"]
        "Gohila" <== ["Latium"; "Vietcong"; "beesting"; "begnawn"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "mnemonicalist"; "pickler"; "relacing"; "ungeuntarium"; "uninnocent"; "vined"]
        "Heptanesian" <== ["clingstones"; "exasperating"; "mediatise"; "ungeuntarium"]
        "Heteroousian" <== ["begnawn"; "comparableness"; "exasperating"; "mediatise"; "mediatise"; "relacing"]
        "ITT" <== ["exasperating"; "mnemonicalist"; "quincunx"]
        "Jenkinson" <== ["Latium"; "exasperating"; "mediatise"; "pro-Elizabethan"]
        "Kovrov" <== ["beesting"; "cometography"; "exasperating"; "mediatise"; "mesioincisal"; "ungeuntarium"]
        "Latium" <== ["Vietcong"; "beshouts"; "cometography"; "comparableness"; "exasperating"; "hypercholesterolemia"; "mediatise"; "ungeuntarium"; "ungeuntarium"; "weaving"]
        "Lookeba" <== ["Vietcong"; "aguamas"; "begnawn"; "cometography"; "demasculinize"; "ichthyol."; "interproximate"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"; "wen"]
        "Luigino" <== ["Latium"; "Vietcong"; "adherer"; "cometography"; "exasperating"; "glorified"; "glorified"; "horsepower-year"; "hypozoic"; "mediatise"; "muensters"; "ungeuntarium"]
        "Maxa" <== ["Arthrobacter"; "begnawn"; "ichthyol."; "interproximate"; "jaspis"; "mediatise"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"]
        "Merrie" <== ["Latium"; "exasperating"; "glorified"; "matriarches"; "mediatise"]
        "Mulvihill" <== ["Lookeba"; "Phascum"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "ichthyol."; "interproximate"; "pro-Elizabethan"; "relacing"; "ungeuntarium"]
        "Nicktown" <== ["begnawn"; "conflagrating"]
        "Nixa" <== ["Latium"; "Vietcong"; "exasperating"; "horsepower-year"; "matriarches"; "mediatise"; "micrometeorite"; "senachie"; "ungeuntarium"]
        "Pannini" <== ["Heteroousian"; "Heteroousian"; "begnawn"; "interproximate"; "jaspis"; "relacing"; "relacing"]
        "Peosta" <== ["Lookeba"; "Mulvihill"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "interproximate"; "outdoorness"; "rain-dropping"; "relacing"; "ungeuntarium"]
        "Phascum" <== ["Arthrobacter"; "beesting"; "begnawn"; "exasperating"; "ichthyol."; "interproximate"; "matriarches"; "mediatise"; "outargued"; "relacing"; "surrendry"; "ungeuntarium"]
        "Pro-kansan" <== ["Arthrobacter"; "Latium"; "Vietcong"; "begnawn"; "hepatectomies"; "hypercholesterolemia"; "ichthyol."; "jaspis"; "relacing"; "ungeuntarium"]
        "Quezaltenango" <== ["Latium"; "beesting"; "compresent"; "exasperating"; "glorified"; "horsepower-year"; "marlinespike"; "mediatise"; "ungeuntarium"; "weaving"]
        "Saone" <== ["exasperating"]
        "Senora" <== ["Latium"; "comparableness"; "exasperating"; "glorified"; "horsepower-year"; "mediatise"; "muensters"; "senachie"; "ungeuntarium"]
        "Shirlie" <== ["Latium"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "glorified"; "horsepower-year"; "interproximate"; "jaspis"; "leered"; "mnemonicalist"; "muensters"; "nonlicentiousness"; "ornithologic"; "purpurean"; "snubs"; "surrendry"; "ungeuntarium"]
        "Sims" <== ["Latium"; "Luigino"; "Vietcong"; "druggeries"; "glorified"; "interproximate"; "ungeuntarium"]
        "Stalk" <== ["Latium"; "Phascum"; "Vietcong"; "adherer"; "beesting"; "begnawn"; "cometography"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "leered"; "mnemonicalist"; "muensters"; "outdoorness"; "poddish"; "quicksilvers"; "relacing"; "semi-aridity"; "ungeuntarium"]
        "Takhaar" <== ["interproximate"]
        "Vietcong" <== ["exasperating"; "mediatise"]
        "adenocarcinomas" <== ["Latium"; "Vietcong"; "glorified"; "horsepower-year"; "hypozoic"; "ichthyol."; "mnemonicalist"; "quicksilvers"; "ungeuntarium"; "valvelets"]
        "adherer" <== ["Latium"; "Vietcong"; "comparableness"; "exasperating"; "glorified"; "horsepower-year"; "mediatise"; "ungeuntarium"]
        "allegresse" <== ["Latium"; "Vietcong"; "exasperating"; "horsepower-year"; "mediatise"; "mnemonicalist"; "ungeuntarium"]
        "amorphi" <== ["begnawn"; "weaving"; "weaving"]
        "apickpack" <== ["Arthrobacter"; "Heteroousian"; "Latium"; "Lookeba"; "Phascum"; "Vietcong"; "aguamas"; "beesting"; "begnawn"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "matriarches"; "milage"; "mnemonicalist"; "muensters"; "relacing"; "rubine"; "surrendry"; "unfix"; "ungeuntarium"; "valvelets"; "wen"]
        "appearer" <== ["Latium"; "Lookeba"; "Luigino"; "Vietcong"; "begnawn"; "draffish"; "ever-strong"; "glorified"; "horsepower-year"; "hypercholesterolemia"; "ichthyol."; "interproximate"; "leered"; "mnemonicalist"; "muensters"; "papyrographic"; "relacing"; "surrendry"; "ungeuntarium"; "valvelets"]
        "archeal" <== ["Latium"; "Vietcong"; "begnawn"; "glorified"; "horsepower-year"; "interproximate"; "jaspis"; "muensters"; "relacing"; "ungeuntarium"]
        "arthropodan" <== ["Latium"; "Vietcong"; "exasperating"; "horsepower-year"; "mediatise"; "mnemonicalist"; "ungeuntarium"]
        "baratheas" <== ["Latium"; "Stalk"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "glorified"; "illiberalized"; "interproximate"; "leered"; "muensters"; "outdoorness"; "poddish"; "purpurean"; "snubs"; "ungeuntarium"]
        "baronetizing" <== ["Vietcong"; "begnawn"; "cometography"; "hepatectomies"; "interproximate"; "relacing"; "ungeuntarium"]
        "beesting" <== ["Lookeba"; "Vietcong"; "cometography"; "comparableness"; "draffish"; "exasperating"; "horsepower-year"; "marlinespike"; "mediatise"; "pro-Elizabethan"; "reinsertions"; "reinsertions"; "ungeuntarium"]
        "begnawn" <== ["Saone"; "exasperating"]
        "beshouts" <== ["exasperating"; "mediatise"]
        "biocatalytic" <== ["Latium"; "beesting"; "cometography"; "exasperating"; "marlinespike"; "mediatise"; "ungeuntarium"]
        "brauna" <== ["Saone"; "exasperating"]
        "breadroot" <== ["Arthrobacter"; "Vietcong"; "begnawn"; "druggeries"; "ichthyol."; "interproximate"; "jaspis"; "relacing"; "surrendry"; "ungeuntarium"]
        "caranda" <== ["beesting"; "cometography"; "exasperating"; "marlinespike"; "marlinespike"; "mediatise"; "ungeuntarium"]
        "carp-" <== ["Latium"; "Vietcong"; "exasperating"; "mediatise"; "ungeuntarium"]
        "catacoustics" <== ["exasperating"; "hastily"; "mediatise"; "mnemonicalist"; "muensters"; "papyrographic"]
        "clingstones" <== ["comparableness"; "exasperating"; "mediatise"; "ungeuntarium"]
        "coadjute" <== ["Latium"; "Vietcong"; "exasperating"; "mediatise"; "mnemonicalist"; "ungeuntarium"]
        "codline" <== ["Latium"; "Vietcong"; "begnawn"; "glorified"; "hydrosulphocyanic"; "interproximate"; "leered"; "muensters"; "pickler"; "relacing"; "ungeuntarium"]
        "collection" <== ["exasperating"; "glorified"; "mediatise"; "muensters"; "shillings"; "ungeuntarium"]
        "comparableness" <== ["exasperating"; "mediatise"; "peptidoglycan"]
        "compresent" <== ["Latium"; "Vietcong"; "comparableness"; "exasperating"; "mediatise"; "peptidoglycan"; "ungeuntarium"]
        "consonantalize" <== ["Phascum"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "ichthyol."; "interproximate"; "outargued"; "relacing"; "surrendry"; "ungeuntarium"]
        "conspiratorially" <== ["begnawn"; "druggeries"; "hepatectomies"; "interproximate"; "relacing"]
        "contin" <== ["exasperating"; "mnemonicalist"; "ungeuntarium"]
        "contrivement" <== ["Vietcong"; "exasperating"; "mediatise"; "mnemonicalist"; "ungeuntarium"]
        "cowshed" <== ["Vietcong"; "beesting"; "begnawn"; "cometography"; "exasperating"; "interproximate"; "mediatise"; "rain-dropping"; "relacing"; "ungeuntarium"]
        "cubitoplantar" <== ["Vietcong"; "beesting"; "exasperating"; "glorified"; "hypozoic"; "mediatise"; "muensters"; "ungeuntarium"; "ungeuntarium"]
        "demasculinize" <== ["aguamas"; "wen"]
        "diphen-" <== ["Latium"; "Luigino"; "Mulvihill"; "Mulvihill"; "Vietcong"; "beesting"; "cometography"; "compresent"; "exasperating"; "glorified"; "horsepower-year"; "hypozoic"; "matriarches"; "mediatise"; "muensters"; "soundless"; "ungeuntarium"]
        "draffish" <== ["Lookeba"; "Vietcong"; "begnawn"; "cometography"; "ichthyol."; "interproximate"; "relacing"; "rubine"; "ungeuntarium"]
        "druggeries" <== ["begnawn"; "ichthyol."; "interproximate"; "relacing"]
        "earth-nut" <== ["exasperating"; "lords-in-waiting"; "mediatise"]
        "eelpot" <== ["begnawn"; "cometography"; "relacing"]
        "empty-looking" <== ["Arthrobacter"; "Heteroousian"; "Latium"; "Vietcong"; "begnawn"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "mnemonicalist"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"]
        "enormities" <== ["Arthrobacter"; "Latium"; "Lookeba"; "Mulvihill"; "Phascum"; "Vietcong"; "aguamas"; "beesting"; "begnawn"; "demasculinize"; "glorified"; "horsepower-year"; "hypercholesterolemia"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "mnemonicalist"; "muensters"; "pickler"; "relacing"; "surrendry"; "trunkback"; "ungeuntarium"; "valvelets"; "wen"]
        "erichtoid" <== ["Latium"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "glorified"; "interproximate"; "leered"; "muensters"; "poddish"; "purpurean"; "snubs"; "ungeuntarium"]
        "ever-strong" <== ["Arthrobacter"; "Bersil"; "Latium"; "Lookeba"; "Phascum"; "Vietcong"; "beesting"; "begnawn"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "jaspis"; "mnemonicalist"; "muensters"; "pettings"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"; "valvelets"]
        "exasperating" <== ["cometography"; "rhyacolite"]
        "explorator" <== ["Arthrobacter"; "Vietcong"; "begnawn"; "ichthyol."; "mediatise"; "relacing"; "surrendry"; "ungeuntarium"; "valvelets"]
        "fleecer" <== ["Latium"; "exasperating"; "mnemonicalist"; "ungeuntarium"]
        "flywinch" <== ["Heteroousian"; "Latium"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "glorified"; "horsepower-year"; "interproximate"; "leered"; "mnemonicalist"; "muensters"; "ornithologic"; "snubs"; "ungeuntarium"]
        "formats" <== ["Latium"; "Phascum"; "Stalk"; "Vietcong"; "adherer"; "beesting"; "begnawn"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "leered"; "mnemonicalist"; "muensters"; "outdoorness"; "quicksilvers"; "relacing"; "semi-aridity"; "ungeuntarium"]
        "formicarian" <== ["exasperating"; "mediatise"; "mnemonicalist"; "muensters"; "papyrographic"; "shillings"]
        "glorified" <== ["Latium"; "Vietcong"; "cometography"; "cometography"; "comparableness"; "exasperating"; "horsepower-year"; "matriarches"; "mediatise"; "pro-Elizabethan"; "reinsertions"; "senachie"; "ungeuntarium"]
        "grovy" <== ["Georgi"; "exasperating"; "glorified"; "hastily"; "mediatise"; "mnemonicalist"; "muensters"; "shillings"; "soundless"; "ungeuntarium"]
        "hagioscopic" <== ["exasperating"]
        "hastily" <== ["Georgi"; "Vietcong"; "collection"; "exasperating"; "glorified"; "mediatise"; "muensters"; "quincunx"; "shillings"]
        "hepatectomies" <== ["begnawn"; "exasperating"; "exasperating"; "relacing"]
        "hollowroot" <== ["exasperating"]
        "horsepower-year" <== ["Latium"; "Vietcong"; "exasperating"; "matriarches"; "mediatise"; "mnemonicalist"; "ungeuntarium"; "viscerate"]
        "hydrachnid" <== ["synactic"]
        "hydrolatry" <== ["Latium"; "Saone"; "Stalk"; "Vietcong"; "beesting"; "begnawn"; "compresent"; "druggeries"; "glorified"; "interproximate"; "leered"; "mnemonicalist"; "muensters"; "outdoorness"; "papyrographic"; "poddish"; "purpurean"; "snubs"; "ungeuntarium"]
        "hydrosulphocyanic" <== ["Latium"; "Luigino"; "Vietcong"; "adherer"; "begnawn"; "glorified"; "horsepower-year"; "interproximate"; "leered"; "mnemonicalist"; "muensters"; "quicksilvers"; "relacing"; "surrendry"; "ungeuntarium"]
        "hyeniform" <== ["Latium"; "Vietcong"; "exasperating"; "mnemonicalist"; "ungeuntarium"]
        "hypercholesterolemia" <== ["Vietcong"; "begnawn"; "cometography"; "interproximate"; "relacing"; "surrendry"; "ungeuntarium"]
        "hypermoralistic" <== ["clingstones"; "exasperating"; "mediatise"; "ungeuntarium"]
        "hyperpersonal" <== ["Lookeba"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "draffish"; "ichthyol."; "interproximate"; "relacing"; "surrendry"; "ungeuntarium"]
        "hypozoic" <== ["Latium"; "exasperating"; "glorified"; "muensters"; "ungeuntarium"]
        "ichthyol." <== ["Vietcong"; "begnawn"; "cometography"; "interproximate"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"]
        "illiberalized" <== ["Phascum"; "Stalk"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "formats"; "ichthyol."; "interproximate"; "outdoorness"; "relacing"; "semi-aridity"; "ungeuntarium"]
        "incapsulation" <== ["Latium"; "Stalk"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "formats"; "glorified"; "hydrolatry"; "interproximate"; "leered"; "muensters"; "outdoorness"; "purpurean"; "quicksilvers"; "snubs"; "ungeuntarium"]
        "injected" <== ["Latium"; "Vietcong"; "amorphi"; "amorphi"; "begnawn"; "glorified"; "interproximate"; "leered"; "relacing"; "surrendry"; "trunkback"; "ungeuntarium"]
        "interproximate" <== ["Vietcong"; "begnawn"; "cometography"; "hepatectomies"; "relacing"; "semi-aridity"; "surrendry"; "ungeuntarium"]
        "isologues" <== ["Vietcong"; "comparableness"; "exasperating"; "mediatise"]
        "jaspis" <== ["Heteroousian"; "Latium"; "Vietcong"; "begnawn"; "hepatectomies"; "ichthyol."; "interproximate"; "matriarches"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"]
        "kaw-" <== ["Arthrobacter"; "Latium"; "Vietcong"; "begnawn"; "druggeries"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "mnemonicalist"; "muensters"; "surrendry"; "ungeuntarium"; "valvelets"]
        "komatiks" <== ["comparableness"; "exasperating"; "mediatise"]
        "leered" <== ["Latium"; "Vietcong"; "amorphi"; "beesting"; "begnawn"; "cometography"; "glorified"; "hepatectomies"; "horsepower-year"; "interproximate"; "jaspis"; "mnemonicalist"; "muensters"; "relacing"; "surrendry"; "ungeuntarium"]
        "litotic" <== ["exasperating"; "mediatise"; "mnemonicalist"; "papyrographic"]
        "lords-in-waiting" <== ["Latium"; "Vietcong"; "compresent"; "exasperating"; "mediatise"; "peptidoglycan"; "ungeuntarium"]
        "marlinespike" <== ["Danelage"; "Lookeba"; "exasperating"; "mediatise"; "ungeuntarium"]
        "matriarches" <== ["Latium"; "Vietcong"; "comparableness"; "comparableness"; "compresent"; "exasperating"; "lords-in-waiting"; "mediatise"; "ungeuntarium"]
        "mediatise" <== ["brauna"; "exasperating"]
        "mesioincisal" <== ["Latium"; "Vietcong"; "beesting"; "clingstones"; "cometography"; "comparableness"; "compresent"; "earth-nut"; "exasperating"; "jaspis"; "lords-in-waiting"; "marlinespike"; "matriarches"; "mediatise"; "ungeuntarium"]
        "micrometeorite" <== ["Latium"; "exasperating"; "horsepower-year"; "mediatise"; "senachie"; "ungeuntarium"]
        "milage" <== ["Latium"; "beesting"; "cometography"; "exasperating"; "glorified"; "marlinespike"; "mediatise"; "muensters"; "ungeuntarium"]
        "mischio" <== ["Latium"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "glorified"; "horsepower-year"; "interproximate"; "leered"; "mesioincisal"; "mnemonicalist"; "purpurean"; "snubs"; "surrendry"; "ungeuntarium"]
        "mnemonicalist" <== ["Latium"; "Latium"; "exasperating"; "exasperating"; "ungeuntarium"]
        "mobilizable" <== ["Phascum"; "Vietcong"; "beesting"; "begnawn"; "ichthyol."; "pickler"; "relacing"; "ungeuntarium"; "yawnproof"]
        "muensters" <== ["Latium"; "Vietcong"; "compresent"; "exasperating"; "glorified"; "horsepower-year"; "matriarches"; "mediatise"; "ungeuntarium"]
        "nitta" <== ["Latium"; "Lookeba"; "Peosta"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "druggeries"; "glorified"; "interproximate"; "leered"; "outdoorness"; "purpurean"; "snubs"; "surrendry"; "ungeuntarium"]
        "noninheritability" <== ["Latium"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "glorified"; "horsepower-year"; "interproximate"; "jaspis"; "leered"; "mnemonicalist"; "muensters"; "purpurean"; "quicksilvers"; "snubs"; "surrendry"; "ungeuntarium"]
        "nonlicentiousness" <== ["Latium"; "Quezaltenango"; "Vietcong"; "beesting"; "compresent"; "glorified"; "ungeuntarium"]
        "nonresponsible" <== ["Saone"; "begnawn"]
        "nonsubjectiveness" <== ["begnawn"]
        "numbnesses" <== ["begnawn"; "ichthyol."; "interproximate"; "relacing"; "surrendry"; "ungeuntarium"]
        "ondogram" <== ["Latium"; "exasperating"; "glorified"; "horsepower-year"; "mediatise"; "ungeuntarium"]
        "orchiocele" <== ["beesting"; "cometography"; "exasperating"; "marlinespike"; "mediatise"]
        "ornithologic" <== ["Latium"; "Quezaltenango"; "Vietcong"; "beesting"; "begnawn"; "glorified"; "nonlicentiousness"; "relacing"; "ungeuntarium"]
        "outargued" <== ["Vietcong"; "beesting"; "begnawn"; "interproximate"; "mediatise"; "relacing"; "ungeuntarium"]
        "outdoorness" <== ["Latium"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "glorified"; "ichthyol."; "interproximate"; "leered"; "semi-aridity"; "trunkback"; "ungeuntarium"]
        "outgreen" <== ["Latium"; "Phascum"; "Vietcong"; "beesting"; "begnawn"; "druggeries"; "ichthyol."; "interproximate"; "matriarches"; "snubs"; "ungeuntarium"]
        "overanalyze" <== ["comparableness"; "exasperating"; "mediatise"; "uninnocent"]
        "paleologist" <== ["Vietcong"; "begnawn"; "ichthyol."; "interproximate"; "jaspis"; "pro-Elizabethan"; "relacing"; "ungeuntarium"]
        "panmixias" <== ["Latium"; "Vietcong"; "exasperating"; "mediatise"]
        "papyrographic" <== ["Latium"; "Vietcong"; "exasperating"; "mediatise"; "mnemonicalist"; "ungeuntarium"]
        "parlante" <== ["Arthrobacter"; "Lookeba"; "begnawn"; "draffish"; "ichthyol."; "jaspis"; "relacing"; "rubine"]
        "peptidoglycan" <== ["exasperating"; "exasperating"]
        "petro-" <== ["Andrej"; "Billye"; "Callipus"; "Chaetopoda"; "FAR"; "ITT"; "Jenkinson"; "Kovrov"; "Merrie"; "Nixa"; "Senora"; "caranda"; "catacoustics"; "coadjute"; "cometography"; "contin"; "exasperating"; "fleecer"; "formicarian"; "grovy"; "hagioscopic"; "hyeniform"; "isologues"; "litotic"; "mediatise"; "mnemonicalist"; "muensters"; "orchiocele"; "overanalyze"; "papyrographic"; "refreshener"; "sclerodactylia"; "splurgiest"; "subtetanic"; "swiftest"; "thoracoabdominal"; "uninnocent"; "unmarvellously"; "unquestioningly"; "vined"; "withgate"]
        "pettings" <== ["Arthrobacter"; "Heteroousian"; "Latium"; "Phascum"; "Vietcong"; "beesting"; "begnawn"; "begnawn"; "cometography"; "glorified"; "horsepower-year"; "hypercholesterolemia"; "ichthyol."; "ichthyol."; "interproximate"; "interproximate"; "jaspis"; "jaspis"; "mnemonicalist"; "muensters"; "paleologist"; "relacing"; "relacing"; "rubine"; "rubine"; "surrendry"; "ungeuntarium"]
        "pickler" <== ["Arthrobacter"; "Latium"; "Phascum"; "Vietcong"; "beesting"; "begnawn"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "mnemonicalist"; "muensters"; "relacing"; "relacing"; "surrendry"; "ungeuntarium"; "uninnocent"]
        "piperidine" <== ["Latium"; "Vietcong"; "begnawn"; "begnawn"; "cometography"; "cometography"; "compresent"; "glorified"; "hepatectomies"; "horsepower-year"; "ichthyol."; "interproximate"; "interproximate"; "mnemonicalist"; "muensters"; "quicksilvers"; "relacing"; "relacing"; "rubine"; "surrendry"; "surrendry"; "ungeuntarium"]
        "piques" <== ["begnawn"; "interproximate"; "nonsubjectiveness"; "relacing"]
        "poddish" <== ["Latium"; "Vietcong"; "adherer"; "beesting"; "begnawn"; "glorified"; "horsepower-year"; "hypercholesterolemia"; "ichthyol."; "interproximate"; "leered"; "mnemonicalist"; "muensters"; "quicksilvers"; "relacing"; "ungeuntarium"]
        "potto" <== ["Phascum"; "begnawn"; "ichthyol."; "interproximate"; "leered"; "relacing"]
        "prefearful" <== ["comparableness"; "exasperating"; "komatiks"; "ungeuntarium"]
        "pro-Elizabethan" <== ["Vietcong"; "begnawn"; "hepatectomies"; "ichthyol."; "interproximate"; "relacing"; "ungeuntarium"]
        "psychologist's" <== ["Arthrobacter"; "Vietcong"; "begnawn"; "ichthyol."; "mediatise"; "relacing"; "surrendry"; "valvelets"]
        "purpurean" <== ["Arthrobacter"; "Arthrobacter"; "Heteroousian"; "Latium"; "Lookeba"; "Mulvihill"; "Phascum"; "Quezaltenango"; "Vietcong"; "archeal"; "beesting"; "begnawn"; "cometography"; "ever-strong"; "glorified"; "hepatectomies"; "horsepower-year"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "leered"; "muensters"; "outargued"; "pro-Elizabethan"; "relacing"; "rubine"; "self-gratulation"; "surrendry"; "unfix"; "ungeuntarium"; "valvelets"]
        "quicksilvers" <== ["Arthrobacter"; "Heteroousian"; "Latium"; "Lookeba"; "Phascum"; "Saone"; "Vietcong"; "adherer"; "aguamas"; "apickpack"; "beesting"; "begnawn"; "cometography"; "ever-strong"; "exasperating"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "mediatise"; "mnemonicalist"; "muensters"; "papyrographic"; "relacing"; "surrendry"; "ungeuntarium"; "valvelets"; "wen"]
        "quincunx" <== ["Vietcong"; "exasperating"; "mediatise"]
        "rain-dropping" <== ["beesting"; "cometography"; "exasperating"; "mediatise"]
        "rangelands" <== ["cometography"]
        "refreshener" <== ["Aldermaston"; "Esra"; "Georgi"; "Luigino"; "allegresse"; "arthropodan"; "collection"; "cubitoplantar"; "exasperating"; "glorified"; "hastily"; "mediatise"; "panmixias"; "shillings"; "soundless"; "terroristic"; "ungeuntarium"]
        "relacing" <== ["Vietcong"; "begnawn"; "exasperating"; "mediatise"]
        "rough-hackled" <== ["begnawn"; "interproximate"; "jaspis"; "nonsubjectiveness"; "piques"; "relacing"]
        "rubine" <== ["begnawn"; "interproximate"; "relacing"]
        "sclerodactylia" <== ["comparableness"; "compresent"; "exasperating"; "mediatise"]
        "self-gratulation" <== ["Vietcong"; "begnawn"; "ungeuntarium"]
        "senachie" <== ["Latium"; "Vietcong"; "beshouts"; "comparableness"; "exasperating"; "horsepower-year"; "matriarches"; "mediatise"; "mnemonicalist"; "papyrographic"; "prefearful"; "ungeuntarium"]
        "shillings" <== ["Latium"; "exasperating"; "glorified"; "horsepower-year"; "hypozoic"; "mediatise"; "muensters"; "ungeuntarium"]
        "snubs" <== ["Latium"; "Lookeba"; "Mulvihill"; "Phascum"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "cubitoplantar"; "druggeries"; "glorified"; "horsepower-year"; "interproximate"; "mesioincisal"; "mnemonicalist"; "muensters"; "papyrographic"; "ungeuntarium"]
        "soundless" <== ["Latium"; "Vietcong"; "comparableness"; "exasperating"; "glorified"; "horsepower-year"; "hypozoic"; "lords-in-waiting"; "matriarches"; "mediatise"; "muensters"; "shillings"; "ungeuntarium"]
        "splurgiest" <== ["exasperating"; "mediatise"]
        "subarytenoidal" <== ["Vietcong"; "beesting"; "biocatalytic"; "druggeries"; "ever-strong"; "ichthyol."; "snubs"; "ungeuntarium"]
        "subtetanic" <== ["Georgi"; "exasperating"; "mediatise"; "mnemonicalist"]
        "surrendry" <== ["Vietcong"; "begnawn"; "cometography"; "relacing"; "ungeuntarium"]
        "swiftest" <== ["Latium"; "Vietcong"; "beshouts"; "exasperating"; "glorified"; "horsepower-year"; "mediatise"; "muensters"; "ungeuntarium"]
        "synactic" <== ["begnawn"; "conflagrating"; "interproximate"]
        "terroristic" <== ["Kovrov"; "Vietcong"; "beesting"; "cometography"; "diphen-"; "exasperating"; "mediatise"; "mesioincisal"]
        "thoracoabdominal" <== ["collection"; "exasperating"; "mediatise"; "papyrographic"]
        "trunkback" <== ["Latium"; "Vietcong"; "begnawn"; "glorified"; "horsepower-year"; "interproximate"; "leered"; "mnemonicalist"; "ungeuntarium"]
        "unfix" <== ["Arthrobacter"; "Heteroousian"; "Latium"; "Vietcong"; "beesting"; "begnawn"; "cometography"; "ever-strong"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "mediatise"; "mnemonicalist"; "muensters"; "relacing"; "rubine"; "ungeuntarium"; "valvelets"]
        "ungeuntarium" <== ["Vietcong"; "cometography"; "cometography"; "exasperating"; "mediatise"]
        "uninnocent" <== ["Latium"; "Vietcong"; "beesting"; "cometography"; "comparableness"; "exasperating"; "glorified"; "horsepower-year"; "hypozoic"; "jaspis"; "marlinespike"; "matriarches"; "mediatise"; "muensters"; "numbnesses"; "soundless"; "ungeuntarium"]
        "unmarvellously" <== ["exasperating"; "mediatise"]
        "unquestioningly" <== ["comparableness"; "exasperating"; "mediatise"; "peptidoglycan"]
        "unslashed" <== ["Takhaar"; "Vietcong"; "begnawn"; "druggeries"; "exasperating"; "interproximate"; "ungeuntarium"]
        "valvelets" <== ["Arthrobacter"; "Latium"; "Lookeba"; "Luigino"; "Phascum"; "Saone"; "Vietcong"; "beesting"; "begnawn"; "glorified"; "horsepower-year"; "hypercholesterolemia"; "ichthyol."; "interproximate"; "jaspis"; "leered"; "muensters"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"]
        "verruculose" <== ["Vietcong"; "begnawn"; "druggeries"; "glorified"; "interproximate"; "muensters"; "trunkback"; "ungeuntarium"]
        "vined" <== ["Latium"; "Vietcong"; "beesting"; "cometography"; "comparableness"; "exasperating"; "glorified"; "horsepower-year"; "hypozoic"; "mediatise"; "mnemonicalist"; "muensters"; "ungeuntarium"; "uninnocent"]
        "viscerate" <== ["exasperating"; "mediatise"]
        "voter" <== ["begnawn"; "ichthyol."; "interproximate"; "outdoorness"; "ungeuntarium"]
        "weaving" <== ["exasperating"]
        "wen" <== ["aguamas"]
        "white-bosomed" <== ["Vietcong"; "begnawn"; "druggeries"; "hypercholesterolemia"; "interproximate"; "ungeuntarium"]
        "withgate" <== ["Bersil"; "Lookeba"; "Mulvihill"; "Phascum"; "Quezaltenango"; "Quezaltenango"; "beesting"; "begnawn"; "caranda"; "cometography"; "empty-looking"; "exasperating"; "ichthyol."; "interproximate"; "marlinespike"; "mediatise"; "paleologist"; "ungeuntarium"; "voter"]
        "yawnproof" <== ["Arthrobacter"; "Latium"; "Phascum"; "Pro-kansan"; "Vietcong"; "beesting"; "begnawn"; "begnawn"; "glorified"; "horsepower-year"; "ichthyol."; "interproximate"; "interproximate"; "jaspis"; "leered"; "mesioincisal"; "mnemonicalist"; "muensters"; "pickler"; "relacing"; "rubine"; "surrendry"; "ungeuntarium"; "uninnocent"]

        // Add list of targets that nothing depends on
        "All" <== ["Aaru"; "Gohila"; "Heptanesian"; "Maxa"; "Nicktown"; "Pannini"; "Shirlie"; "Sims"; "adenocarcinomas"; "appearer"; "baratheas"; "baronetizing"; "breadroot"; "carp-"; "codline"; "consonantalize"; "conspiratorially"; "contrivement"; "cowshed"; "eelpot"; "enormities"; "erichtoid"; "explorator"; "flywinch"; "hollowroot"; "hydrachnid"; "hypermoralistic"; "hyperpersonal"; "incapsulation"; "injected"; "kaw-"; "mischio"; "mobilizable"; "nitta"; "noninheritability"; "nonresponsible"; "ondogram"; "outgreen"; "parlante"; "petro-"; "piperidine"; "potto"; "psychologist's"; "rangelands"; "rough-hackled"; "subarytenoidal"; "unslashed"; "verruculose"; "white-bosomed"]

        let sw = System.Diagnostics.Stopwatch()
        sw.Start()

        let order = determineBuildOrder "All" 2
        validateBuildOrder order "All"

        let elapsedSeconds = sw.Elapsed.Seconds

        // This takes a second on my machine. 10 seconds allowed for a buffer
        if elapsedSeconds > 10
        then failwithf "Unexpectedly long time to complete (%d seconds); should only take a second" elapsedSeconds

        match order with
            | [
                TargetSet [ "aguamas"; "cometography"; "conflagrating"; "reinsertions"; "rhyacolite"; "semi-aridity" ]
                TargetSet [ "exasperating"; "rangelands"; "wen" ]
                TargetSet [ "Saone"; "demasculinize"; "hagioscopic"; "hollowroot"; "peptidoglycan"; "weaving" ]
                TargetSet [ "begnawn"; "brauna" ]
                TargetSet [ "Nicktown"; "amorphi"; "mediatise"; "nonresponsible"; "nonsubjectiveness" ]
                TargetSet [ "Vietcong"; "beshouts"; "comparableness"; "splurgiest"; "unmarvellously"; "viscerate" ]
                TargetSet [ "isologues"; "komatiks"; "quincunx"; "relacing"; "ungeuntarium"; "unquestioningly" ]
                TargetSet [ "Heteroousian"; "clingstones"; "eelpot"; "hepatectomies"; "prefearful"; "self-gratulation"; "surrendry" ]
                TargetSet [ "Heptanesian"; "hypermoralistic"; "interproximate" ]
                TargetSet [ "Takhaar"; "baronetizing"; "hypercholesterolemia"; "piques"; "rubine"; "synactic" ]
                TargetSet [ "Latium"; "hydrachnid"; "ichthyol." ]
                TargetSet [ "Lookeba"; "carp-"; "compresent"; "druggeries"; "mnemonicalist"; "numbnesses"; "panmixias"; "pro-Elizabethan" ]
                TargetSet [ "Billye"; "Danelage"; "ITT"; "Jenkinson"; "coadjute"; "conspiratorially"; "contin"; "contrivement"; "draffish"; "fleecer"; "hyeniform"; "lords-in-waiting"; "papyrographic"; "sclerodactylia"; "unslashed"; "white-bosomed" ]
                TargetSet [ "Aaru"; "earth-nut"; "litotic"; "marlinespike"; "matriarches" ]
                TargetSet [ "Chaetopoda"; "horsepower-year"; "jaspis" ]
                TargetSet [ "Arthrobacter"; "Pannini"; "allegresse"; "arthropodan"; "beesting"; "paleologist"; "rough-hackled"; "senachie" ]
                TargetSet [ "FAR"; "Maxa"; "Pro-kansan"; "biocatalytic"; "breadroot"; "caranda"; "glorified"; "hyperpersonal"; "mesioincisal"; "micrometeorite"; "orchiocele"; "outargued"; "parlante"; "rain-dropping" ]
                TargetSet [ "Kovrov"; "Merrie"; "Nixa"; "Phascum"; "Quezaltenango"; "adherer"; "cowshed"; "muensters"; "ondogram" ]
                TargetSet [ "Aldermaston"; "Andrej"; "Callipus"; "Esra"; "Mulvihill"; "Senora"; "archeal"; "consonantalize"; "hypozoic"; "leered"; "milage"; "nonlicentiousness"; "pettings"; "swiftest" ]
                TargetSet [ "Bersil"; "Luigino"; "cubitoplantar"; "empty-looking"; "ornithologic"; "potto"; "shillings"; "trunkback" ]
                TargetSet [ "Georgi"; "Sims"; "collection"; "formicarian"; "injected"; "outdoorness"; "snubs"; "soundless"; "valvelets"; "verruculose" ]
                TargetSet [ "Peosta"; "diphen-"; "ever-strong"; "explorator"; "flywinch"; "hastily"; "kaw-"; "outgreen"; "psychologist's"; "subtetanic"; "thoracoabdominal"; "uninnocent"; "voter" ]
                TargetSet [ "appearer"; "catacoustics"; "grovy"; "overanalyze"; "pickler"; "subarytenoidal"; "terroristic"; "unfix"; "vined"; "withgate" ]
                TargetSet [ "Gohila"; "apickpack"; "enormities"; "purpurean"; "refreshener"; "yawnproof" ]
                TargetSet [ "Shirlie"; "mischio"; "mobilizable"; "nitta"; "petro-"; "quicksilvers" ]
                TargetSet [ "adenocarcinomas"; "hydrosulphocyanic"; "noninheritability"; "piperidine"; "poddish" ]
                TargetSet [ "Stalk"; "codline"; "erichtoid" ]
                TargetSet [ "formats"; "hydrolatry" ]
                TargetSet [ "illiberalized"; "incapsulation" ]
                TargetSet [ "baratheas" ]
                TargetSet [ "All" ]
               ] ->
                // as expected
                ()

            | _ ->
                failwithf "unexpected order: %A" (order |> List.map (Array.map (fun t -> t.Name)))
  ]
  @ (
    [     
    testCaseMultipleRuns "Not activated final target does not run" <| fun myRun _ ->
      Target.create "a" ignore
      Target.create "b" ignore
      Target.create "c" ignore
      "a" ==> "b" ==> "c" |> ignore
      let mutable finalTargetResult = 0
      Target.createFinal "Final" (fun _ -> finalTargetResult <- 1)
            
      let context = myRun "c"
      let actualOrder = 
        context.PreviousTargets
        |> List.map (fun tr -> tr.Target.Name)
      let expectedOrder = ["a";"b";"c"]    
      Expect.equal 3 context.PreviousTargets.Length "Expected context to contain 3 targets"
      Expect.equal expectedOrder actualOrder "Expected context to contain 3 targets in right order"
      Expect.equal 0 finalTargetResult "Expected final target to not run"

    testCaseMultipleRuns "Final targets run after all targets" <| fun myRun _ ->
      Target.create "a" DoNothing
      Target.create "b" DoNothing
      Target.create "c" DoNothing
      "a" ==> "b" ==> "c" |> ignore
      let mutable finalTargetResult = 0
      Target.createFinal "Final" (fun _ -> finalTargetResult <- 1)
      Target.createFinal "Final2" (fun _ -> finalTargetResult <- finalTargetResult+1)
      Target.activateFinal "Final"
      Target.activateFinal "Final2"
      let context = myRun "c"
      let actualOrder = 
        context.PreviousTargets
        |> List.map (fun tr -> tr.Target.Name)
      let expectedOrder = ["a";"b";"c";"Final";"Final2"]    
      Expect.equal 5 context.PreviousTargets.Length "Expected context to contain 5 targets"
      Expect.equal expectedOrder actualOrder "Expected context to contain 5 targets in right order"
      Expect.equal 2 finalTargetResult "Expected final targets to run"

    testCaseMultipleRuns "BuildFailure targets do not run if nothing fails" <| fun myRun _ ->
      Target.create "a" ignore
      Target.create "b" ignore
      Target.create "c" ignore
      "a" ==> "b" ==> "c" |> ignore
      let mutable failureTargetResult = 0
      Target.createBuildFailure "FailureTarget" (fun _ -> failureTargetResult <- 1)
      Target.activateBuildFailure "FailureTarget"     
      let context = myRun "c"
      let actualOrder = 
        context.PreviousTargets
        |> List.map (fun tr -> tr.Target.Name)
      let expectedOrder = ["a";"b";"c"]    
      Expect.equal 3 context.PreviousTargets.Length "Expected context to contain 3 targets" 
      Expect.equal expectedOrder actualOrder "Expected context to contain 3 targets in right order"
      Expect.equal 0 failureTargetResult "Expected buildFailure target to not run"

    testCaseMultipleRuns "BuildFailure targets do not run if not activated" <| fun myRun _ ->
      Target.create "a" ignore
      Target.create "b" (fun _ -> failwith "failed dependency")
      Target.create "c" ignore
      "a" ==> "b" ==> "c" |> ignore
      let mutable failureTargetResult = 0
      Target.createBuildFailure "FailureTarget" (fun _ -> failureTargetResult <- 1)
      
      let context = myRun "c"
      let actualOrder = 
        context.PreviousTargets
        |> List.map (fun tr -> tr.Target.Name)
      let expectedOrder = ["a";"b";"c"]    
      Expect.equal 3 context.PreviousTargets.Length "Expected context to contain 3 targets"
      Expect.equal expectedOrder actualOrder "Expected context to contain 3 targets in right order"
      Expect.equal 0 failureTargetResult "Expected buildFailure target to not run"

    testCaseMultipleRuns "BuildFailure targets run after failing targets" <| fun myRun _ ->
      Target.create "a" DoNothing
      Target.create "b" (fun _ -> failwith "failed dependency")
      Target.create "c" DoNothing
      "a" ==> "b" ==> "c" |> ignore
      let mutable failureTargetResult = 0
      Target.createBuildFailure "FailureTarget" (fun _ -> failureTargetResult <- 1)
      Target.createBuildFailure "FailureTarget2" (fun _ -> failureTargetResult <- failureTargetResult+1)
      Target.activateBuildFailure "FailureTarget"
      Target.activateBuildFailure "FailureTarget2"
      let context = myRun "c"
      let actualOrder = 
        context.PreviousTargets
        |> List.map (fun tr -> tr.Target.Name)
      let expectedOrder = ["a";"b";"c";"FailureTarget";"FailureTarget2"]
      let bResult = 
        context.PreviousTargets
        |> List.find (fun tr -> tr.Target.Name="b")
      let cResult = 
        context.PreviousTargets
        |> List.find (fun tr -> tr.Target.Name="c")  
      Expect.equal true context.HasError "Expected failure"  
      Expect.equal true context.HasError "Expected second target to skip after failure" 
      Expect.equal 5 context.PreviousTargets.Length "Expected context to contain 5 targets" 
      Expect.equal expectedOrder actualOrder "Expected context to contain 5 targets in right order"
      Expect.isSome bResult.Error "Expected target b to error"
      Expect.isTrue cResult.WasSkipped "Expected target c to skip"
      Expect.equal 2 failureTargetResult "Expected buildFailure targets to run"

    testCaseMultipleRuns "Final targets run after failing targets" <| fun myRun _ ->
      Target.create "a" DoNothing
      Target.create "b" (fun _ -> failwith "failed dependency")
      Target.create "c" DoNothing
      "a" ==> "b" ==> "c" |> ignore
      let mutable finalTargetResult = 0
      Target.createFinal "Final" (fun _ -> finalTargetResult <- 1)
      Target.createFinal "Final2" (fun _ -> finalTargetResult <- finalTargetResult+1)
      Target.activateFinal "Final"
      Target.activateFinal "Final2"
      let context = myRun "c"
      let actualOrder = 
        context.PreviousTargets
        |> List.map (fun tr -> tr.Target.Name)
      let expectedOrder = ["a";"b";"c";"Final";"Final2"]
      let bResult = 
        context.PreviousTargets
        |> List.find (fun tr -> tr.Target.Name="b")
      let cResult = 
        context.PreviousTargets
        |> List.find (fun tr -> tr.Target.Name="c")    
      Expect.equal true context.HasError "Expected failure"  
      Expect.equal true context.HasError "Expected second target to skip after failure"
      Expect.equal 5 context.PreviousTargets.Length "Expected context to contain 5 targets" 
      Expect.equal expectedOrder actualOrder "Expected context to contain 5 targets in right order"
      Expect.isSome bResult.Error "Expected target b to error"
      Expect.isTrue cResult.WasSkipped "Expected target c to skip"
      Expect.equal 2 finalTargetResult "Expected final targets to run"
  ] |> List.concat))
