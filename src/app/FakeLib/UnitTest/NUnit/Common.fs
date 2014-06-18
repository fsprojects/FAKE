[<AutoOpen>]
/// Contains types and utility functions relaited to running [NUnit](http://www.nunit.org/) unit tests.
module Fake.NUnitCommon

open System
open System.IO
open System.Text

/// Option which allows to specify if a NUnit error should break the build.
type NUnitErrorLevel = TestRunnerErrorLevel // a type alias to keep backwards compatibility

/// Process model for nunit to use, see http://www.nunit.org/index.php?p=projectEditor&r=2.5
type NUnitProcessModel = 
    | DefaultProcessModel
    | SingleProcessModel
    | SeparateProcessModel
    | MultipleProcessModel with 
    member x.ParamString =
        match x with
        | DefaultProcessModel -> ""
        | SingleProcessModel -> "Single"
        | SeparateProcessModel -> "Separate" 
        | MultipleProcessModel -> "Multiple"
/// The /domain option controls of the creation of AppDomains for running tests. See http://www.nunit.org/index.php?p=consoleCommandLine&r=2.4.6
type NUnitDomainModel = 
    /// No domain is created - the tests are run in the primary domain. This normally requires copying the NUnit assemblies into the same directory as your tests.
    | DefaultDomainModel
    /// A test domain is created - this is how NUnit worked prior to version 2.4
    | SingleDomainModel
    /// A separate test domain is created for each assembly
    | MultipleDomainModel with
    member x.ParamString =
        match x with
        | DefaultDomainModel -> ""
        | SingleDomainModel -> "Single"
        | MultipleDomainModel -> "Multiple"
/// Parameter type for NUnit.
type NUnitParams = 
    { IncludeCategory : string
      ExcludeCategory : string
      ToolPath : string
      ToolName : string
      DontTestInNewThread : bool
      StopOnError : bool
      OutputFile : string
      Out : string
      ErrorOutputFile : string
      Framework : string
      ProcessModel : NUnitProcessModel
      ShowLabels : bool
      WorkingDir : string
      XsltTransformFile : string
      TimeOut : TimeSpan
      DisableShadowCopy : bool
      Domain : NUnitDomainModel
      ErrorLevel : NUnitErrorLevel }

/// NUnit default parameters. FAKE tries to locate nunit-console.exe in any subfolder.
let NUnitDefaults = 
    let toolname = "nunit-console.exe"
    { IncludeCategory = ""
      ExcludeCategory = ""
      ToolPath = findToolFolderInSubPath toolname (currentDirectory @@ "tools" @@ "Nunit")
      ToolName = toolname
      DontTestInNewThread = false
      StopOnError = false
      OutputFile = currentDirectory @@ "TestResult.xml"
      Out = ""
      ErrorOutputFile = ""
      WorkingDir = ""
      Framework = ""
      ProcessModel = DefaultProcessModel
      ShowLabels = true
      XsltTransformFile = ""
      TimeOut = TimeSpan.FromMinutes 5.0
      DisableShadowCopy = false
      Domain = DefaultDomainModel
      ErrorLevel = Error }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let buildNUnitdArgs parameters assemblies = 
    new StringBuilder()
    |> append "-nologo"
    |> appendIfTrue parameters.DisableShadowCopy "-noshadow"
    |> appendIfTrue parameters.ShowLabels "-labels"
    |> appendIfTrue parameters.DontTestInNewThread "-nothread"
    |> appendIfTrue parameters.StopOnError "-stoponerror"
    |> appendFileNamesIfNotNull assemblies
    |> appendIfNotNullOrEmpty parameters.IncludeCategory "-include:"
    |> appendIfNotNullOrEmpty parameters.ExcludeCategory "-exclude:"
    |> appendIfNotNullOrEmpty parameters.XsltTransformFile "-transform:"
    |> appendIfNotNullOrEmpty parameters.OutputFile "-xml:"
    |> appendIfNotNullOrEmpty parameters.Out "-out:"
    |> appendIfNotNullOrEmpty parameters.Framework "-framework:"
    |> appendIfNotNullOrEmpty parameters.ProcessModel.ParamString "-process:"
    |> appendIfNotNullOrEmpty parameters.ErrorOutputFile "-err:"
    |> appendIfNotNullOrEmpty parameters.Domain.ParamString "-domain:"
    |> toText

/// Tries to detect the working directory as specified in the parameters or via TeamCity settings
/// [omit]
let getWorkingDir parameters = 
    Seq.find isNotNullOrEmpty [ parameters.WorkingDir
                                environVar ("teamcity.build.workingDir")
                                "." ]
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
