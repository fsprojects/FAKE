namespace FakeExpecto

open System.Globalization

#nowarn "46"
open Expecto
open System
open System.Diagnostics
open System.Reflection
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
module internal Helpers =
    let inline dispose (d:IDisposable) = d.Dispose()
    let inline addFst a b = a,b
    let inline addSnd b a = a,b
    let inline fst3 (a,_,_) = a
    let inline commaString (i:int) = i.ToString("#,##0")
    let inline tryParse (s: string) =
      let mutable r = Unchecked.defaultof<_>
      if (^a : (static member TryParse: string * ^a byref -> bool) (s, &r))
      then Some r else None
    let inline tryParseNumber (s: string) =
      let mutable r = Unchecked.defaultof<_>
      if (^a : (static member TryParse: string * NumberStyles * IFormatProvider * ^a byref -> bool) (s, NumberStyles.Any, CultureInfo.InvariantCulture, &r))
      then Some r else None
    
    type ResizeMap<'k,'v> = Collections.Generic.Dictionary<'k,'v>

    module Option =
      let orDefault def =
        function | Some a -> a | None -> def
      let orFun fn =
        function | Some a -> a | None -> fn()
        
    module Result =
        let traverse f list =
            List.fold (fun s i ->
                match s,f i with
                | Ok l, Ok h -> Ok (h::l)
                | Error l, Ok _ -> Error l
                | Ok _, Error e -> Error [e]
                | Error l, Error h -> Error (h::l)
            ) (Ok []) list
        let sequence list = traverse id list

    module Async =
        open System
        open System.Threading
        open System.Threading.Tasks

        let map fn a =
          async {
            let! v = a
            return fn v
          }

        let bind fn a =
          async.Bind(a, fn)

        let foldSequentiallyWithCancel (ct: CancellationToken) folder state (s:_ seq) =
          async {
            let mutable state = state
            let tcs = TaskCompletionSource()
            use _ = Action tcs.SetResult |> ct.Register
            let tasks: Task[] = [| null; tcs.Task |]
            use e = s.GetEnumerator()
            while not ct.IsCancellationRequested && e.MoveNext() do
              let task = Async.StartAsTask e.Current
              tasks.[0] <- task :> Task
              if Task.WaitAny tasks = 0 then
                state <- folder state task.Result
            return state
          }

        let foldSequentially folder state (s: _ seq) =
          foldSequentiallyWithCancel CancellationToken.None folder state s

        let foldParallelWithCancel maxParallelism (ct: CancellationToken) folder state (s: _ seq) =
          async {
            let mutable state = state
            use e = s.GetEnumerator()
            if e.MoveNext() then
              let mutable tasks = [Async.StartAsTask e.Current]
              while not(ct.IsCancellationRequested || List.isEmpty tasks) do
                if List.length tasks = maxParallelism || not(e.MoveNext()) then
                  while not( tasks |> List.exists (fun t -> t.IsCompleted)
                          || ct.IsCancellationRequested) do
                    do! Async.Sleep 10
                  tasks |> List.tryFindIndex (fun t -> t.IsCompleted)
                  |> Option.iter (fun i ->
                    let a,b = List.splitAt i tasks
                    state <- (List.head b).Result |> folder state
                    tasks <- a @ List.tail b
                  )
                else tasks <- Async.StartAsTask e.Current :: tasks
            return state
          }
          
    let matchFocusAttributes = function
        | "Expecto.FTestsAttribute" -> Some (1, Focused)
        | "Expecto.TestsAttribute" -> Some (2, Normal)
        | "Expecto.PTestsAttribute" -> Some (3, Pending)
        | _ -> None
          
    let allTestAttributes =
        Set [
            typeof<FTestsAttribute>.FullName
            typeof<TestsAttribute>.FullName
            typeof<PTestsAttribute>.FullName
        ]
          
    type MemberInfo with
        member m.HasAttributePred (pred: Type -> bool) =
            m.GetCustomAttributes true
            |> Seq.filter (fun a -> pred(a.GetType()))
            |> Seq.length |> (<) 0
          
        member m.HasAttributeType (attr: Type) =
            m.HasAttributePred ((=) attr)
          
        member m.HasAttribute (attr: string) =
            m.HasAttributePred (fun (t: Type) -> t.FullName = attr)
          
        member m.GetAttributes (attr: string) : Attribute seq =
            m.GetCustomAttributes true
            |> Seq.filter (fun a -> a.GetType().FullName = attr)
            |> Seq.cast
          
        member m.MatchTestsAttributes () =
            m.GetCustomAttributes true
            |> Seq.map (fun t -> t.GetType().FullName)
            |> Set.ofSeq
            |> Set.intersect allTestAttributes
            |> Set.toList
            |> List.choose matchFocusAttributes
            |> List.sortBy fst
            |> List.map snd
            |> List.tryFind (fun _ -> true)



open Helpers

type private TestNameHolder() =
  [<ThreadStatic;DefaultValue>]
  static val mutable private name : string
  static member Name
      with get () = TestNameHolder.name
      and  set name = TestNameHolder.name <- name

// When exposing Extension Methods, you should declare an assembly-level attribute (in addition to class and method)
[<assembly: Extension>]
do ()

type FlatTest = Expecto.FlatTest

