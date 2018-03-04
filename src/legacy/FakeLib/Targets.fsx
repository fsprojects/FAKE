// At the devspace in Leipzig I had a discusiion with Sergey Shishkin and Max Malook about the current implementation of FAKE's 
// target runtime. Together we found some interesting improvements which might help (C# ?!) devs
// to get started in no time. It also allows usto get build scripts which are better maintainable.
//
// Sergey proposed we should build an opionated nuget package (or maybe even more than one) on top of FAKE.
// Let's assume we have a C#/F# solution and we use NUnit.
// If you `install-package FAKE-NUnit' you would get a predefined build pipeline which would give you a standard build and test 
// run out of the box. 
// This package might build all "**/*.?sproj" files and run NUnit on "**/*Test*.dll"
// Afterwards it might zip all other *.dll or something like that.
// Sergey told me he already uses such an opionated nuget package in production (which is AWESOME).
// 
// Of course such an opionated default pipeline can't work in every case so we need to provide a way to extend this
// pipeline in a custom (solution-specific) build script.
// This gist shows a prototype implementation (not important - scroll down over it) and a sample for this.
// 
// This is a very early work and I really appreciate any feedback.
// Some remarks:
//   * I want to get feedback about the idea (building opionated nuget packages which provide a default pipeline)
//   * I want to discuss the syntax of the resulting DSL
//   * We need to keep ==> and <=> operators backwards compatible.
//   * <=> is already buggy in FAKE so we don't need to maintain compatibility here

// *********************************************************************************************************************
//                                                Prototype for new target syntax
// *************************************************************************************************************************
open System.Collections.Generic
open System.Linq

type Target = {
    Action : unit -> unit
    Name : string
    Dependencies : List<string>
    mutable Parent : string
    SubTargets : List<string> }

let targets = new Dictionary<string,Target>()

let Target name action =
    let target = {
        Name = name
        Action = action
        SubTargets = new List<string>()
        Dependencies = new List<string>()
        Parent = null}

    targets.Add(name, target)

let alreadyRunTargets = new List<string>()

let getTarget name =
    match targets.TryGetValue name with
    | true, target -> target
    | _ -> failwith "the target %s was not defined."

let inline (==>) oldTarget targetName =
    let target = getTarget targetName
    target.Dependencies.Add(oldTarget)

    targetName

let inline (|=>) nodeTarget target =    
    targets.[nodeTarget].SubTargets.Add(target)

    if targets.[target].Parent <> null then
        failwithf "The target %s is already a subtarget of %s" target targets.[target].Parent
    targets.[target].Parent <- nodeTarget
    
    nodeTarget

let run targetName =    
    let rec run runSubTargets targetName =
        let target = getTarget targetName

        if alreadyRunTargets.Contains targetName then () else
        alreadyRunTargets.Add(targetName) |> ignore

        printfn "Starting target %s" targetName        

        if target.Parent <> null then
            printfn "Running parent %s from %s" target.Parent targetName
            run false target.Parent

        for dependentTarget in target.Dependencies do
            printfn "Running dependency %s from %s" dependentTarget targetName
            run true dependentTarget

        target.Action()

        if runSubTargets then            
            for subTarget in target.SubTargets do
                run true subTarget

        printfn "Finished target %s" targetName

    run true targetName

let DoNothing = fun () -> ()

// *************************************************************************************************************************
//                                                Sample
// *************************************************************************************************************************

// Create default targets - these targets can be shipped with an opionated nuget package on top of FAKE
//   (Of course we should provide some default implementaions and not DoNothing)
Target "Clean" DoNothing
Target "Build" DoNothing
Target "Test" DoNothing
Target "BuildDocs" DoNothing
Target "Zip" DoNothing
Target "Deploy" DoNothing

// Default pipe line - shipped with the opnionated package
// The x ==> y works as before and means if I want to run y then FAKE needs to be sure that x was already run. 
// In any case FAKE would run x before y.
"Clean"
  ==> "Build"
  ==> "Test"
  ==> "BuildDocs"
  ==> "Deploy"

// Here starts the custom (solution-specific) build.fsx
// We define some specialized targets
Target "Build1" (fun _ -> printfn "I'am building some special projects which are not found in the default Build target")
Target "Build2" (fun _ -> printfn "I'am compiling this Brainfuck code - don't stop me now")
Target "Test1"  (fun _ -> printfn "Doing special testing stuff")
Target "Test2"  (fun _ -> printfn "Man, this testing is really hard")

// Now we need to hook up the custom targets into the predefined build pipeline
// Adding custom build steps to the default Build target.
// The semantics of x |=> y |=> z is that we want to insert y and z into the build pipleline at the same position like x.
"Build"
  |=> "Build1"
  |=> "Build2"

// Adding custom test steps to the default Test target
"Test"
  |=> "Test1"
  |=> "Test2"


// Now we try this out - see F# Interactive output
let rerun target =
    alreadyRunTargets.Clear()
    run target

rerun "Test2"     
rerun "Test1"     
rerun "Test"     
rerun "Build2"     
rerun "Deploy"