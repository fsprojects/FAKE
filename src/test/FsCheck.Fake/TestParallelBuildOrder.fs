module FsCheck.Fake.TestParallelBuildOrder

open System
open System.Collections.Generic
open global.Xunit
open Fake
open FsCheck

let inline (==>) a b = global.Fake.AdditionalSyntax.(==>) a b

let (|Target|) (t : Target) =
    Target t.Name

let (|TargetSet|) (t : seq<Target>) =
    let list = t |> Seq.map (fun t -> t.Name) |> Seq.sort |> Seq.toList
    TargetSet list

// checks whether the given order is consistent with all dependencies
let validateBuildOrder (order : list<Target[]>) (rootTarget : string) =
    let rootTarget = getTarget rootTarget

    // store a "level" for all targets in order
    let targetLevelMap = 
        order |> List.mapi (fun i arr ->
                arr |> Array.map (fun t -> t.Name, i)
              )
              |> Seq.concat
              |> Map.ofSeq

    // check whether the assigned level is smaller or equal to
    // the given max level.
    let checkLevel (target : Target) (maxLevel : int) =
        match Map.tryFind target.Name targetLevelMap with
            | Some level ->
                if level > maxLevel then
                    failwithf "found target on unexpected level %d (should be at most %d)" level maxLevel

            | None ->
                failwithf "target %A was not assigned a level but occurs in the dependency tree" target.Name
    
    // recursively validate the target levels
    let rec validate fDeps (t : Target) (maxLevel : int) =
        checkLevel t maxLevel
        let realLevel = Map.find t.Name targetLevelMap

        let deps = fDeps t|> List.map getTarget
        for d in deps do
            validate fDeps d (realLevel - 1)

    // initially the max-level is unbounded
    validate (fun t -> t.Dependencies) rootTarget Int32.MaxValue
    validate (fun t -> t.SoftDependencies) rootTarget Int32.MaxValue

