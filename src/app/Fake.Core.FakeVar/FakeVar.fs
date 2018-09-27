/// This module contains helpers for managing build time variables
[<RequireQualifiedAccess>]
module Fake.Core.FakeVar

open Fake.Core.Context

let internal getFrom<'a> name context = 
    context
    |> getFakeContext name
    |> Option.map (fun o -> 
        try
            o :?> 'a
        with e -> 
            raise <| exn(sprintf "Cast error on variable '%s'" name, e))
    

/// Gets a strongly typed FakeVar by name returning an option type
let get<'a> name =
    forceFakeContext()
    |> getFrom<'a> name

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
let internal removeFrom name context =
    context
    |> removeFakeContext name
    |> ignore

/// Removes a FakeVar by name
let remove name =
    forceFakeContext()
    |> removeFrom name

/// Sets value of a FakeVar
let internal setFrom name (v:'a) context =
    context
    |> setFakeContext name v (fun _ -> v :> obj)
    |> ignore

/// Sets value of a FakeVar
let set name (v:'a) =
    forceFakeContext()
    |> setFrom name v

/// Define a named FakeVar providing the get, remove and set
/// And of the functions will fail if there is no context
let define<'a> name =
    (fun () ->
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> getFrom name context : 'a option
        | _ -> failwithf "Cannot retrieve '%s' as we have no fake context" name),
    (fun () ->
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> removeFrom name context
        | _ -> failwithf "Cannot remove '%s' as we have no fake context" name),
    (fun (v : 'a) -> 
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> setFrom name v context
        | _ -> failwithf "Cannot set '%s' as we have no fake context" name)

/// Define a named FakeVar providing the get, remove and set
/// Will use a local variable if there is no context
let defineAllowNoContext<'a> name =
    let mutable varWithoutContext = None
    (fun () ->
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> getFrom name context : 'a option
        | _ -> varWithoutContext),
    (fun () ->
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> removeFrom name context
        | _ -> varWithoutContext <- None),
    (fun (v : 'a) -> 
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> setFrom name v context
        | _ -> varWithoutContext <- Some v)

/// Define a named FakeVar providing the get, remove and set
/// Will always return 'None' when no context is set and 'throw' on set
let defineOrNone<'a> name =
    (fun () ->
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> getFrom name context : 'a option
        | _ -> None),
    (fun () ->
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> removeFrom name context
        | _ -> failwithf "Cannot remove '%s' as we have no fake context" name),
    (fun (v : 'a) -> 
        match getExecutionContext() |> getFakeExecutionContext with
        | Some context -> setFrom name v context
        | _ -> failwithf "Cannot set '%s' as we have no fake context" name)