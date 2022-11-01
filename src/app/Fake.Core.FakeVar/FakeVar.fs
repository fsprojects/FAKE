namespace Fake.Core

/// <summary>
/// This module contains helpers for managing build time variables
/// </summary>
[<RequireQualifiedAccess>]
module FakeVar =

    open Fake.Core.Context

    let internal getFrom<'a> name context =
        context
        |> getFakeContext name
        |> Option.map (fun o ->
            try
                o :?> 'a
            with e ->
                raise <| exn (sprintf "Cast error on variable '%s'" name, e))

    /// <summary>
    /// Gets a strongly typed FakeVar by name returning an option type
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    let get<'a> name = forceFakeContext () |> getFrom<'a> name

    /// <summary>
    /// Gets a strongly typed FakeVar by name will fail if variable is not found
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    let getOrFail<'a> name =
        match get<'a> name with
        | Some v -> v
        | _ -> failwithf "Unable to find variable '%s'" name

    /// <summary>
    /// Gets a strongly typed FakeVar by name will return default value if variable is not found
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    /// <param name="defaultValue">The default value to return if variable is not found</param>
    let getOrDefault<'a> name defaultValue =
        match get<'a> name with
        | Some v -> v
        | _ -> defaultValue

    /// <summary>
    /// Removes a FakeVar by name
    /// </summary>
    let internal removeFrom name context =
        context |> removeFakeContext name |> ignore

    /// <summary>
    /// Removes a FakeVar by name
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    let remove name = forceFakeContext () |> removeFrom name

    /// <summary>
    /// Sets value of a FakeVar
    /// </summary>
    let internal setFrom name (v: 'a) context =
        context |> setFakeContext name v (fun _ -> v :> obj) |> ignore

    /// <summary>
    /// Sets value of a FakeVar
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    /// <param name="v">The value of the FakeVar</param>
    let set name (v: 'a) = forceFakeContext () |> setFrom name v

    /// <summary>
    /// Define a named FakeVar providing the get, remove and set
    /// And of the functions will fail if there is no context
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    let define<'a> name =
        (fun () ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> getFrom name context: 'a option
            | _ -> failwithf "Cannot retrieve '%s' as we have no fake context" name),
        (fun () ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> removeFrom name context
            | _ -> failwithf "Cannot remove '%s' as we have no fake context" name),
        (fun (v: 'a) ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> setFrom name v context
            | _ -> failwithf "Cannot set '%s' as we have no fake context" name)

    /// <summary>
    /// Define a named FakeVar providing the get, remove and set
    /// Will use a local variable if there is no context
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    let defineAllowNoContext<'a> name =
        let mutable varWithoutContext = None

        (fun () ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> getFrom name context: 'a option
            | _ -> varWithoutContext),
        (fun () ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> removeFrom name context
            | _ -> varWithoutContext <- None),
        (fun (v: 'a) ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> setFrom name v context
            | _ -> varWithoutContext <- Some v)

    /// <summary>
    /// Define a named FakeVar providing the get, remove and set
    /// Will always return 'None' when no context is set and 'throw' on set
    /// </summary>
    ///
    /// <param name="name">The name of the FakeVar</param>
    let defineOrNone<'a> name =
        (fun () ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> getFrom name context: 'a option
            | _ -> None),
        (fun () ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> removeFrom name context
            | _ -> failwithf "Cannot remove '%s' as we have no fake context" name),
        (fun (v: 'a) ->
            match getExecutionContext () |> getFakeExecutionContext with
            | Some context -> setFrom name v context
            | _ -> failwithf "Cannot set '%s' as we have no fake context" name)
