namespace Fake.Core

/// <summary>
/// This module tracks the context of the build.
/// <remarks>
/// This allows us to run some modules without any context and change behavior depending on the context
/// (For example <c>Fake.Process</c> kills all processes when the Fake Context exists, but it should not when used
/// as library)
/// </remarks>
/// </summary>
module Context =

    /// <summary>
    /// FAKE execution context type
    /// </summary>
    type FakeExecutionContext =
        {
            /// Mark if script is cached
            IsCached: bool

            /// The context data
            Context: System.Collections.Concurrent.ConcurrentDictionary<string, obj>

            /// The script file current build is running
            ScriptFile: string

            /// Script arguments
            Arguments: string list
        }

        interface System.IDisposable with
            member x.Dispose() =
                let l = x.Context.Values |> Seq.toList
                x.Context.Clear()

                let rec cleanSeq (s: System.Collections.IEnumerable) =
                    for item in s do
                        match item with
                        | :? System.IDisposable as d -> d.Dispose()
                        | :? System.Collections.IEnumerable as ie -> cleanSeq ie
                        | _ -> ()

                cleanSeq l

        static member Create isCached scriptFile args =
            { IsCached = isCached
              Context = System.Collections.Concurrent.ConcurrentDictionary<string, obj>()
              ScriptFile = scriptFile
              Arguments = args }

    /// FAKE runtime execution context
    type RuntimeContext =
        | Fake of FakeExecutionContext
        | UnknownObj of obj
        | Unknown

    [<RequireQualifiedAccess>]
    type internal RuntimeContextWrapper(t: RuntimeContext) =
#if !FX_NO_REMOTING
        inherit System.MarshalByRefObject()
#endif
        member x.Type = t

        override x.ToString() =
            match t with
            | Fake f -> sprintf "Wrapper(ScriptFile=%s)" f.ScriptFile
            | UnknownObj o -> sprintf "Wrapper(UnknownObj=%O)" o
            | Unknown -> sprintf "Wrapper(Unknown)"


#if USE_ASYNC_LOCAL
    open System.Threading

    let private fake_data =
        AsyncLocal<System.Collections.Concurrent.ConcurrentDictionary<string, obj>>()

    let private getDataDict () =
        let value = fake_data.Value

        if isNull value then
            let l = System.Collections.Concurrent.ConcurrentDictionary<string, obj>()
            fake_data.Value <- l
            l
        else
            value
#endif

    let private setContext (name: string) (o: obj) : unit =
#if USE_ASYNC_LOCAL
        let d = getDataDict ()
        d.AddOrUpdate(name, o, (fun _ old -> o)) |> ignore
#else
        System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(name, o)
#endif

    let private getContext (name: string) : obj =
        let result =
#if USE_ASYNC_LOCAL
            let d = getDataDict ()

            match d.TryGetValue(name) with
            | true, v -> v
            | false, _ -> null
#else
            System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(name)
#endif
        result

    let private fake_ExecutionType = "fake_context_execution_type"

    /// <summary>
    /// Gets FAKE execution context
    /// </summary>
    let getExecutionContext () =
        match getContext fake_ExecutionType with
        | null -> RuntimeContext.Unknown
        | :? RuntimeContextWrapper as e -> e.Type
        | o -> RuntimeContext.UnknownObj o

    /// <summary>
    /// Sets FAKE execution context to the given context
    /// </summary>
    let setExecutionContext (e: RuntimeContext) =
        setContext fake_ExecutionType (RuntimeContextWrapper(e))

    /// <summary>
    /// Remove execution context
    /// </summary>
    let removeExecutionContext () = setContext fake_ExecutionType null

    /// <summary>
    /// Gets FAKE execution context by FAKE runtime context
    /// </summary>
    ///
    /// <param name="e">FAKE runtime execution context</param>
    let getFakeExecutionContext (e: RuntimeContext) =
        match e with
        | RuntimeContext.UnknownObj _
        | RuntimeContext.Unknown -> None
        | RuntimeContext.Fake e -> Some e

    /// <summary>
    /// Gets FAKE execution context data by name
    /// </summary>
    ///
    /// <param name="name">FAKE execution context data name</param>
    /// <param name="f">FAKE execution context</param>
    let getFakeContext name (f: FakeExecutionContext) =
        match f.Context.TryGetValue(name) with
        | true, v -> Some v
        | _ -> None

    /// <summary>
    /// Removes FAKE execution context data by name
    /// </summary>
    ///
    /// <param name="name">FAKE execution context data name</param>
    /// <param name="f">FAKE execution context</param>
    let removeFakeContext (name: string) (f: FakeExecutionContext) =
        match f.Context.TryRemove(name) with
        | true, v -> Some v
        | _ -> None

    /// <summary>
    /// Set or update FAKE execution context data by name
    /// </summary>
    ///
    /// <param name="name">FAKE execution context data name</param>
    /// <param name="updateF">Callback to call when updating the value</param>
    /// <param name="f">FAKE execution context</param>
    let setFakeContext name (v: obj) updateF (f: FakeExecutionContext) =
        f.Context.AddOrUpdate(name, v, (fun _ old -> updateF old))

    /// <summary>
    /// Check if execution context is a FAKE execution context
    /// </summary>
    let isFakeContext () =
        getExecutionContext () |> getFakeExecutionContext |> Option.isSome

    /// <summary>
    /// Check and current context is a FAKE execution context and throws `InvalidOperationException`
    /// exception when not
    /// </summary>
    let forceFakeContext () =
        match getExecutionContext () with
        | RuntimeContext.UnknownObj o ->
            sprintf
                "Invalid Fake Execution context was found: Expected '%s' but was '%s'"
                typeof<RuntimeContextWrapper>.FullName
                (o.GetType().FullName)
            |> invalidOp
        | RuntimeContext.Unknown ->
            invalidOp
                "no Fake Execution context was found. You can initialize one via Fake.Core.Context.setExecutionContext"
        | RuntimeContext.Fake e -> e

    /// <summary>
    /// Creates and sets the FAKE execution context from command line arguments.
    /// </summary>
    let setExecutionContextFromCommandLineArgs scriptFile : unit =
        System.Environment.GetCommandLineArgs()
        |> Array.skip 2 // skip fsi & scriptFile
        |> Array.toList
        |> FakeExecutionContext.Create false scriptFile
        |> RuntimeContext.Fake
        |> setExecutionContext