[<CompilationRepresentationAttribute(CompilationRepresentationFlags.ModuleSuffix)>]
module Test =
  /// Compute the child test state based on parent test state
  let computeChildFocusState parentState childState =
    match parentState, childState with
    | Focused, Pending -> Pending
    | Pending, _ -> Pending
    | Focused, _ -> Focused
    | Normal, _ -> childState

  /// Is focused set on at least one test
  let rec isFocused test =
    match test with
    | TestLabel (_,_,Focused)
    | TestCase (_,Focused)
    | TestList (_,Focused) -> true
    | TestLabel (_,_,Pending)
    | TestList (_,Pending)
    | TestCase _ -> false
    | TestLabel (_,test,Normal)
    | Test.Sequenced (_,test) -> isFocused test
    | TestList (tests,Normal) -> List.exists isFocused tests

  /// Flattens a tree of tests
  let toTestCodeList test =
    let isFocused = isFocused test
    let rec loop parentName testList parentState sequenced =
      function
      | TestLabel (name, test, state) ->
        let fullName =
          if String.IsNullOrEmpty parentName
            then name
            else parentName + "/" + name
        loop fullName testList (computeChildFocusState parentState state) sequenced test
      | TestCase (test, state) ->
        { name=parentName
          test=test
          state=computeChildFocusState parentState state
          focusOn = isFocused
          sequenced=sequenced } :: testList
      | TestList (tests, state) -> List.collect (loop parentName testList (computeChildFocusState parentState state) sequenced) tests
      | Test.Sequenced (sequenced,test) -> loop parentName testList parentState sequenced test
    loop null [] Normal InParallel test

  let fromFlatTests (tests:FlatTest list) =
    TestList(
      List.map (fun t ->
        TestLabel(t.name, Test.Sequenced(t.sequenced, TestCase (t.test, t.state)), t.state)
      ) tests
    , Normal)

  let shuffle (test:Test) = Expecto.Test.shuffle test

  /// Change the FocusState by applying the old state to a new state
  /// Note: this is not state replacement!!!
  ///
  /// Used in replaceTestCode and the order is intended for scenario:
  ///  1. User wants to automate some tests and his intent is not to change
  ///      the test state (use Normal), so this way the current state will be preserved
  ///
  /// Don't see the use case: the user wants to automate some tests and wishes
  /// to change the test states
  let rec translateFocusState newFocusState =
    function
    | TestCase (test, oldFocusState) -> TestCase(test, computeChildFocusState oldFocusState newFocusState)
    | TestList (testList, oldFocusState) -> TestList(testList, computeChildFocusState oldFocusState newFocusState)
    | TestLabel (label, test, oldFocusState) -> TestLabel(label, test, computeChildFocusState oldFocusState newFocusState)
    | Test.Sequenced (sequenced,test) -> Test.Sequenced (sequenced,translateFocusState newFocusState test)

  /// Recursively replaces TestCodes in a Test.
  /// Check translateFocusState for focus state behaviour description.
  let rec replaceTestCode (f:string -> TestCode -> Test) =
    function
    | TestLabel (label, TestCase (test, childState), parentState) ->
      f label test
      |> translateFocusState (computeChildFocusState parentState childState)
    | TestCase (test, state) ->
      f null test
      |> translateFocusState state
    | TestList (testList, state) -> TestList (List.map (replaceTestCode f) testList, state)
    | TestLabel (label, test, state) -> TestLabel (label, replaceTestCode f test, state)
    | Test.Sequenced (sequenced,test) -> Test.Sequenced (sequenced,replaceTestCode f test)

  /// Filter tests by name.
  let filter pred =
    toTestCodeList
    >> List.filter (fun t -> pred t.name)
    >> List.map (fun t ->
        let test = TestLabel (t.name, TestCase (t.test, t.state), t.state)
        match t.sequenced with
        | InParallel ->
          test
        | s ->
          Test.Sequenced (s,test)
      )
    >> (fun x -> TestList (x,Normal))

  /// Applies a timeout to a test.
  let timeout timeout (test: TestCode) : TestCode =
    let timeoutAsync testAsync =
      async {
        try
          let! async = Async.StartChild(testAsync, timeout)
          do! async
        with :? TimeoutException ->
          let ts = TimeSpan.FromMilliseconds (float timeout)
          raise <| AssertException(sprintf "Timeout (%A)" ts)
      }

    match test with
    | Sync test -> async { test() } |> timeoutAsync |> Async
    | SyncWithCancel test ->
      SyncWithCancel (fun ct ->
        Async.StartImmediate(async { test ct } |> timeoutAsync)
      )
    | Async test -> timeoutAsync test |> Async
    | AsyncFsCheck (testConfig, stressConfig, test) ->
      AsyncFsCheck (testConfig, stressConfig, test >> timeoutAsync)



