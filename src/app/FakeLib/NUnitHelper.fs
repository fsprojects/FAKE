[<AutoOpen>]
/// Contains tasks to run [NUnit](http://www.nunit.org/) unit tests.
module Fake.NUnitHelper

open System
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq

/// [omit]
let inline imp arg = ( ^a : (static member op_Implicit : ^b -> ^a) arg)
let inline private (?) (elem: XElement) attr = elem.Attribute(imp attr).Value
let inline private attr attr value (elem: XElement) = elem.SetAttributeValue (imp attr, value); elem
let inline private elem name = XElement (imp name : XName)

/// [omit]
let GetTestAssemblies (xDoc : XDocument) =
    xDoc.Descendants()
    |> Seq.filter (fun el -> el.Name = (imp "test-suite") && el?``type`` = "Assembly")
    |> Seq.toList

/// Used by the NUnitParallel helper, can also be used to merge test results
/// from multiple calls to the normal NUnit helper.
module private NUnitMerge =
    type ResultSummary = 
        { Total: int
          Errors: int
          Failures: int
          NotRun: int
          Inconclusive: int
          Ignored: int
          Skipped: int
          Invalid: int
          DateTime: DateTime }
        static member ofXDoc (xDoc: XDocument) = 
            let tr = xDoc.Element (imp "test-results")
            { Total = int tr ? total
              Errors = int tr ? errors
              Failures = int tr ? failures
              NotRun = int tr ? ``not-run``
              Inconclusive = int tr ? inconclusive
              Ignored = int tr ? ignored
              Skipped = int tr ? skipped
              Invalid = int tr ? invalid
              DateTime = DateTime.Parse (sprintf "%s %s" tr ? date tr ? time) }
        static member toXElement res =
            elem "test-results"
            |> attr "name" "Merged results" 
            |> attr "total" res.Total
            |> attr "errors" res.Errors
            |> attr "failures" res.Failures
            |> attr "not-run" res.NotRun
            |> attr "inconclusive" res.Inconclusive 
            |> attr "skipped" res.Skipped
            |> attr "ignored" res.Ignored
            |> attr "invalid" res.Invalid
            |> attr "date" (res.DateTime.ToString("yyyy-MM-dd")) 
            |> attr "time" (res.DateTime.ToString("HH:mm:ss"))
        static member append r1 r2 = 
            { r1 with Total = r1.Total + r2.Total
                      Errors = r1.Errors + r2.Errors
                      Failures = r1.Failures + r2.Failures
                      NotRun = r1.NotRun + r2.NotRun
                      Inconclusive = r1.Inconclusive + r2.Inconclusive
                      Ignored = r1.Ignored + r2.Ignored
                      Skipped = r1.Skipped + r2.Skipped
                      Invalid = r1.Invalid + r2.Invalid
                      DateTime = Seq.min [r1.DateTime; r2.DateTime] }

    type Environment = 
        { NUnitVersion: string
          ClrVersion: string
          OSVersion: string
          Platform: string
          Cwd: string
          MachineName: string
          User: string
          UserDomain: string }
        static member ofXDoc (xDoc: XDocument) = 
            let env = xDoc.Element(imp "test-results").Element(imp "environment")
            { NUnitVersion = env ? ``nunit-version``
              ClrVersion = env ? ``clr-version``
              OSVersion = env ? ``os-version``
              Platform = env ? platform
              Cwd = env ? cwd
              MachineName = env ? ``machine-name``
              User = env ? user
              UserDomain = env ? ``user-domain`` }
        static member toXElement env =
            elem "environment"
            |> attr "nunit-version" env.NUnitVersion
            |> attr "clr-version" env.ClrVersion
            |> attr "os-version" env.OSVersion
            |> attr "platform" env.Platform
            |> attr "cwd" env.Cwd
            |> attr "machine-name" env.MachineName
            |> attr "user" env.User 
            |> attr "user-domain" env.UserDomain

    type Culture = 
        { CurrentCulture: string
          CurrentUICulture: string }
        static member ofXDoc (xDoc: XDocument) = 
            let culture = xDoc.Element(imp "test-results").Element (imp "culture-info")
            { CurrentCulture = culture ? ``current-culture``
              CurrentUICulture = culture ? ``current-uiculture`` }
        static member toXElement culture =
            elem "culture-info"
            |> attr "current-culture" culture.CurrentCulture
            |> attr "current-uiculture" culture.CurrentUICulture

    type Doc = 
        { Doc: XDocument
          Summary: ResultSummary
          Env: Environment
          Culture: Culture
          Assemblies: XElement list }
        static member ofXDoc doc =  
            { Doc = doc
              Summary = ResultSummary.ofXDoc doc
              Env = Environment.ofXDoc doc
              Culture = Culture.ofXDoc doc
              Assemblies = GetTestAssemblies doc }
        static member append doc1 doc2 =
            // Sanity check!
            if doc1.Env <> doc2.Env || doc1.Culture <> doc2.Culture then 
                traceImportant "Unmatched environment and/or cultures detected: some of theses results files are not from the same test run."
            { doc1 with Summary = ResultSummary.append doc2.Summary doc1.Summary; Assemblies = doc2.Assemblies @ doc1.Assemblies }

    let foldAssemblyToProjectTuple (result, time, asserts) (assembly : XElement) =
        let outResult =
            match assembly?result, result with
            | "Failure", _ -> "Failure" 
            | "Inconclusive", "Success" -> "Inconclusive"
            | _ -> result
        outResult, time + double assembly?time, asserts + int assembly?asserts

    let TestProjectSummary assemblies =
        assemblies |> List.fold foldAssemblyToProjectTuple ("Success", 0.0, 0)

    let createTestProjectNode assemblies =
        let result, time, asserts = TestProjectSummary assemblies
        let projectEl = 
            elem "test-suite"
            |> attr "type" "Test Project"
            |> attr "name" ""
            |> attr "executed" "True"
            |> attr "result" result
            |> attr "time" time
            |> attr "asserts" asserts
        let results = elem "results"
        results.Add (Seq.toArray assemblies)
        projectEl.Add results
        projectEl
           
    let getXDocs directory filter =
        Directory.GetFiles(directory, filter, SearchOption.AllDirectories)
        |> Array.toList
        |> List.map (fun fileName -> XDocument.Parse(File.ReadAllText(fileName)))

    /// Merges non-empty list of test result XDocuments into a single XElement
    let mergeXDocs xDocs : XElement = 
        xDocs
        |> List.map Doc.ofXDoc
        |> List.reduce Doc.append
        |> fun merged -> 
             let res = ResultSummary.toXElement merged.Summary
             res.Add
                [Environment.toXElement merged.Env
                 Culture.toXElement merged.Culture
                 createTestProjectNode merged.Assemblies]
             res

    let writeMergedNunitResults (directory, filter, outfile) =
        getXDocs directory filter
        |> mergeXDocs
        |> sprintf "%O"
        |> WriteStringToFile false outfile 

