
module Fake.Core.TargetTests

open Fake.Runtime
open Fake.Core
open Expecto
open Expecto.Flip

let run targetName =
    try Target.RunAndGetContext targetName
    with | :? BuildFailedException as bfe ->
        match bfe.Info with
        | Some context -> context
        | None -> failwithf "No context given!"

open Fake.Core.TargetOperators

[<Tests>]
let tests = 
  testList "Fake.Core.Target.Tests" [
    testCase "Test that we run a simple target with dependency" <| fun _ ->
      
      use execContext = Fake.Core.Context.FakeExecutionContext.Create false "text.fsx" []
      Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

      Target.Create "SimpleTest" ignore
      Target.Create "Dependency" ignore
      
      "Dependency" ==> "SimpleTest" |> ignore
      let context = run "SimpleTest"
      Expect.equal "Expected both tasks to succeed" false context.HasError  
      Expect.equal "Expected context to contain both targets" 2 context.PreviousTargets.Length
      
    testCase "Test we output targets after failing targets" <| fun _ ->
      
      use execContext = Fake.Core.Context.FakeExecutionContext.Create false "text.fsx" []
      Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

      Target.Create "SimpleTest" ignore
      Target.Create "Dependency" (fun _ -> failwith "failed dependency")
      
      "Dependency" ==> "SimpleTest" |> ignore
      let context = run "SimpleTest"
      Expect.equal "Expected failure" true context.HasError
      Expect.equal "Expected context to contain both targets" 2 context.PreviousTargets.Length  // second one as "skipped"
  ]    