// TODO: make internal?
module Impl =
  open Expecto.Logging
  open Expecto.Logging.Message
  open Mono.Cecil

  let mutable logger = Log.create "Expecto"
  let setLogName name = logger <- Log.create name

  let rec private exnWithInnerMsg (ex: exn) msg =
    let currentMsg =
      msg + (sprintf "%s%s" Environment.NewLine (ex.ToString()))
    if isNull ex.InnerException then
      currentMsg
    else
      exnWithInnerMsg ex.InnerException currentMsg

  type TestResult = Expecto.Impl.TestResult

  type TestSummary = Expecto.Impl.TestSummary

  type TestRunSummary = Expecto.Impl.TestRunSummary

  let createSummaryMessage (summary: TestRunSummary) = Expecto.Impl.createSummaryMessage summary

  let createSummaryText (summary: TestRunSummary) =  Expecto.Impl.createSummaryText summary

  let logSummary (summary: TestRunSummary) =
    createSummaryMessage summary
    |> logger.logWithAck Info

  let logSummaryWithLocation locate (summary: TestRunSummary) = Expecto.Impl.logSummaryWithLocation locate summary

  /// Hooks to print report through test run
  type TestPrinters = Expecto.Impl.TestPrinters

  // Runner options
  type ExpectoConfig = Expecto.Impl.ExpectoConfig

  let execTestAsync (ct:CancellationToken) (config:ExpectoConfig) (test:FlatTest) : Async<TestSummary> =
    async {
      let w = Stopwatch.StartNew()
      try
        match test.shouldSkipEvaluation with
        | Some ignoredMessage ->
          return TestSummary.single (TestResult.Ignored ignoredMessage) 0.0
        | None ->
          TestNameHolder.Name <- test.name
          match test.test with
          | Sync test ->
            test()
          | SyncWithCancel test ->
            test ct
          | Async test ->
            do! test
          | AsyncFsCheck (testConfig, stressConfig, test) ->
            let fsConfig =
              match config.stress with
              | None -> testConfig
              | Some _ -> stressConfig
              |> Option.orFun (fun () ->
                  { FsCheckConfig.defaultConfig with
                      maxTest = config.fsCheckMaxTests
                      startSize = config.fsCheckStartSize
                      endSize =
                        match config.fsCheckEndSize, config.stress with
                        | Some i, _ -> i
                        | None, None -> 100
                        | None, Some _ -> 10000
                  }
                )
            do! test fsConfig
          w.Stop()
          return TestSummary.single TestResult.Passed (float w.ElapsedMilliseconds)
      with
        | :? AssertException as e ->
          w.Stop()
          let msg =
            "\n" + e.Message + "\n" +
            (e.StackTrace.Split('\n')
             |> Seq.skipWhile (fun l -> l.StartsWith("   at Expecto.Expect."))
             |> Seq.truncate 5
             |> String.concat "\n")
          return TestSummary.single (TestResult.Failed msg) (float w.ElapsedMilliseconds)
        | :? FailedException as e ->
          w.Stop()
          return TestSummary.single (TestResult.Failed ("\n"+e.Message)) (float w.ElapsedMilliseconds)
        | :? IgnoreException as e ->
          w.Stop()
          return TestSummary.single (TestResult.Ignored e.Message) (float w.ElapsedMilliseconds)
        | :? AggregateException as e when e.InnerExceptions.Count = 1 ->
          w.Stop()
          if e.InnerException :? IgnoreException then
            return TestSummary.single (TestResult.Ignored e.InnerException.Message) (float w.ElapsedMilliseconds)
          else
            return TestSummary.single (TestResult.Error e.InnerException) (float w.ElapsedMilliseconds)
        | e ->
          w.Stop()
          return TestSummary.single (TestResult.Error e) (float w.ElapsedMilliseconds)
    }

  let private numberOfWorkers limit (config:ExpectoConfig) =
    if config.parallelWorkers < 0 then
      -config.parallelWorkers * Environment.ProcessorCount
    elif config.parallelWorkers = 0 then
      if limit then
        Environment.ProcessorCount
      else
        Int32.MaxValue
    else
      config.parallelWorkers

  /// Evaluates tests.
  let evalTestsWithCancel (ct:CancellationToken) (config:ExpectoConfig) test progressStarted =
    async {

      let tests = Test.toTestCodeList test
      let testLength =
        tests
        |> Seq.where (fun t -> Option.isNone t.shouldSkipEvaluation)
        |> Seq.length

      let testsCompleted = ref 0

      let evalTestAsync (test:FlatTest) =

        let beforeEach (test:FlatTest) =
          config.printer.beforeEach test.name

        async {
          let! beforeAsync = beforeEach test |> Async.StartChild
          let! result = execTestAsync ct config test
          do! beforeAsync
          do! TestPrinters.printResult config test result

          //if progressStarted && Option.isNone test.shouldSkipEvaluation then
          //  Fraction (Interlocked.Increment testsCompleted, testLength)
          //  |> ProgressIndicator.update

          return test,result
        }

      let inline cons xs x = x::xs

      if not config.``parallel`` ||
         config.parallelWorkers = 1 ||
         List.forall (fun t -> t.sequenced=Synchronous) tests then
        return!
          List.map evalTestAsync tests
          |> Async.foldSequentiallyWithCancel ct cons []
      else
        let sequenced =
          List.filter (fun t -> t.sequenced=Synchronous) tests
          |> List.map evalTestAsync

        let parallel =
          List.filter (fun t -> t.sequenced<>Synchronous) tests
          |> Seq.groupBy (fun t -> t.sequenced)
          |> Seq.collect(fun (group,tests) ->
              match group with
              | InParallel ->
                Seq.map (evalTestAsync >> List.singleton) tests
              | _ ->
                Seq.map evalTestAsync tests
                |> Seq.toList
                |> Seq.singleton
            )
          |> Seq.toList
          |> List.sortBy (List.length >> (~-))
          |> List.map (
              function
              | [test] -> Async.map List.singleton test
              | l -> Async.foldSequentiallyWithCancel ct cons [] l
            )

        let! parallelResults =
          let noWorkers = numberOfWorkers false config
          Async.foldParallelWithCancel noWorkers ct (@) [] parallel

        if List.isEmpty sequenced |> not && List.isEmpty parallel |> not then
          do! config.printer.info "Starting sequenced tests..."

        let! results = Async.foldSequentiallyWithCancel ct cons parallelResults sequenced
        return List.sortBy (fun (t,_) ->
                  List.tryFindIndex (LanguagePrimitives.PhysicalEquality t) tests
               ) results
      }

  /// Evaluates tests.
  let evalTests config test =
    evalTestsWithCancel CancellationToken.None config test false

  let evalTestsSilent test =
    let config =
      { ExpectoConfig.defaultConfig with
          parallel = false
          verbosity = LogLevel.Fatal
          printer = TestPrinters.silent
      }
    evalTests config test

  /// Runs tests, returns error code
  let runEvalWithCancel (ct:CancellationToken) (config:ExpectoConfig) test =
    async {
      do! config.printer.beforeRun test

      let progressStarted = false
        //if config.noSpinner then false
        //else
        //  ProgressIndicator.text "Expecto Running... "
        //  ProgressIndicator.start()


      let w = Stopwatch.StartNew()
      let! results = evalTestsWithCancel ct config test progressStarted
      w.Stop()
      let testSummary : TestRunSummary = {
        results = results
        duration = w.Elapsed
        maxMemory = 0L
        memoryLimit = 0L
        timedOut = []
      }
      do! config.printer.summary config testSummary

      //if progressStarted then
      //  ProgressIndicator.stop ()

      //ANSIOutputWriter.close ()

      return testSummary.errorCode
    }

  /// Runs tests, returns error code
  let runEval config test =
    runEvalWithCancel CancellationToken.None config test

  let runStressWithCancel (ct: CancellationToken) (config:ExpectoConfig) test =
    async {
      do! config.printer.beforeRun test

      //let progressStarted =
      //  if config.noSpinner then false
      //  else
      //    ProgressIndicator.text "Expecto Running... "
      //    ProgressIndicator.start()

      let tests =
        Test.toTestCodeList test
        |> List.filter (fun t -> Option.isNone t.shouldSkipEvaluation)

      let memoryLimit =
        config.stressMemoryLimit * 1024.0 * 1024.0 |> int64

      let evalTestAsync test =
        execTestAsync ct config test |> Async.map (addFst test)

      let rand = Random()

      let randNext tests =
        let next = List.length tests |> rand.Next
        List.item next tests

      let totalTicks =
        config.stress.Value.TotalSeconds * float Stopwatch.Frequency
        |> int64

      let finishTime =
        lazy
        totalTicks |> (+) (Stopwatch.GetTimestamp())

      let asyncRun foldRunner (runningTests: ResizeArray<_>,
                               results,
                               maxMemory) =
        let cancel = new CancellationTokenSource()

        let folder (runningTests: ResizeArray<_>, results: ResizeMap<_,_>, maxMemory)
                   (test, result : TestSummary) =

          runningTests.Remove test |> ignore

          results.[test] <-
            match results.TryGetValue test with
            | true, existing ->
              existing + (result.result, result.meanDuration)
            | false, _ ->
              result

          let maxMemory = GC.GetTotalMemory false |> max maxMemory

          if maxMemory > memoryLimit then
            cancel.Cancel()

          runningTests, results, maxMemory

        Async.Start(async {
          let finishMilliseconds =
            max (finishTime.Value - Stopwatch.GetTimestamp()) 0L
            * 1000L / Stopwatch.Frequency
          let timeout =
            int finishMilliseconds + int config.stressTimeout.TotalMilliseconds
          do! Async.Sleep timeout
          cancel.Cancel()
        }, cancel.Token)

        Seq.takeWhile (fun test ->
          let now = Stopwatch.GetTimestamp()

          //if progressStarted then
          //  100 - int((finishTime.Value - now) * 100L / totalTicks)
          //  |> Percent
          //  |> ProgressIndicator.update

          if now < finishTime.Value
              && not ct.IsCancellationRequested then
            runningTests.Add test
            true
          else
            false )
        >> Seq.map evalTestAsync
        >> foldRunner cancel.Token folder (runningTests,results,maxMemory)

      let initial = ResizeArray(), ResizeMap(), GC.GetTotalMemory false

      let w = Stopwatch.StartNew()

      let! runningTests,results,maxMemory =
        if not config.``parallel`` ||
           config.parallelWorkers = 1 ||
           List.forall (fun t -> t.sequenced=Synchronous) tests then

          Seq.initInfinite (fun _ -> randNext tests)
          |> Seq.append tests
          |> asyncRun Async.foldSequentiallyWithCancel initial
        else
          List.filter (fun t -> t.sequenced=Synchronous) tests
          |> asyncRun Async.foldSequentiallyWithCancel initial
          |> Async.bind (fun (runningTests,results,maxMemory) ->
               if maxMemory > memoryLimit ||
                  Stopwatch.GetTimestamp() > finishTime.Value then
                 async.Return (runningTests,results,maxMemory)
               else
                 let parallel =
                   List.filter (fun t -> t.sequenced<>Synchronous) tests
                 Seq.initInfinite (fun _ -> randNext parallel)
                 |> Seq.append parallel
                 |> Seq.filter (fun test ->
                      let s = test.sequenced
                      s=InParallel ||
                      not(Seq.exists (fun t -> t.sequenced=s) runningTests)
                    )
                 |> asyncRun
                      (Async.foldParallelWithCancel (numberOfWorkers true config))
                      (runningTests,results,maxMemory)
             )

      w.Stop()

      let testSummary : TestRunSummary = { 
        results = 
            results
            |> Seq.map (fun kv -> kv.Key,kv.Value)
            |> List.ofSeq
        duration = w.Elapsed
        maxMemory = maxMemory
        memoryLimit = memoryLimit
        timedOut = List.ofSeq runningTests }

      do! config.printer.summary config testSummary

      //if progressStarted then
      //  ProgressIndicator.stop()

      //ANSIOutputWriter.close()

      return testSummary.errorCode
    }

  let runStress config test =
    runStressWithCancel CancellationToken.None config test

  let testFromMember (mi: MemberInfo) : Test option =
    let inline unboxTest v =
      if isNull v then
        "Test is null. Assembly may not be initialized. Consider adding an [<EntryPoint>] or making it a library/classlib."
        |> NullTestDiscoveryException |> raise
      else unbox v
    let getTestFromMemberInfo focusedState =
      match box mi with
      | :? FieldInfo as m ->
        if m.FieldType = typeof<Test> then Some(focusedState, m.GetValue(null) |> unboxTest)
        else None
      | :? MethodInfo as m ->
        if m.ReturnType = typeof<Test> then Some(focusedState, m.Invoke(null, null) |> unboxTest)
        else None
      | :? PropertyInfo as m ->
        if m.PropertyType = typeof<Test> then Some(focusedState, m.GetValue(null, null) |> unboxTest)
        else None
      | _ -> None
    mi.MatchTestsAttributes ()
    |> Option.map getTestFromMemberInfo
    |> function
    | Some (Some (focusedState, test)) -> Some (Test.translateFocusState focusedState test)
    | _ -> None

  let listToTestListOption =
    function
    | [] -> None
    | x -> Some (TestList (x, Normal))

  let testFromType =
    let asMembers x = Seq.map (fun m -> m :> MemberInfo) x
    let bindingFlags = BindingFlags.Public ||| BindingFlags.Static
    fun (t: Type) ->
      [ t.GetTypeInfo().GetMethods bindingFlags |> asMembers
        t.GetTypeInfo().GetProperties bindingFlags |> asMembers
        t.GetTypeInfo().GetFields bindingFlags |> asMembers ]
      |> Seq.collect id
      |> Seq.choose testFromMember
      |> Seq.toList
      |> listToTestListOption

  // If the test function we've found doesn't seem to be in the test assembly, it's
  // possible we're looking at an FsCheck 'testProperty' style check. In that case,
  // the function of interest (i.e., the one in the test assembly, and for which we
  // might be able to find corresponding source code) is referred to in a field
  // of the function object.
  let isFsharpFuncType t =
    let baseType =
      let rec findBase (t:Type) =
        if t.GetTypeInfo().BaseType |> isNull || t.GetTypeInfo().BaseType = typeof<obj> then
          t
        else
          findBase (t.GetTypeInfo().BaseType)
      findBase t
    baseType.GetTypeInfo().IsGenericType && baseType.GetTypeInfo().GetGenericTypeDefinition() = typedefof<FSharpFunc<unit, unit>>

  let getFuncTypeToUse (testFunc:unit->unit) (asm:Assembly) =
    let t = testFunc.GetType()
    if t.GetTypeInfo().Assembly.FullName = asm.FullName then
      t
    else
      let nestedFunc =
        t.GetTypeInfo().GetFields()
        |> Seq.tryFind (fun f -> isFsharpFuncType f.FieldType)
      match nestedFunc with
      | Some f -> f.GetValue(testFunc).GetType()
      | None -> t

  let getMethodName asm testCode =
    match testCode with
    | Sync test ->
      let t = getFuncTypeToUse test asm
      let m = t.GetTypeInfo().GetMethods () |> Seq.find (fun m -> (m.Name = "Invoke") && (m.DeclaringType = t))
      (t.FullName, m.Name)
    | SyncWithCancel _ ->
      ("Unknown SyncWithCancel", "Unknown SyncWithCancel")
    | Async _ | AsyncFsCheck _ ->
      ("Unknown Async", "Unknown Async")

  // Ported from
  // https://github.com/adamchester/expecto-adapter/blob/885fc9fff0/src/Expecto.VisualStudio.TestAdapter/SourceLocation.fs
  let getSourceLocation (asm: Assembly) className methodName =
    let lineNumberIndicatingHiddenLine = 0xfeefee
    let getEcma335TypeName (clrTypeName:string) = clrTypeName.Replace("+", "/")

    let types =
      let readerParams = new ReaderParameters( ReadSymbols = true )
      let moduleDefinition = ModuleDefinition.ReadModule(asm.Location, readerParams)

      seq { for t in moduleDefinition.GetTypes() -> (t.FullName, t) }
      |> Map.ofSeq

    let getMethods typeName =
      match types.TryFind (getEcma335TypeName typeName) with
      | Some t -> Some (t.Methods)
      | _ -> None

    let getFirstOrDefaultSequencePoint (m:MethodDefinition) =
      m.Body.Instructions
      |> Seq.tryPick (fun i ->
        let sp = m.DebugInformation.GetSequencePoint i
        if isNull sp |> not && sp.StartLine <> lineNumberIndicatingHiddenLine then
          Some sp else None)

    match getMethods className with
    | None -> SourceLocation.empty
    | Some methods ->
      let candidateSequencePoints =
        methods
        |> Seq.where (fun m -> m.Name = methodName)
        |> Seq.choose getFirstOrDefaultSequencePoint
        |> Seq.sortBy (fun sp -> sp.StartLine)
        |> Seq.toList
      match candidateSequencePoints with
      | [] -> SourceLocation.empty
      | xs -> {sourcePath = xs.Head.Document.Url ; lineNumber = xs.Head.StartLine}

  //val apply : f:(TestCode * FocusState * SourceLocation -> TestCode * FocusState * SourceLocation) -> _arg1:Test -> Test
  let getLocation (asm:Assembly) code =
    let typeName, methodName = getMethodName asm code
    try
      getSourceLocation asm typeName methodName
    with :? IO.FileNotFoundException ->
      SourceLocation.empty

  /// Scan filtered tests marked with TestsAttribute from an assembly
  let testFromAssemblyWithFilter typeFilter (a: Assembly) =
    a.GetExportedTypes()
    |> Seq.filter typeFilter
    |> Seq.choose testFromType
    |> Seq.toList
    |> listToTestListOption

  /// Scan tests marked with TestsAttribute from an assembly
  let testFromAssembly = testFromAssemblyWithFilter (fun _ -> true)

  /// Scan tests marked with TestsAttribute from entry assembly
  let testFromThisAssembly () = testFromAssembly (Assembly.GetEntryAssembly())

  /// When the failOnFocusedTests switch is activated this function that no
  /// focused tests exist.
  ///
  /// Returns true if the check passes, otherwise false.
  let passesFocusTestCheck (config:ExpectoConfig) tests =
    let isFocused : FlatTest -> _ = function t when t.state = Focused -> true | _ -> false
    let focused = Test.toTestCodeList tests |> List.filter isFocused
    if focused.Length = 0 then true
    else
      if config.verbosity <> LogLevel.Fatal then
        logger.logWithAck LogLevel.Error (
          eventX "It was requested that no focused tests exist, but yet there are {count} focused tests found."
          >> setField "count" focused.Length)
        |> Async.StartImmediate
        //ANSIOutputWriter.flush ()
      false

