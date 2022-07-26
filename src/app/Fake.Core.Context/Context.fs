namespace Fake.Core

/// This module tracks the context of the build.
/// This allows us to run some modules without any context and change behavior depending on the context
/// (For example `Fake.Process` kills all processes when the Fake Context exists, but it should not when used as library)
module Context =

    /// FAKE execution context type
    type FakeExecutionContext =
        {
          /// Mark if script is cached
          IsCached: bool
          
          /// The context data
          Context: System.Collections.Concurrent.ConcurrentDictionary<string, obj>
          
          /// The script file current build is running
          ScriptFile: string
          
          /// Script arguments
          Arguments: string list }

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

    /// Gets FAKE execution context
    let getExecutionContext () =
        match getContext fake_ExecutionType with
        | null -> RuntimeContext.Unknown
        | :? RuntimeContextWrapper as e -> e.Type
        | o -> RuntimeContext.UnknownObj o

    /// Sets FAKE execution context to the given context
    let setExecutionContext (e: RuntimeContext) =
        setContext fake_ExecutionType (RuntimeContextWrapper(e))

    /// Remove execution context
    let removeExecutionContext () = setContext fake_ExecutionType null

    /// Gets FAKE execution context by FAKE runtime context
    /// 
    /// ## Parameters
    ///  - `e` - FAKE runtime execution context
    let getFakeExecutionContext (e: RuntimeContext) =
        match e with
        | RuntimeContext.UnknownObj _
        | RuntimeContext.Unknown -> None
        | RuntimeContext.Fake e -> Some e

    /// Gets FAKE execution context data by name
    /// 
    /// ## Parameters
    ///  - `name` - FAKE execution context data name
    ///  - `f` - FAKE execution context
    let getFakeContext name (f: FakeExecutionContext) =
        match f.Context.TryGetValue(name) with
        | true, v -> Some v
        | _ -> None

    /// Removes FAKE execution context data by name
    /// 
    /// ## Parameters
    ///  - `name` - FAKE execution context data name
    ///  - `f` - FAKE execution context
    let removeFakeContext (name: string) (f: FakeExecutionContext) =
        match f.Context.TryRemove(name) with
        | true, v -> Some v
        | _ -> None

    /// Set or update FAKE execution context data by name
    /// 
    /// ## Parameters
    ///  - `name` - FAKE execution context data name
    ///  - `updateF` - callback to call when updating the value
    ///  - `f` - FAKE execution context
    let setFakeContext name (v: obj) updateF (f: FakeExecutionContext) =
        f.Context.AddOrUpdate(name, v, (fun _ old -> updateF old))

    /// Check if execution context is a FAKE execution context
    let isFakeContext () =
        getExecutionContext () |> getFakeExecutionContext |> Option.isSome

    /// Check and current context is a FAKE execution context and throws `InvalidOperationException`
    /// exception when not
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