/// Returns whether all tests in the given test result have succeeded
let AllSucceeded xDocs =
    xDocs
    |> Seq.map GetTestAssemblies
    |> Seq.concat
    |> Seq.map (fun assembly -> assembly ? result)
    |> Seq.map ((<>) "Failure")
    |> Seq.reduce (&&)

/// Option which allow to specify if a NUnit error should break the build.
type NUnitErrorLevel =
/// This option instructs FAKE to break the build if NUnit reports an error. (Default)
| Error
/// With this option set, no exception is thrown if a test is broken.
| DontFailBuild

/// Parameter type for NUnit.
type NUnitParams ={ 
    IncludeCategory : string
    ExcludeCategory : string
    ToolPath : string
    ToolName : string
    TestInNewThread : bool
    OutputFile : string
    Out : string
    ErrorOutputFile : string
    Framework : string
    ShowLabels : bool
    WorkingDir : string 
    XsltTransformFile : string
    TimeOut : TimeSpan
    DisableShadowCopy : bool
    Domain : string
    ErrorLevel : NUnitErrorLevel }

/// NUnit default parameters. FAKE tries to locate nunit-console.exe in any subfolder.
let NUnitDefaults =
    let toolname = "nunit-console.exe"

    { IncludeCategory = null
      ExcludeCategory = null
      ToolPath = findToolFolderInSubPath toolname (currentDirectory @@ "tools" @@ "Nunit")
      ToolName = toolname
      TestInNewThread = false
      OutputFile = currentDirectory @@ "TestResult.xml"
      Out = null
      ErrorOutputFile = null
      WorkingDir = null
      Framework = null
      ShowLabels = true
      XsltTransformFile = null
      TimeOut = TimeSpan.FromMinutes 5.
      DisableShadowCopy = false
      Domain = null
      ErrorLevel = Error }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let commandLineBuilder parameters assemblies =
    let cl = 
        new StringBuilder()
        |> append "-nologo"
        |> appendIfTrue parameters.DisableShadowCopy "-noshadow" 
        |> appendIfTrue parameters.ShowLabels "-labels" 
        |> appendIfTrue parameters.TestInNewThread "-thread" 
        |> appendFileNamesIfNotNull assemblies
        |> appendIfNotNull parameters.IncludeCategory "-include:"
        |> appendIfNotNull parameters.ExcludeCategory "-exclude:"
        |> appendIfNotNull parameters.XsltTransformFile "-transform:"
        |> appendIfNotNull parameters.OutputFile  "-xml:"
        |> appendIfNotNull parameters.Out "-out:"
        |> appendIfNotNull parameters.Framework  "-framework:"
        |> appendIfNotNull parameters.ErrorOutputFile "-err:"
        |> appendIfNotNull parameters.Domain "-domain:"
    cl.ToString()