[<AutoOpen; Extension>]
module Tests =
  open Impl
  open Helpers
  open Expecto.Logging
  open FSharp.Control.Tasks.CopiedDoNotReference.V2

  let mutable private afterRunTestsList = []
  let private afterRunTestsListLock = obj()
  /// Add a function that will be called after all testing has finished.
  let afterRunTests f =
    lock afterRunTestsListLock (fun () ->
        afterRunTestsList <- f :: afterRunTestsList
      )
  let internal afterRunTestsInvoke() =
    lock afterRunTestsListLock (fun () ->
      let failures =
        List.rev afterRunTestsList
        |> List.choose (fun f ->
          try
            f()
            None
          with e -> Some e
        )
      match failures with
      | [] -> ()
      | l -> List.toArray l |> AggregateException |> raise
    )
  Console.CancelKeyPress |> Event.add (fun _ -> afterRunTestsInvoke())

  /// Expecto atomic printfn shadow function
  let printfn format =
    Printf.ksprintf (fun s ->
        Console.Write(s.PadRight 40 + "\n")
      ) format

  /// Expecto atomic eprintfn shadow function
  let eprintfn format =
    Printf.ksprintf (fun s ->
      Console.Error.Write(s.PadRight 40 + "\n")
    ) format

  /// The full name of the currently running test
  let testName() = TestNameHolder.Name

  /// Fail this test
  let inline failtest msg = raise <| AssertException msg
  /// Fail this test
  let inline failtestf fmt = Printf.ksprintf (AssertException >> raise) fmt
  /// Fail this test
  let inline failtestNoStack msg = raise <| FailedException msg
  /// Fail this test
  let inline failtestNoStackf fmt = Printf.ksprintf (FailedException >> raise) fmt

  /// Skip this test
  let inline skiptest msg = raise <| IgnoreException msg
  /// Skip this test
  let inline skiptestf fmt = Printf.ksprintf (IgnoreException >> raise) fmt

  /// Builds a list/group of tests that will be ignored by Expecto if exists
  /// focused tests and none of the parents is focused
  let inline testList name tests = TestLabel(name, TestList (tests, Normal), Normal)

  /// Builds a list/group of tests that will make Expecto to ignore other unfocused tests
  let inline ftestList name tests = TestLabel(name, TestList (tests, Focused), Focused)
  /// Builds a list/group of tests that will be ignored by Expecto
  let inline ptestList name tests = TestLabel(name, TestList (tests, Pending), Pending)

  /// Labels the passed test with a text segment. In Expecto, tests are slash-separated (`/`), so this wraps the passed
  /// tests in such a label. Useful when you don't want lots of indentation in your tests (the code would become hard to
  /// modify and read, due to all the whitespace), and you want to do `testList "..." [ ] |> testLabel "api"`.
  let inline testLabel name test = TestLabel(name, test, Normal)

  /// Builds a test case that will be ignored by Expecto if exists focused
  /// tests and none of the parents is focused
  let inline testCase name test = TestLabel(name, TestCase (Sync test,Normal), Normal)
  /// Builds a test case with a CancellationToken that can be check for cancel
  let inline testCaseWithCancel name test = TestLabel(name, TestCase (SyncWithCancel test,Normal), Normal)
  /// Builds an async test case
  let inline testCaseAsync name test = TestLabel(name, TestCase (Async test,Normal), Normal)
  /// Builds a test case that will make Expecto to ignore other unfocused tests
  let inline ftestCase name test = TestLabel(name, TestCase (Sync test, Focused), Focused)
  /// Builds a test case with cancel that will make Expecto to ignore other unfocused tests
  let inline ftestCaseWithCancel name test = TestLabel(name, TestCase (SyncWithCancel test, Focused), Focused)
  /// Builds an async test case that will make Expecto to ignore other unfocused tests
  let inline ftestCaseAsync name test = TestLabel(name, TestCase (Async test, Focused), Focused)
  /// Builds a test case that will be ignored by Expecto
  let inline ptestCase name test = TestLabel(name, TestCase (Sync test, Pending), Pending)
  /// Builds a test case with cancel that will be ignored by Expecto
  let inline ptestCaseWithCancel name test = TestLabel(name, TestCase (SyncWithCancel test, Pending), Pending)
  /// Builds an async test case that will be ignored by Expecto
  let inline ptestCaseAsync name test = TestLabel(name, TestCase (Async test, Pending), Pending)
  /// Test case or list needs to run sequenced. Use for any benchmark code or
  /// for tests using `Expect.isFasterThan`
  let inline testSequenced test = Test.Sequenced (Synchronous,test)
  /// Test case or list needs to run sequenced with other tests in this group.
  let inline testSequencedGroup name test = Test.Sequenced (SynchronousGroup name,test)

  /// Applies a function to a list of values to build test cases
  let inline testFixture setup =
    Seq.map (fun (name, partialTest) ->
      testCase name (setup partialTest))

  /// Applies a value to a list of partial tests
  let inline testParam param =
    Seq.map (fun (name, partialTest) ->
      testCase name (partialTest param))

  /// Test case computation expression builder
  type TestCaseBuilder(name, focusState) =
    member __.TryFinally(f, compensation) =
      try
        f()
      finally
        compensation()
    member __.TryWith(f, catchHandler) =
      try
        f()
      with e -> catchHandler e
    member __.Using(disposable: #IDisposable, f) =
      try
        f disposable
      finally
        match box disposable with
        | :? IDisposable as d when not (isNull d) -> d.Dispose()
        | _ -> ()
    member __.For(sequence, f) =
      for i in sequence do f i
    member __.Combine(f1, f2) = f2(); f1
    member __.Zero() = ()
    member __.Delay f = f
    member __.Run f =
      match focusState with
      | Normal -> testCase name f
      | Focused -> ftestCase name f
      | Pending -> ptestCase name f

  /// Builds a test case
  let inline test name =
    TestCaseBuilder (name, Normal)
  /// Builds a test case that will make Expecto to ignore other unfocused tests
  let inline ftest name =
    TestCaseBuilder (name, Focused)
  /// Builds a test case that will be ignored by Expecto
  let inline ptest name =
    TestCaseBuilder (name, Pending)

  /// Async test case computation expression builder
  type TestAsyncBuilder(name, focusState) =
    member __.Zero() = async.Zero()
    member __.Delay(f) = async.Delay(f)
    member __.Return(x) = async.Return(x)
    member __.ReturnFrom(x) = async.ReturnFrom(x)
    member __.Bind(p1, p2) = async.Bind(p1, p2)
    member __.Using(g, p) = async.Using(g, p)
    member __.While(gd, prog) = async.While(gd, prog)
    member __.For(e, prog) = async.For(e, prog)
    member __.Combine(p1, p2) = async.Combine(p1, p2)
    member __.TryFinally(p, cf) = async.TryFinally(p, cf)
    member __.TryWith(p, cf) = async.TryWith(p, cf)
    member __.Run f =
      match focusState with
      | Normal -> testCaseAsync name f
      | Focused -> ftestCaseAsync name f
      | Pending -> ptestCaseAsync name f

  /// Builds an async test case
  let inline testAsync name =
    TestAsyncBuilder (name, Normal)
  /// Builds an async test case that will make Expecto to ignore other unfocused tests
  let inline ftestAsync name =
    TestAsyncBuilder (name, Focused)
  /// Builds an async test case that will be ignored by Expecto
  let inline ptestAsync name =
    TestAsyncBuilder (name, Pending)

  type TestTaskBuilder(name, focusState) =
    member __.Zero() = task.Zero()
    member __.Delay(f) = task.Delay(f)
    member __.Return(x) = task.Return(x)
    member __.ReturnFrom(x) = task.ReturnFrom(x)
    member __.Bind(p1:Task<'a>, p2:'a->_) = task.Bind(p1, p2)
    member __.Bind(p1:Task, p2:unit->_) = task.Bind(p1, p2)
    member __.Using(g, p) = task.Using(g, p)
    member __.While(gd, prog) = task.While(gd, prog)
    member __.For(e, prog) = task.For(e, prog)
    member __.Combine(p1, p2) = task.Combine(p1, p2)
    member __.TryFinally(p, cf) = task.TryFinally(p, cf)
    member __.TryWith(p, cf) = task.TryWith(p, cf)
    member __.Run f =
      let a = async {
          do! task.Run f |> Async.AwaitTask
      }
      match focusState with
      | Normal -> testCaseAsync name a
      | Focused -> ftestCaseAsync name a
      | Pending -> ptestCaseAsync name a

  /// Builds a task test case
  let inline testTask name =
    TestTaskBuilder (name, Normal)
  /// Builds a task test case that will make Expecto to ignore other unfocused tests
  let inline ftestTask name =
    TestTaskBuilder (name, Focused)
  /// Builds a task test case that will be ignored by Expecto
  let inline ptestTask name =
    TestTaskBuilder (name, Pending)

  /// The default configuration for Expecto.
  let defaultConfig = ExpectoConfig.defaultConfig

  module Args =
    open FSharp.Core

    type Parser<'a> = (string[] * int * int) -> Result<'a,string> * int

    let parseOptions (options:(string * string * Parser<_>) list) (strings:string[]) =
      let rec updateUnknown unknown last length =
        if length = 0 then unknown
        else updateUnknown (strings.[last]::unknown) (last-1) (length-1)
      let rec collect isHelp unknown args paramCount i =
        if i>=0 then
          let currentArg = strings.[i]
          if currentArg = "--help" || currentArg = "-h" || currentArg = "-?" || currentArg = "/?" then
            collect true (updateUnknown unknown (i+paramCount) paramCount) args 0 (i-1)
          else
            match List.tryFind (fst3 >> (=) currentArg) options with
            | Some (option, _, parser) ->
              let arg, unknownCount = parser (strings, i+1, paramCount)
              collect isHelp
                (updateUnknown unknown (i+paramCount) unknownCount)
                (Result.mapError (fun i -> option + " " + i) arg::args) 0 (i-1)
            | None -> collect isHelp unknown args (paramCount+1) (i-1)
        else
          let unknown =
            match updateUnknown unknown (paramCount-1) paramCount with
            | [] -> None
            | l -> String.Join(" ","unknown options:" :: l) |> Some
          match isHelp, Result.sequence args, unknown with
          | false, Ok os, None -> Ok(List.rev os)
          | true, Ok _, None -> Error []
          | _, Ok _, Some u -> Error [u]
          | _, r, None -> r
          | _, Error es, Some u -> List.rev (u::es) |> Error
      collect false [] [] 0 (strings.Length-1)

    let [<Obsolete "Use 'deprecated', not 'depricated">] depricated = "Deprecated"
    let deprecated = "Deprecated"

    let usage commandName (options: (string * string * Parser<_>) list) =
      let sb = Text.StringBuilder("Usage: ")
      let add (text:string) = sb.Append(text) |> ignore
      add commandName
      add " [options]\n\nOptions:\n"
      let maxLength =
        options |> Seq.map (fun (s,_,_) -> s.Length) |> Seq.max
      ["--help","Show this help message."]
      |> Seq.append (Seq.map (fun (s,d,_) -> s,d) options)
      |> Seq.where (snd >> (<>)deprecated)
      |> Seq.iter (fun (s,d) ->
        add "  "
        add (s.PadRight maxLength)
        add "  "
        add d
        add "\n"
      )
      sb.ToString()

    let none case : Parser<_> =
      fun (_,_,l) -> Ok case, l

    let string case : Parser<_> =
      fun (ss,i,l) ->
        if l>0 then Ok(case ss.[i]), l-1
        else Error "requires a parameter", 0

    let list (parser:_->Parser<_>) case : Parser<_> =
      fun (ss,i,l) ->
        [i..i+l-1]
        |> Result.traverse (fun j -> parser id (ss,j,1) |> fst)
        |> Result.map (fun l -> case(List.rev l))
        |> Result.mapError (fun i -> String.Join(", ", i))
        , 0

    let inline private parseWith tryParseFn case: Parser<'a> =
      fun (args, i, l) ->
        if l = 0 then Error "requires a parameter", 0
        else
          match tryParseFn args.[i] with
          | Some i -> Ok(case i), l-1
          | None -> Error("Cannot parse parameter '" + args.[i] + "'"), l-1


    let inline parse case: Parser<'a> = parseWith tryParse case
    let inline number case: Parser<'a> = parseWith tryParseNumber case


  [<ReferenceEquality>]
  type SummaryHandler =
    | SummaryHandler of (TestRunSummary -> unit)

  /// The CLI arguments are the parameters that are possible to send to Expecto
  /// and change the runner's behaviour.
  type CLIArguments =
    /// Don't run the tests in parallel.
    | Sequenced
    /// Run all tests in parallel (default).
    | Parallel
    /// Set the number of parallel workers (defaults to the number of logical processors).
    | Parallel_Workers of int
    /// Set FsCheck maximum number of tests (default: 100).
    | FsCheck_Max_Tests of int
    /// Set FsCheck start size (default: 1).
    | FsCheck_Start_Size of int
    /// Set FsCheck end size (default: 100 for testing and 10,000 for stress testing).
    | FsCheck_End_Size of int
    /// Run the tests randomly for the given number of minutes.
    | Stress of float
    /// Set the time to wait in minutes after the stress test before reporting as a deadlock (default 5 mins).
    | Stress_Timeout of float
    /// Set the Stress test memory limit in MB to stop the test and report as a memory leak (default 100 MB).
    | Stress_Memory_Limit of float
    /// This will make the test runner fail if focused tests exist.
    | Fail_On_Focused_Tests
    /// Extra verbose printing. Useful to combine with --sequenced.
    | Debug
    /// Set the process name to log under (default: "Expecto").
    | Log_Name of name:string
    /// Filters the list of tests by a hierarchy that's slash (/) separated.
    | Filter of hiera:string
    /// Filters the list of test lists by a given substring.
    | Filter_Test_List of substring:string
    /// Filters the list of test cases by a given substring.
    | Filter_Test_Case of substring:string
    /// Runs only provided list of tests.
    | Run of tests:string list
    /// Don't run tests, but prints out list of tests instead.
    | List_Tests
    /// Print out a summary after all tests are finished.
    | Summary
    /// Print out a summary after all tests are finished including their source code location.
    | Summary_Location
    /// Print out version information.
    | Version
    /// Depricated
    | My_Spirit_Is_Weak
    /// Allow duplicate test names.
    | Allow_Duplicate_Names
    /// Disable the spinner progress update.
    | No_Spinner
    // Set the level of colours to use. Can be 0, 8 (default) or 256.
    | Colours of int
    /// Adds a test printer.
    | Printer of TestPrinters
    /// Sets the verbosity level.
    | Verbosity of LogLevel
    /// Append a summary handler.
    | Append_Summary_Handler of SummaryHandler

  let options = [
      "--sequenced", "Don't run the tests in parallel.", Args.none Sequenced
      "--parallel", "Run all tests in parallel (default).", Args.none Parallel
      "--parallel-workers", "Set the number of parallel workers (defaults to the number of logical processors).", Args.number Parallel_Workers
      "--stress", "Run the tests randomly for the given number of minutes.", Args.number Stress
      "--stress-timeout", "Set the time to wait in minutes after the stress test before reporting as a deadlock (default 5 mins).", Args.number Stress_Timeout
      "--stress-memory-limit", "Set the Stress test memory limit in MB to stop the test and report as a memory leak (default 100 MB).", Args.number Stress_Memory_Limit
      "--fail-on-focused-tests", "This will make the test runner fail if focused tests exist.", Args.none Fail_On_Focused_Tests
      "--debug", "Extra verbose printing. Useful to combine with --sequenced.", Args.none Debug
      "--log-name", "Set the process name to log under (default: \"Expecto\").", Args.string Log_Name
      "--filter", "Filters the list of tests by a hierarchy that's slash (/) separated.", Args.string Filter
      "--filter-test-list", "Filters the list of test lists by a given substring.", Args.string Filter_Test_List
      "--filter-test-case", "Filters the list of test cases by a given substring.", Args.string Filter_Test_Case
      "--run", "Runs only provided list of tests.", Args.list Args.string Run
      "--list-tests", "Don't run tests, but prints out list of tests instead.", Args.none List_Tests
      "--summary", "Print out a summary after all tests are finished.", Args.none Summary
      "--version", "Print out version information.", Args.none Version
      "--summary-location", "Print out a summary after all tests are finished including their source code location.", Args.none Summary_Location
      "--fscheck-max-tests", "Set FsCheck maximum number of tests (default: 100).", Args.number FsCheck_Max_Tests
      "--fscheck-start-size", "Set FsCheck start size (default: 1).", Args.number FsCheck_Start_Size
      "--fscheck-end-size", "Set FsCheck end size (default: 100 for testing and 10,000 for stress testing).", Args.number FsCheck_End_Size
      "--my-spirit-is-weak", Args.deprecated, Args.none My_Spirit_Is_Weak
      "--allow-duplicate-names", "Allow duplicate test names.", Args.none Allow_Duplicate_Names
      "--colours", "Set the level of colours to use. Can be 0, 8 (default) or 256.", Args.number Colours
      "--no-spinner", "Disable the spinner progress update.", Args.none No_Spinner
  ]

  type FillFromArgsResult =
    | ArgsRun of ExpectoConfig
    | ArgsList of ExpectoConfig
    | ArgsVersion of ExpectoConfig
    | ArgsUsage of usage:string * errors:string list

  let private getTestList (s:string) =
    let all = s.Split('/')
    match all with
    | [||] | [|_|] -> [||]
    | xs -> xs.[0..all.Length-2]

  let private getTestCase (s:string) =
    let i = s.LastIndexOf('/')
    if i= -1 then s else s.Substring(i+1)

  let private foldCLIArgumentToConfig = function
    | Sequenced -> fun o -> { o with ExpectoConfig.parallel = false }
    | Parallel -> fun o -> { o with parallel = true }
    | Parallel_Workers n -> fun o -> { o with parallelWorkers = n }
    | Stress n -> fun o  -> {o with
                                stress = TimeSpan.FromMinutes n |> Some
                                printer = TestPrinters.stressPrinter }
    | Stress_Timeout n -> fun o -> { o with stressTimeout = TimeSpan.FromMinutes n }
    | Stress_Memory_Limit n -> fun o -> { o with stressMemoryLimit = n }
    | Fail_On_Focused_Tests -> fun o -> { o with failOnFocusedTests = true }
    | Debug -> fun o -> { o with verbosity = LogLevel.Debug }
    | Log_Name name -> fun o -> { o with logName = Some name }
    | Filter hiera -> fun o -> {o with filter = Test.filter (fun s -> s.StartsWith hiera )}
    | Filter_Test_List name ->  fun o -> {o with filter = Test.filter (fun s -> s |> getTestList |> Array.exists(fun s -> s.Contains name )) }
    | Filter_Test_Case name ->  fun o -> {o with filter = Test.filter (fun s -> s |> getTestCase |> fun s -> s.Contains name )}
    | Run tests -> fun o -> {o with filter = Test.filter (fun s -> tests |> List.exists ((=) s) )}
    | List_Tests -> id
    | Summary -> fun o -> {o with printer = TestPrinters.summaryPrinter o.printer}
    | Version -> id
    | Summary_Location -> fun o -> {o with printer = TestPrinters.summaryWithLocationPrinter o.printer}
    | FsCheck_Max_Tests n -> fun o -> {o with fsCheckMaxTests = n }
    | FsCheck_Start_Size n -> fun o -> {o with fsCheckStartSize = n }
    | FsCheck_End_Size n -> fun o -> {o with fsCheckEndSize = Some n }
    | My_Spirit_Is_Weak -> id
    | Allow_Duplicate_Names -> fun o -> { o with allowDuplicateNames = true }
    | No_Spinner -> fun o -> { o with noSpinner = true }
    | Colours i -> fun o -> { o with colour =
                                      if i >= 256 then Colour256
                                      elif i >= 8 then Colour8
                                      else Colour0
                  }
    | Printer p -> fun o -> { o with printer = p }
    | Verbosity l -> fun o -> { o with verbosity = l }
    | Append_Summary_Handler (SummaryHandler h) -> fun o -> o.appendSummaryHandler h

  [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
  module ExpectoConfig =

    let expectoVersion = "8.13.1"

    /// Parses command-line arguments into a config. This allows you to
    /// override the config from the command line, rather than having
    /// to go into the compiled code to change how they are being run.
    /// Also checks if tests should be run or only listed
    let fillFromArgs baseConfig args =
      match Args.parseOptions options args with
      | Ok cliArguments ->
          let config =
            Seq.fold (fun s a -> foldCLIArgumentToConfig a s) baseConfig cliArguments
          if List.contains List_Tests cliArguments then
            ArgsList config
          elif List.contains Version cliArguments then
            ArgsVersion config
          else
            ArgsRun config
      | Result.Error errors ->
        let commandName =
          Environment.GetCommandLineArgs().[0]
          |> IO.Path.GetFileName
          |> fun f -> if f.EndsWith(".dll") then "dotnet " + f else f
        ArgsUsage (Args.usage commandName options, errors)

  /// Prints out names of all tests for given test suite.
  let listTests test =
    Test.toTestCodeList test
    |> Seq.iter (fun t -> printfn "%s" t.name)

  /// Prints out names of all tests for given test suite.
  let duplicatedNames test =
    Test.toTestCodeList test
    |> Seq.toList
    |> List.groupBy (fun t -> t.name)
    |> List.choose (function
        | _, x :: _ :: _ -> Some x.name
        | _ -> None
    )
  /// Runs tests with the supplied config.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsWithCancel (ct:CancellationToken) config (tests:Test) =
    printfn "Running Expecto with FakeExpecto!!!"
    //ANSIOutputWriter.setColourLevel config.colour
    Global.initialiseIfDefault
      { Global.defaultConfig with
          getLogger = fun name ->
            LiterateConsoleTarget(
              name, config.verbosity,
              consoleSemaphore = obj()) :> Logger
      }

    //let config = { config with
    //                printer = TestPrinters.silent }

    config.logName |> Option.iter setLogName
    if config.failOnFocusedTests && passesFocusTestCheck config tests |> not then
      1
    else
      let tests = config.filter tests
      let duplicates = lazy duplicatedNames tests
      if config.allowDuplicateNames || List.isEmpty duplicates.Value then
        let retCode =
          match config.stress with
          | None -> runEvalWithCancel ct config tests |> Async.RunSynchronously
          | Some _ -> runStressWithCancel ct config tests |> Async.RunSynchronously
        afterRunTestsInvoke()
        retCode
      else
        sprintf "Found duplicated test names, these names are: %A" duplicates.Value
        |> config.printer.info
        |> Async.RunSynchronously
        //ANSIOutputWriter.close()
        1
  /// Runs tests with the supplied config.
  /// Returns 0 if all tests passed, otherwise 1
  let runTests config tests =
    runTestsWithCancel CancellationToken.None config tests

  /// Runs all given tests with the supplied command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsWithArgsAndCancel (ct:CancellationToken) config args tests =
    match ExpectoConfig.fillFromArgs config args with
    | ArgsUsage (usage, errors) ->
      if not (List.isEmpty errors) then
        printfn "ERROR: %s\n" (String.Join(" ",errors))
      printfn "EXPECTO! v%s\n\n%s" ExpectoConfig.expectoVersion usage
      if List.isEmpty errors then 0 else 1
    | ArgsList config ->
      config.filter tests
      |> listTests
      0
    | ArgsRun config ->
      runTestsWithCancel ct config tests
    | ArgsVersion config ->
      printfn "EXPECTO! v%s\n" ExpectoConfig.expectoVersion
      runTestsWithCancel ct config tests

  /// Runs all given tests with the supplied typed command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsWithCLIArgsAndCancel (ct:CancellationToken) cliArgs args tests =
    let config =
      Seq.fold (fun s a -> foldCLIArgumentToConfig a s)
        ExpectoConfig.defaultConfig cliArgs
    runTestsWithArgsAndCancel ct config args tests

  /// Runs all given tests with the supplied command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsWithArgs config args tests =
    runTestsWithArgsAndCancel CancellationToken.None config args tests

  /// Runs all given tests with the supplied typed command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsWithCLIArgs cliArgs args tests =
    runTestsWithCLIArgsAndCancel CancellationToken.None cliArgs args tests

  /// Runs tests in this assembly with the supplied command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsInAssemblyWithCancel (ct:CancellationToken) config args =
    let config = { config with locate = getLocation (Assembly.GetEntryAssembly()) }
    testFromThisAssembly ()
    |> Option.orDefault (TestList ([], Normal))
    |> runTestsWithArgsAndCancel ct config args

  /// Runs tests in this assembly with the supplied command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsInAssemblyWithCLIArgsAndCancel (ct:CancellationToken) cliArgs args =
    let config = { ExpectoConfig.defaultConfig
                    with locate = getLocation (Assembly.GetEntryAssembly()) }
    let config = Seq.fold (fun s a -> foldCLIArgumentToConfig a s) config cliArgs
    let tests = testFromThisAssembly() |> Option.orDefault (TestList ([], Normal))
    runTestsWithArgsAndCancel ct config args tests

  /// Runs tests in this assembly with the supplied command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsInAssembly config args =
    runTestsInAssemblyWithCancel CancellationToken.None config args

  /// Runs tests in this assembly with the supplied command-line options.
  /// Returns 0 if all tests passed, otherwise 1
  let runTestsInAssemblyWithCLIArgs cliArgs args =
    runTestsInAssemblyWithCLIArgsAndCancel CancellationToken.None cliArgs args
