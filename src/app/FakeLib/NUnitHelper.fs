[<AutoOpen>]
module Fake.NUnitHelper

open System
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq

let inline imp arg =
  ( ^a : (static member op_Implicit : ^b -> ^a) arg)

let GetTestAssemblies (xDoc : XDocument) =
    xDoc.Descendants()
    |> Seq.filter (fun el -> el.Name = (imp "test-suite") && el.Attribute(imp "type").Value = "Assembly")

(*  Used by the NUnitParallel helper, can also be used to merge test results
    from multiple calls to the normal NUnit helper *)
module NUnitMerge =
    type ResultSummary =
        {
            total : int
            errors : int
            failures : int
            notrun : int
            inconclusive : int
            ignored : int
            skipped : int
            invalid : int
            datetime : DateTime
        }

    let GetTestSummary (xDoc : XDocument) =
        let tr = xDoc.Element(imp "test-results")
        {
            total = tr.Attribute(imp "total").Value |> Convert.ToInt32
            errors = tr.Attribute(imp "errors").Value |> Convert.ToInt32
            failures = tr.Attribute(imp "failures").Value |> Convert.ToInt32
            notrun = tr.Attribute(imp "not-run").Value |> Convert.ToInt32 
            inconclusive = tr.Attribute(imp "inconclusive").Value |> Convert.ToInt32
            ignored = tr.Attribute(imp "ignored").Value |> Convert.ToInt32
            skipped = tr.Attribute(imp "skipped").Value |> Convert.ToInt32
            invalid = tr.Attribute(imp "invalid").Value |> Convert.ToInt32
            datetime = String.concat " " [tr.Attribute(imp "date").Value;tr.Attribute(imp "time").Value] |> DateTime.Parse
        }

    let CreateTestSummaryElement summary =
        XElement.Parse (sprintf "<test-results name=\"Merged results\" total=\"%d\" errors=\"%d\" failures=\"%d\" not-run=\"%d\" inconclusive=\"%d\" skipped=\"%d\" ignored=\"%d\" invalid=\"%d\" date=\"%s\" time=\"%s\" />" summary.total summary.errors summary.failures summary.notrun summary.inconclusive summary.skipped summary.ignored summary.invalid (summary.datetime.ToString("yyyy-MM-dd")) (summary.datetime.ToString("HH:mm:ss")))

    type environment =
        {
            nunitversion : string
            clrversion : string
            osversion : string
            platform : string
            cwd : string
            machinename : string
            user : string
            userdomain : string
        }

    let GetEnvironment (xDoc : XDocument) =
        let env = xDoc.Element(imp "test-results").Element(imp "environment")
        {
            nunitversion = env.Attribute(imp "nunit-version").Value
            clrversion = env.Attribute(imp "clr-version").Value
            osversion = env.Attribute(imp "os-version").Value
            platform = env.Attribute(imp "platform").Value
            cwd = env.Attribute(imp "cwd").Value
            machinename = env.Attribute(imp "machine-name").Value
            user = env.Attribute(imp "user").Value
            userdomain = env.Attribute(imp "user-domain").Value
        }

    let CreateEnvironment environment =
        XElement.Parse (sprintf "<environment nunit-version=\"%s\" clr-version=\"%s\" os-version=\"%s\" platform=\"%s\" cwd=\"%s\" machine-name=\"%s\" user=\"%s\" user-domain=\"%s\" />" environment.nunitversion environment.clrversion environment.osversion environment.platform environment.cwd environment.machinename environment.user environment.userdomain)

    type culture =
        {
            currentculture : string
            currentuiculture : string
        }

    let GetCulture (xDoc : XDocument) =
        let culture = xDoc.Element(imp "test-results").Element(imp "culture-info")
        {
            currentculture = culture.Attribute(imp "current-culture").Value
            currentuiculture = culture.Attribute(imp "current-uiculture").Value
        }

    let CreateCulture culture =
        XElement.Parse (sprintf "<culture-info current-culture=\"%s\" current-uiculture=\"%s\" />" culture.currentculture culture.currentuiculture)

    let FoldAssemblyToProjectTuple agg (assembly : XElement) =
        let result, time, asserts = agg
        let outResult =
            if assembly.Attribute(imp "result").Value = "Failure" then "Failure" 
            elif assembly.Attribute(imp "result").Value = "Inconclusive" && result = "Success" then "Inconclusive"
            else result
        (outResult, time + Convert.ToDouble (assembly.Attribute(imp "time").Value), asserts + Convert.ToInt32 (assembly.Attribute(imp "asserts").Value))
        

    let TestProjectSummary assemblies =
        assemblies
        |> Seq.fold FoldAssemblyToProjectTuple ("Success", 0.0, 0)

    let CreateTestProjectNode assemblies =
        let result, time, asserts = TestProjectSummary assemblies
        let projectEl = XElement.Parse (sprintf "<test-suite type=\"Test Project\" name=\"\" executed=\"True\" result=\"%s\" time=\"%f\" asserts=\"%d\" />" result time asserts)
        let results = XElement.Parse ("<results/>")
        results.Add (assemblies |> Seq.toArray)
        projectEl.Add results
        projectEl

    let MergeTestSummary agg summary =
        { agg with 
            total = agg.total + summary.total
            errors = agg.errors + summary.errors
            failures = agg.failures + summary.failures
            notrun = agg.notrun + summary.notrun
            inconclusive = agg.inconclusive + summary.inconclusive
            ignored = agg.ignored + summary.ignored
            skipped = agg.skipped + summary.skipped
            invalid = agg.invalid + summary.invalid
            datetime = Seq.min [agg.datetime;summary.datetime]
        }


    let GetXDocs directory filter =
        Directory.GetFiles(directory, filter, SearchOption.AllDirectories)
        |> Seq.map (fun fileName -> XDocument.Parse(File.ReadAllText(fileName)))

    let Folder state xDoc =
        let summary, environment, culture, assemblies = state
        // Sanity check!
        if environment <> (GetEnvironment xDoc) || culture <> (GetCulture xDoc) then printf "Unmatched environment and/or cultures detected: some of theses results files are not from the same test run."
        (MergeTestSummary (GetTestSummary xDoc) summary, environment, culture, Seq.append assemblies (GetTestAssemblies xDoc))

    let FoldDocs docs =
        let state = (Seq.head docs |> GetTestSummary, Seq.head docs |> GetEnvironment, Seq.head docs |> GetCulture, Seq.head docs |> GetTestAssemblies)
        Seq.fold Folder state docs

    let CreateMerged state =
        let summary, environment, culture, assemblies = state
        let results = (CreateTestSummaryElement summary)
        results.Add [CreateEnvironment environment;CreateCulture culture;CreateTestProjectNode assemblies]
        results

    let WriteMergedNunitResults (directory, filter, outfile) =
        GetXDocs directory filter
        |> FoldDocs
        |> CreateMerged
        |> fun x -> File.WriteAllText(outfile, x.ToString())

