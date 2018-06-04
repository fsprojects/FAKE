/// This module contains helpers for managing build time variables
module Fake.Core.Variables

open Fake.Core.Context

let getFakeVar<'a> name =
  forceFakeContext()
  |> getFakeContext name
  |> Option.map (fun o -> o :?> 'a)

let getFakeVarOrFail<'a> name =
  match getFakeVar<'a> name with
  | Some v -> v
  | _ -> failwithf "Unable to find FakeVar '%s'" name

let getFakeVarOrDefault<'a> name defaultValue =
  match getFakeVar<'a> name with
  | Some v -> v
  | _ -> defaultValue
  
let removeFakeVar name =
  forceFakeContext()
  |> removeFakeContext name
  |> Option.map (fun o -> o :?> 'a)

let setFakeVar name (v:'a) =
  forceFakeContext()
  |> setFakeContext name v (fun _ -> v :> obj)
  :?> 'a

let fakeVar name =
  (fun () -> getFakeVar name : 'a option),
  (fun () -> (removeFakeVar name : 'a option) |> ignore),
  (fun (v : 'a) -> setFakeVar name v |> ignore)

let fakeVarAllowNoContext name =
  let mutable varWithoutContext = None
  (fun () -> 
    if isFakeContext() then
      getFakeVar name : 'a option
    else varWithoutContext),
  (fun () -> 
    if isFakeContext() then
      (removeFakeVar name : 'a option) |> ignore
    else varWithoutContext <- None),
  (fun (v : 'a) -> 
    if isFakeContext() then
      setFakeVar name v |> ignore
    else varWithoutContext <- Some v)
