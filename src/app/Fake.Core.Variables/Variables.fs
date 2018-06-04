/// This module contains helpers for managing build time variables
module Fake.Core.Variables

open Fake.Core.Context

let get<'a> name =
  forceFakeContext()
  |> getFakeContext name
  |> Option.map (fun o -> o :?> 'a)

let getOrFail<'a> name =
  match get<'a> name with
  | Some v -> v
  | _ -> failwithf "Unable to find variable '%s'" name

let getOrDefault<'a> name defaultValue =
  match get<'a> name with
  | Some v -> v
  | _ -> defaultValue
  
let remove name =
  forceFakeContext()
  |> removeFakeContext name
  |> Option.map (fun o -> o :?> 'a)

let set name (v:'a) =
  forceFakeContext()
  |> setFakeContext name v (fun _ -> v :> obj)
  :?> 'a

let fakeVar<'a> name =
  (fun () -> get name : 'a option),
  (fun () -> (remove name : 'a option) |> ignore),
  (fun (v : 'a) -> set name v |> ignore)

let fakeVarNoContext<'a> name =
  let mutable varWithoutContext = None
  (fun () -> 
    if isFakeContext() then
      get name : 'a option
    else varWithoutContext),
  (fun () -> 
    if isFakeContext() then
      (remove name : 'a option) |> ignore
    else varWithoutContext <- None),
  (fun (v : 'a) -> 
    if isFakeContext() then
      set name v |> ignore
    else varWithoutContext <- Some v)