let AllSucceeded xDocs =
    xDocs
    |> Seq.map GetTestAssemblies
    |> Seq.concat
    |> Seq.map (fun assembly -> assembly.Attribute(imp "result").Value)
    |> Seq.map (fun x -> x <> "Failure")
    |> Seq.reduce (&&)

type NUnitErrorLevel =
    | Error
    | DontFailBuild

type NUnitParams =
    { IncludeCategory:string;
      ExcludeCategory:string;
      ToolPath:string;
      ToolName:string;
      TestInNewThread:bool;
      OutputFile:string;
      Out: string;
      ErrorOutputFile:string;
      Framework:string;
      ShowLabels: bool;
      WorkingDir:string; 
      XsltTransformFile:string;
      TimeOut: TimeSpan;
      DisableShadowCopy:bool
      Domain:string
      ErrorLevel:NUnitErrorLevel}

/// NUnit default params  
let NUnitDefaults =
    { IncludeCategory = null;
      ExcludeCategory = null;
      ToolPath = currentDirectory @@ "tools" @@ "Nunit";
      ToolName = @"nunit-console.exe";
      TestInNewThread = false;
      OutputFile = currentDirectory @@ "TestResult.xml";
      Out = null;
      ErrorOutputFile = null;
      WorkingDir = null;
      Framework = null;
      ShowLabels = true;
      XsltTransformFile = null;
      TimeOut = TimeSpan.FromMinutes 5.
      DisableShadowCopy = false;
      Domain = null;
      ErrorLevel = Error}

