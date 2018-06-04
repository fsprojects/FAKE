/// This module tracks the context of the build.
/// This allows us to run some modules without any context and change behavior depending on the context
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
        let rec cleanSeq (s:System.Collections.IEnumerable) =
          for item in s do
            match item with
            | :? System.IDisposable as d -> d.Dispose()
            | :? System.Collections.IEnumerable as ie -> cleanSeq ie
            | _ -> ()
        cleanSeq l
    static member Create (isCached) scriptFile args =
      { IsCached = isCached
        Context = new System.Collections.Concurrent.ConcurrentDictionary<string, obj>()
        ScriptFile = scriptFile
        Arguments = args }

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

#if USE_ASYNC_LOCAL
open System.Threading
let private fake_data = new AsyncLocal<System.Collections.Concurrent.ConcurrentDictionary<string, obj>>()

let private getDataDict() =
  let value = fake_data.Value
  if isNull value then
    let l = new System.Collections.Concurrent.ConcurrentDictionary<string, obj>()
    fake_data.Value <- l
    l
  else
    value  
  

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
  | o -> RuntimeContext.UnknownObj o

let setExecutionContext (e:RuntimeContext) = setContext fake_ExecutionType (new RuntimeContextWrapper(e))

let getFakeExecutionContext (e:RuntimeContext) =
  match e with
  | RuntimeContext.UnknownObj _
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
  match getExecutionContext() with
  | RuntimeContext.UnknownObj o ->
    sprintf "Invalid Fake Execution context was found: Expected '%s' but was '%s'" (typeof<RuntimeContextWrapper>.FullName) (o.GetType().FullName)
    |> invalidOp
  | RuntimeContext.Unknown ->
    invalidOp "no Fake Execution context was found. You can initialize one via Fake.Core.Context.setExecutionContext"
  | RuntimeContext.Fake e -> e

let getFakeVar<'a> name =
  forceFakeContext()
  |> getFakeContext name
  |> Option.map (fun o -> o :?> 'a)

let getFakeVarOrFail<'a> name =
  match getFakeVar<'a> name with
  | Some v -> v
  | _ -> failwithf "Unable to find FakeVar %s" name
  
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
