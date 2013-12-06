[<AutoOpen>]
/// Contains types and utility functions relaited to running [NUnit](http://www.nunit.org/) unit tests.
module Fake.NUnitCommon

open System
open System.IO
open System.Text

/// Option which allow to specify if a NUnit error should break the build.
type NUnitErrorLevel =
/// This option instructs FAKE to break the build if NUnit reports an error. (Default)
| Error
/// With this option set, no exception is thrown if a test is broken.
| DontFailBuild

/// Parameter type for NUnit.
type NUnitParams = 
    { IncludeCategory: string
      ExcludeCategory: string
      ToolPath: string
      ToolName: string
      TestInNewThread: bool
      OutputFile: string
      Out: string
      ErrorOutputFile: string
      Framework: string
      ShowLabels: bool
      WorkingDir: string
      XsltTransformFile: string
      TimeOut: TimeSpan
      DisableShadowCopy: bool
      Domain: string
      ErrorLevel: NUnitErrorLevel }

/// NUnit default parameters. FAKE tries to locate nunit-console.exe in any subfolder.
let NUnitDefaults = 
    let toolname = "nunit-console.exe"
    { IncludeCategory = ""
      ExcludeCategory = ""
      ToolPath = findToolFolderInSubPath toolname (currentDirectory @@ "tools" @@ "Nunit")
      ToolName = toolname
      TestInNewThread = false
      OutputFile = currentDirectory @@ "TestResult.xml"
      Out = ""
      ErrorOutputFile = ""
      WorkingDir = ""
      Framework = ""
      ShowLabels = true
      XsltTransformFile = ""
      TimeOut = TimeSpan.FromMinutes 5.0
      DisableShadowCopy = false
      Domain = ""
      ErrorLevel = Error }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let buildNUnitdArgs parameters assemblies =
    new StringBuilder()
    |> append "-nologo"
    |> appendIfTrue parameters.DisableShadowCopy "-noshadow" 
    |> appendIfTrue parameters.ShowLabels "-labels" 
    |> appendIfTrue parameters.TestInNewThread "-thread" 
    |> appendFileNamesIfNotNull assemblies
    |> appendIfNotNullOrEmpty parameters.IncludeCategory "-include:"
    |> appendIfNotNullOrEmpty parameters.ExcludeCategory "-exclude:"
    |> appendIfNotNullOrEmpty parameters.XsltTransformFile "-transform:"
    |> appendIfNotNullOrEmpty parameters.OutputFile  "-xml:"
    |> appendIfNotNullOrEmpty parameters.Out "-out:"
    |> appendIfNotNullOrEmpty parameters.Framework  "-framework:"
    |> appendIfNotNullOrEmpty parameters.ErrorOutputFile "-err:"
    |> appendIfNotNullOrEmpty parameters.Domain "-domain:"
    |> toText

/// Tries to detect the working directory as specified in the parameters or via TeamCity settings
/// [omit]
let getWorkingDir parameters =
    Seq.find isNotNullOrEmpty [parameters.WorkingDir; environVar("teamcity.build.workingDir"); "."]
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


