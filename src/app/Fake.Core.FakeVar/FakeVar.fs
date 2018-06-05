/// This module contains helpers for managing build time variables
[<RequireQualifiedAccess>]
module Fake.Core.FakeVar

open Fake.Core.Context

/// Gets a strongly typed FakeVar by name returning an option type
let get<'a> name =
    forceFakeContext()
    |> getFakeContext name
    |> Option.map (fun o -> try
                                o :?> 'a
                            with e -> 
                                raise <| exn(sprintf "Cast error on variable '%s'" name, e)
                  )

/// Gets a strongly typed FakeVar by name will fail if variable is not found
let getOrFail<'a> name =
    match get<'a> name with
    | Some v -> v
    | _ -> failwithf "Unable to find variable '%s'" name

/// Gets a strongly typed FakeVar by name will return default value if variable is not found
let getOrDefault<'a> name defaultValue =
    match get<'a> name with
    | Some v -> v
    | _ -> defaultValue
  
/// Removes a FakeVar by name
let remove name =
    forceFakeContext()
    |> removeFakeContext name
    |> ignore

/// Sets value of a FakeVar
let set name (v:'a) =
    forceFakeContext()
    |> setFakeContext name v (fun _ -> v :> obj)
    |> ignore

/// Define a named FakeVar providing the get, remove and set
/// Will fail if there is no context
let define<'a> name =
    if isFakeContext() then
        (fun () -> get name : 'a option),
        (fun () -> remove name),
        (fun (v : 'a) -> set name v)
    else
        failwithf "Cannot define variable '%s' without context" name

/// Define a named FakeVar providing the get, remove and set
/// Will create a local variable if there is no context
let defineAllowNoContext<'a> name =
    if isFakeContext() then
        (fun () -> get name : 'a option),
        (fun () -> remove name),
        (fun (v : 'a) -> set name v)
    else         
        let mutable varWithoutContext = None
        (fun () -> varWithoutContext),
        (fun () -> varWithoutContext <- None),
        (fun (v : 'a) -> varWithoutContext <- Some v)
