/// This module tracks the context of the build.
/// This allows as to run some modules without any context and change behavior depending on the context
/// (For example `Fake.Process` kills all processes when the Fake Context exists, but it should not when used as library)
module Fake.Core.Context

type FakeExecutionContext =
  { IsCached : bool
    Context : System.Collections.Concurrent.ConcurrentDictionary<string, obj>
    ScriptFile : string
    Arguments : string list }
    interface System.IDisposable with
      member x.Dispose () =
        let l = x.Context.Values |> Seq.toList
        x.Context.Clear()
        l |> Seq.iter (function
          | :? System.IDisposable as d -> d.Dispose()
          | _ -> ())
    static member Create (isCached) scriptFile args =
      { IsCached = isCached
        Context = new System.Collections.Concurrent.ConcurrentDictionary<string, obj>()
        ScriptFile = scriptFile
        Arguments = args }

type RuntimeContext =
  | Fake of FakeExecutionContext
  | Unknown

[<RequireQualifiedAccess>]
type internal RuntimeContextWrapper(t: RuntimeContext) =
#if !FX_NO_REMOTING
    inherit System.MarshalByRefObject()
#endif
    member x.Type = t

#if USE_ASYNC_LOCAL
open System.Threading
let private fake_data =
  let l = new AsyncLocal<System.Collections.Concurrent.ConcurrentDictionary<string, obj>>()
  l.Value <- new System.Collections.Concurrent.ConcurrentDictionary<string, obj>()
  l
let private getDataDict() = fake_data.Value
#endif

let private setContext (name:string) (o : obj) : unit =
#if USE_ASYNC_LOCAL
  let d = getDataDict()
  d.AddOrUpdate(name, o, fun _ old -> o) |> ignore
#else
  System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(name, o)
#endif

let private getContext (name:string) : obj =
#if USE_ASYNC_LOCAL
  let d = getDataDict()
  match d.TryGetValue(name) with
  | true, v -> v
  | false, _ -> null
#else
  System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(name)
#endif

let private fake_ExecutionType = "fake_context_execution_type"

let getExecutionContext () =
  match getContext fake_ExecutionType with
  | null -> RuntimeContext.Unknown
  | :? RuntimeContextWrapper as e -> e.Type
  | _ -> RuntimeContext.Unknown

let setExecutionContext (e:RuntimeContext) = setContext fake_ExecutionType (new RuntimeContextWrapper(e))

let getFakeExecutionContext (e:RuntimeContext) =
  match e with
  | RuntimeContext.Unknown -> None
  | RuntimeContext.Fake e -> Some e

let getFakeContext name (f:FakeExecutionContext)  = 
  match f.Context.TryGetValue(name) with
  | true, v -> Some v
  | _ -> None
let removeFakeContext name (f:FakeExecutionContext) =
  match f.Context.TryRemove(name) with
  | true, v -> Some v
  | _ -> None
let setFakeContext name (v:obj) updateF (f:FakeExecutionContext) =
  f.Context.AddOrUpdate (name, v, fun _ old -> updateF old)

let isFakeContext () =
  getExecutionContext()
  |> getFakeExecutionContext
  |> Option.isSome

let forceFakeContext () =
  match getExecutionContext()
        |> getFakeExecutionContext with
  | None -> invalidOp "no Fake Execution context was found. You can initialize one via Fake.Core.Context.setExecutionContext"
  | Some e -> e

let getFakeVar name =
  forceFakeContext()
  |> getFakeContext name
  |> Option.map (fun o -> o :?> 'a)
  
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