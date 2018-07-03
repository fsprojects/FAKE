module Fake.Core.TargetTests

open Fake.Core
open Expecto

let run targetName =
    try Target.runAndGetContext 1 targetName []
    with | :? BuildFailedException as bfe ->
        match bfe.Info with
        | Some context -> context
        | None -> failwithf "No context given!"

let runParallel targetName =
    try Target.runAndGetContext 3 targetName []
    with | :? BuildFailedException as bfe ->
        match bfe.Info with
        | Some context -> context
        | None -> failwithf "No context given!"

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

[<Tests>]
let tests =
  testList "Fake.Core.Target.Tests" (
    [
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
            if not (t.Wait(1000)) then
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
