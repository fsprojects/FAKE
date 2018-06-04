/// This module contains helpers for managing build time variables
[<RequireQualifiedAccess>]
module Fake.Core.Variables

open Fake.Core.Context

let get<'a> name =
  forceFakeContext()
  |> getFakeContext name
  |> Option.map (fun o -> try
                            o :?> 'a
                          with e -> 
                            raise <| exn(sprintf "Cast error on variable %s" name, e)
                )

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
  |> ignore

let set name (v:'a) =
  forceFakeContext()
  |> setFakeContext name v (fun _ -> v :> obj)
  |> ignore

let fakeVar<'a> name =
  (fun () -> get name : 'a option),
  (fun () -> remove name),
  (fun (v : 'a) -> set name v)

let fakeVarNoContext<'a> name =
    let mutable varWithoutContext = None
    (fun () -> 
        if isFakeContext() then
            get name : 'a option
        else 
            varWithoutContext
    ),
    (fun () -> 
        if isFakeContext() then
            remove name
        else 
            varWithoutContext <- None
    ),
    (fun (v : 'a) -> 
        if isFakeContext() then
            set name v
        else 
            varWithoutContext <- Some v
    )