[<Fact>]
let ``Independent targets are parallel``() =
    TargetDict.Clear()

    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    
    Target "dep" DoNothing

    "a" ==> "dep" |> ignore
    "b" ==> "dep" |> ignore
    "c" ==> "dep" |> ignore

    let order = determineBuildOrder "dep"

    validateBuildOrder order "dep"

    match order with
        | [TargetSet ["a"; "b"; "c"]; [|Target "dep"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "inconsitent order: %A" order


    ()
    
[<Fact>]
let ``Issue #1395 Example``() =
    TargetDict.Clear()
    Target "T1" DoNothing
    Target "T2.1" DoNothing
    Target "T2.2" DoNothing
    Target "T2.3" DoNothing
    Target "T3" DoNothing
    Target "T4" DoNothing

    // create a graph
    "T1" ==> "T2.1" ==> "T2.2" ==> "T2.3" |> ignore
    "T1" ==> "T3" |> ignore
    "T2.3" ==> "T4" |> ignore
    "T3" ==> "T4" |> ignore

    let order = determineBuildOrder "T4"
    validateBuildOrder order "T4"

    match order with
        | [[|Target "T1"|];TargetSet ["T2.1"; "T3"];[|Target "T2.2"|];[|Target "T2.3"|];[|Target "T4"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``Diamonds are resolved correctly``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    Target "d" DoNothing

    // create graph
    "a" ==> "b" ==> "d" |> ignore
    "a" ==> "c" ==> "d" |> ignore

    let order = determineBuildOrder "d"
    validateBuildOrder order "d"

    match order with
        | [[|Target "a"|];TargetSet ["b"; "c"];[|Target "d"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``Initial Targets Can Run Concurrently``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c1" DoNothing
    Target "c2" DoNothing
    Target "d" DoNothing

    // create graph
    "a" ==> "b" ==> "d" |> ignore
    "c1" ==> "c2" ==> "d" |> ignore

    let order = determineBuildOrder "d"
    validateBuildOrder order "d"

    match order with
        | [TargetSet ["a"; "c1"];TargetSet ["b"; "c2"];[|Target "d"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``Spurs run as early as possible``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c1" DoNothing
    Target "c2" DoNothing
    Target "d" DoNothing

    // create graph
    "a" ==> "b" ==> "d" |> ignore
    "a" ==> "c1" ==> "c2" ==> "d" |> ignore

    let order = determineBuildOrder "d"
    validateBuildOrder order "d"

    match order with
        | [[|Target "a"|];TargetSet ["b"; "c1"];[|Target "c2"|];[|Target "d"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``Spurs run as early as possible 3 and 2 length``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b1" DoNothing
    Target "b2" DoNothing
    Target "c1" DoNothing
    Target "c2" DoNothing
    Target "c3" DoNothing
    Target "d" DoNothing

    // create graph
    "a" ==> "b1" ==> "b2" ==> "d" |> ignore
    "a" ==> "c1" ==> "c2" ==> "c3" ==> "d" |> ignore

    let order = determineBuildOrder "d"
    validateBuildOrder order "d"

    match order with
        | [[|Target "a"|];TargetSet ["b1"; "c1"];TargetSet ["b2"; "c2"];[|Target "c3"|];[|Target "d"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``Spurs run as early as possible (reverse definition order)``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c1" DoNothing
    Target "c2" DoNothing
    Target "d" DoNothing

    // create graph
    "a" ==> "c1" ==> "c2" ==> "d" |> ignore
    "a" ==> "b" ==> "d" |> ignore    

    let order = determineBuildOrder "d"
    validateBuildOrder order "d"

    match order with
        | [[|Target "a"|];TargetSet ["b"; "c1"];[|Target "c2"|];[|Target "d"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``Spurs run as early as possible split on longer spur``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c1" DoNothing
    Target "c21" DoNothing
    Target "c22" DoNothing
    Target "d" DoNothing

    // create graph
    "a" ==> "b" ==> "d" |> ignore
    "a" ==> "c1" ==> "c21" ==> "d" |> ignore
    "a" ==> "c1" ==> "c22" ==> "d" |> ignore

    let order = determineBuildOrder "d"
    validateBuildOrder order "d"

    match order with
        | [[|Target "a"|];TargetSet ["b"; "c1"];TargetSet ["c21"; "c22"];[|Target "d"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``3 way Spurs run as early as possible``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c1" DoNothing
    Target "c2" DoNothing
    Target "d1" DoNothing
    Target "d2" DoNothing
    Target "d3" DoNothing
    Target "e" DoNothing

    // create graph
    "a" ==> "b" ==> "e" |> ignore
    "a" ==> "c1" ==> "c2" ==> "e" |> ignore
    "a" ==> "d1" ==> "d2" ==> "d3" ==> "e" |> ignore

    let order = determineBuildOrder "e"
    validateBuildOrder order "e"

    match order with
        | [[|Target "a"|];TargetSet ["b"; "c1"; "d1"];TargetSet ["c2"; "d2"];[|Target "d3"|];[|Target "e"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order

[<Fact>]
let ``Soft dependencies are respected when dependees are present``() = 
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    Target "d" DoNothing
    Target "e" DoNothing
    Target "f" DoNothing
    
   
    "a" ==> "b" ==> "c" |> ignore
    // d does not depend on c, but if something else forces c to run, then d must come after c.
    "d" <=? "c" |> ignore

    // Running f will run  c, d, and f.  The soft dependency of d on c means that c must run first.
    "d" ==> "f" |> ignore
    "e" ==> "f" |> ignore
    "c" ==> "f" |> ignore

    let order = determineBuildOrder "f"

    validateBuildOrder order "f"

    match order with
        | [[|Target "a"|];TargetSet ["b";];[|Target "c"|];TargetSet ["d";"e"];[|Target "f"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order
    ()

[<Fact>]
let ``Soft dependencies are ignored when dependees are not present``() = 
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    Target "d" DoNothing
    Target "e" DoNothing
    
   
    "a" ==> "b" ==> "c" |> ignore
    // d does not depend on c, but if something else forces c to run, then d must come after c.
    "c" ?=> "d" |> ignore
    
    // Running e will not run c, due to soft dependency
    "d" ==> "e" |> ignore
    "b" ==> "e" |> ignore

    let order = determineBuildOrder "e"

    validateBuildOrder order "e"

    match order with
        | [[|Target "a"|];TargetSet ["b";"d"];[|Target "e"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order
    ()