let commandLineBuilder parameters assemblies =
    let cl = new StringBuilder()
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

let getWorkingDir parameters =
    Seq.find (fun s -> s <> null && s <> "") [parameters.WorkingDir; environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

// NUnit console returns negative error codes for errors and sum of failed/ignored/exceptional 
// tests otherwise. Zero means that all tests passed.
let (|OK|TestsFailed|FatalError|) = function
    | 0 -> OK
    | -1 -> FatalError "InvalidArg"
    | -2 -> FatalError "FileNotFound"
    | -3 -> FatalError "FixtureNotFound"
    | -100 -> FatalError "UnexpectedError"
    | x when x < 0 -> FatalError "FatalError"
    | _ -> TestsFailed

/// Run NUnit on a group of assemblies.
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

type NUnitParallelResult =
    {
        AssemblyName : string
        ErrorOut : StringBuilder
        StandardOut : StringBuilder
        ReturnCode : int
        OutputFile : string
    }

let NUnitParallel setParams (assemblies: string seq) =
    let details = assemblies |> separated ", "
    traceStartTask "NUnitParallel" details
    let parameters = NUnitDefaults |> setParams
              
    let assemblies =  assemblies |> Seq.toArray

    let tool = parameters.ToolPath @@ parameters.ToolName

    let runSingleAssembly name parameters outputFile =
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
        |> Seq.map (fun assembly -> assembly, Path.GetTempFileName())
        |> doParallelWithThrottle (Environment.ProcessorCount) (fun (assembly, outputFile) -> runSingleAssembly assembly parameters outputFile)
    enableProcessTracing <- true

    // Merge all valid results into single results file
    if Array.Exists(testRunResults, fun r -> r.ReturnCode >= 0)  then
        testRunResults
        |> Seq.filter (fun r -> r.ReturnCode >= 0)
        |> Seq.map (fun result -> result.OutputFile)
        |> Seq.map (fun fileName -> XDocument.Parse(File.ReadAllText(fileName)))
        |> NUnitMerge.FoldDocs
        |> NUnitMerge.CreateMerged
        |> fun x -> File.WriteAllText(getWorkingDir parameters @@ parameters.OutputFile, x.ToString())
        sendTeamCityNUnitImport (getWorkingDir parameters @@ parameters.OutputFile)

    // Deal with errors
    let hasFailed = Array.Exists(testRunResults, fun r -> r.ReturnCode <> 0)
    if hasFailed then
        testRunResults
        |> Seq.filter (fun r -> r.ReturnCode <> 0)
        |> Seq.iter (fun r ->
                        match r with
                        | result when r.ReturnCode < 0 ->
                            traceError <| sprintf "NUnit test run for %s returned error code %d, output to stderr was:" r.AssemblyName r.ReturnCode
                            traceError <| r.ErrorOut.ToString()
                        | result ->
                            traceError <| sprintf "NUnit test run for %s reported failed tests, check outputfile %s for details." r.AssemblyName parameters.OutputFile)

    // Make sure we delete the temp files
    testRunResults
    |> Seq.iter (fun result -> File.Delete(result.OutputFile))

    if hasFailed then          
        failwith "NUnitParallel test runs failed."
    else
        traceEndTask "NUnitParallel" details