/// Tries to detect the working directory as specified in the parameters or via TeamCity settings
/// [omit]
let getWorkingDir parameters =
    Seq.find (fun s -> s <> null && s <> "") [parameters.WorkingDir; environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

/// NUnit console returns negative error codes for errors and sum of failed, ignored and exceptional tests otherwise. 
/// Zero means that all tests passed.
let (|OK|TestsFailed|FatalError|) errorCode =
    match errorCode with
    | 0 -> OK
    | -1 -> FatalError "InvalidArg"
    | -2 -> FatalError "FileNotFound"
    | -3 -> FatalError "FixtureNotFound"
    | -100 -> FatalError "UnexpectedError"
    | x when x < 0 -> FatalError "FatalError"
    | _ -> TestsFailed

/// Runs NUnit on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NUnitParams value.
///  - `assemblies` - Sequence of one or more assemblies containing NUnit unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\Test.*.dll") 
///           |> NUnit (fun p -> { p with ErrorLevel = DontFailBuild })
///     )
let NUnit setParams (assemblies: string seq) =
    let details = assemblies |> separated ", "
    traceStartTask "NUnit" details
    let parameters = NUnitDefaults |> setParams
              
    let assemblies =  assemblies |> Seq.toArray

    if Array.isEmpty assemblies then
        failwith "NUnit: cannot run tests (the assembly list is empty)."

    let tool = parameters.ToolPath @@ parameters.ToolName

    let args = commandLineBuilder parameters assemblies
    trace (tool + " " + args)
    let result =
        execProcessAndReturnExitCode (fun info ->  
            info.FileName <- tool
            info.WorkingDirectory <- getWorkingDir parameters
            info.Arguments <- args) parameters.TimeOut

    sendTeamCityNUnitImport (getWorkingDir parameters @@ parameters.OutputFile)

    let errorDescription error = 
        match error with
        | OK -> "OK"
        | TestsFailed -> sprintf "NUnit test failed (%d)." error
        | FatalError x -> sprintf "NUnit test failed. Process finished with exit code %s (%d)." x error
    
    match parameters.ErrorLevel with
    | DontFailBuild ->
        match result with
        | OK | TestsFailed -> traceEndTask "NUnit" details
        | _ -> failwith (errorDescription result)
    | Error ->
        match result with
        | OK -> traceEndTask "NUnit" details
        | _ -> failwith (errorDescription result)

type private NUnitParallelResult = {
    AssemblyName : string
    ErrorOut : StringBuilder
    StandardOut : StringBuilder
    ReturnCode : int
    OutputFile : string }

/// Runs NUnit in parallel on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NUnitParams value.
///  - `assemblies` - Sequence of one or more assemblies containing NUnit unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\Test.*.dll") 
///           |> NUnitParallel (fun p -> { p with ErrorLevel = DontFailBuild })
///     )
let NUnitParallel setParams (assemblies: string seq) =
    let details = assemblies |> separated ", "
    traceStartTask "NUnitParallel" details
    let parameters = NUnitDefaults |> setParams
    let assemblies = assemblies |> Seq.toArray
    let tool = parameters.ToolPath @@ parameters.ToolName

    let runSingleAssembly parameters (name, outputFile) =
        let args = commandLineBuilder { parameters with OutputFile = outputFile } [name]
        let errout = StringBuilder()
        let stdout = StringBuilder()
        let result =
            ExecProcessWithLambdas (fun info ->  
                info.FileName <- tool
                info.WorkingDirectory <- getWorkingDir parameters
                info.Arguments <- args) 
                parameters.TimeOut
                true
                (fun e -> errout.Append(e) |> ignore)                
                (fun s -> stdout.Append(s) |> ignore)
        { AssemblyName = name; ErrorOut = errout; StandardOut = stdout; ReturnCode = result; OutputFile = outputFile }

    enableProcessTracing <- false
    let testRunResults =
        assemblies
        |> Seq.map (fun asm -> asm, Path.GetTempFileName())
        |> doParallelWithThrottle Environment.ProcessorCount (runSingleAssembly parameters)
        |> Seq.toList
    enableProcessTracing <- true

    // Read all valid results
    let docs = 
        testRunResults
        |> List.filter (fun x -> x.ReturnCode >= 0)
        |> List.map (fun x -> x.OutputFile)
        |> List.map (File.ReadAllText >> XDocument.Parse)

    match docs with
    | [] -> ()
    | _ -> 
        File.WriteAllText (getWorkingDir parameters @@ parameters.OutputFile, sprintf "%O" (NUnitMerge.mergeXDocs docs))
        sendTeamCityNUnitImport (getWorkingDir parameters @@ parameters.OutputFile)

    // Make sure we delete the temp files
    testRunResults 
    |> List.map (fun x -> x.OutputFile)
    |> List.iter File.Delete

    // Deal with errors
    match testRunResults |> List.filter (fun r -> r.ReturnCode <> 0) with
    | [] -> traceEndTask "NUnitParallel" details
    | xs -> 
        xs 
        |> List.collect (function
                | r when r.ReturnCode < 0 ->
                        [ sprintf "NUnit test run for %s returned error code %d, output to stderr was:" r.AssemblyName r.ReturnCode
                          sprintf "%O" r.ErrorOut ]
                | r -> [ sprintf "NUnit test run for %s reported failed tests, check outputfile %s for details." r.AssemblyName parameters.OutputFile ])
        |> List.iter traceError
        failwith "NUnitParallel test runs failed."
