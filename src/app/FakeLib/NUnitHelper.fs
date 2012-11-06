[<AutoOpen>]
module Fake.NUnitHelper

open System
open System.IO
open System.Text

type NUnitParams =
    { IncludeCategory:string;
      ExcludeCategory:string;
      ToolPath:string;
      ToolName:string;
      TestInNewThread:bool;
      OutputFile:string;
      ErrorOutputFile:string;
      Framework:string;
      ShowLabels: bool;
      WorkingDir:string; 
      XsltTransformFile:string;
      TimeOut: TimeSpan;
      DisableShadowCopy:bool}
   
type TestCase =
    { Name: string;
      Executed: bool;
      Ignored: bool;
      Skipped: bool;
      Success: bool;
      RunTime: float;
      ErrorMessage: string;
      StackTrace:string}
      
type TestSuite = 
    { Name: string;
      DateTime: DateTime;
      TestCases: TestCase list}           
    
    member x.Success   = x.TestCases |> List.exists (fun test -> not test.Success)
    member x.Executed  = x.TestCases |> List.exists (fun test -> test.Executed)
    member x.TestCount = x.TestCases |> List.length
    member x.Errors = x.TestCases  |> Seq.filter (fun test -> not test.Success) |> Seq.length
    member x.NotRun = x.TestCases  |> Seq.filter (fun test -> not test.Executed) |> Seq.length
    member x.Ignored = x.TestCases |> Seq.filter (fun test -> test.Ignored) |> Seq.length
    member x.Skipped = x.TestCases |> Seq.filter (fun test -> test.Skipped) |> Seq.length
    member x.Runtime = x.TestCases |> List.sumBy (fun test -> test.RunTime)         

/// NUnit default params  
let NUnitDefaults =
    { IncludeCategory = null;
      ExcludeCategory = null;
      ToolPath = currentDirectory @@ "tools" @@ "Nunit";
      ToolName = @"nunit-console.exe";
      TestInNewThread = false;
      OutputFile = currentDirectory @@ "TestResult.xml";
      ErrorOutputFile = null;
      WorkingDir = null;
      Framework = null;
      ShowLabels = true;
      XsltTransformFile = null;
      TimeOut = TimeSpan.FromMinutes 5.
      DisableShadowCopy = false}

/// Run NUnit on a group of assemblies.
let NUnit setParams (assemblies: string seq) =
    let details = assemblies |> separated ", "
    traceStartTask "NUnit" details
    let parameters = NUnitDefaults |> setParams
              
    let assemblies =  assemblies |> Seq.toArray
    let commandLineBuilder =
        new StringBuilder()
          |> append "/nologo"
          |> appendIfTrue parameters.DisableShadowCopy "/noshadow" 
          |> appendIfTrue parameters.ShowLabels "/labels" 
          |> appendIfTrue parameters.TestInNewThread "/thread" 
          |> appendFileNamesIfNotNull assemblies
          |> appendIfNotNull parameters.IncludeCategory "/include:"
          |> appendIfNotNull parameters.ExcludeCategory "/exclude:"
          |> appendIfNotNull parameters.XsltTransformFile "/transform:"
          |> appendIfNotNull parameters.OutputFile  "/xml:"
          |> appendIfNotNull parameters.Framework  "/framework:"
          |> appendIfNotNull parameters.ErrorOutputFile "/err:"

    let tool = parameters.ToolPath @@ parameters.ToolName

    let args = commandLineBuilder.ToString()
    trace (tool + " " + args)
    let result =
        execProcessAndReturnExitCode (fun info ->  
            info.FileName <- tool
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut

    sendTeamCityNUnitImport parameters.OutputFile
    if result = 0 then          
        traceEndTask "NUnit" details
    else
        if result = 2 then
            failwith "NUnit test failed."
        failwithf "NUnit test failed. Process finished with exit code %d." result

/// writes the given TestSuite as XML file in NUnit style
let writeXMLOutput (testSuite:TestSuite) fileName =
  tracefn "Writing XML test results to %s" fileName
  
  use writer = XmlWriter fileName
  
  let writeTestCases writer =
    testSuite.TestCases 
      |> List.fold
           (fun writer test ->                
              let writer'=
                writer 
                  |> XmlStartElement "test-case"
                    |> XmlAttribute "name" test.Name
                    |> XmlAttribute "executed" test.Executed
                    |> XmlAttribute "success" test.Success
                    |> XmlAttribute "time" (test.RunTime.ToString(Globalization.CultureInfo.InvariantCulture))
              let writer'' = 
                if test.Success then writer' else
                writer'
                  |> XmlStartElement "failure"
                    |> XmlCDataElement "message" test.ErrorMessage
                    |> XmlCDataElement "stack-trace" test.StackTrace
                  |> XmlEndElement
                  
              writer'' |> XmlEndElement)
            writer
  
  writer 
    |> XmlComment (sprintf "NUNIT test results - created by %s" fakeVersionStr)
    |> XmlStartElement "test-results"
      |> XmlAttribute "name" testSuite.Name
      |> XmlAttribute "total" testSuite.TestCount
      |> XmlAttribute "errors" testSuite.Errors
      |> XmlAttribute "not-run" testSuite.NotRun
      |> XmlAttribute "ignored" testSuite.Ignored
      |> XmlAttribute "date" testSuite.DateTime.Date
      |> XmlAttribute "time" testSuite.DateTime.TimeOfDay
      |> XmlStartElement "test-suite"
        |> XmlAttribute "name" testSuite.Name
        |> XmlAttribute "executed" testSuite.Executed
        |> XmlAttribute "success" testSuite.Success
        |> XmlAttribute "time" testSuite.Runtime
        |> XmlStartElement "results"
        |> writeTestCases
        |> XmlEndElement
      |> XmlEndElement
    |> XmlEndElement
    |> ignore                