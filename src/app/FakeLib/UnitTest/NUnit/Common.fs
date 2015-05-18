[<AutoOpen>]
/// Contains types and utility functions relaited to running [NUnit](http://www.nunit.org/) unit tests.
module Fake.NUnitCommon

open System
open System.IO
open System.Text

/// Option which allows to specify if a NUnit error should break the build.
type NUnitErrorLevel = TestRunnerErrorLevel // a type alias to keep backwards compatibility

/// Process model for nunit to use, see [Project Editor](http://www.nunit.org/index.php?p=projectEditor&r=2.6.4)
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
/// The /domain option controls of the creation of AppDomains for running tests. See [NUnit-Console Command Line Options](http://www.nunit.org/index.php?p=consoleCommandLine&r=2.6.4)
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

/// The [NUnit](http://www.nunit.org/) Console Parameters type.
/// FAKE will use [NUnitDefaults](fake-nunitcommon.html) for values not provided.
///
/// For reference, see: [NUnit-Console Command Line Options](http://www.nunit.org/index.php?p=consoleCommandLine&r=2.6.4)
type NUnitParams = 
    { 
      /// Default: ""
      IncludeCategory : string
      /// Default: ""
      ExcludeCategory : string
      /// Default FAKE will try to locate nunit-console.exe in any subfolder of the current directory.
      ToolPath : string
      /// Default:"nunit-console.exe"
      ToolName : string
      /// Default: false
      DontTestInNewThread : bool
      /// Default: false
      StopOnError : bool
      /// Default: ".\TestResult.xml"
      OutputFile : string
      /// Default: ""
      Out : string
      /// Default: ""
      ErrorOutputFile : string
      /// Default: ""
      Framework : string
      /// Default: [NUnitProcessModel](fake-nunitcommon-nunitprocessmodel.html).DefaultProcessModel
      ProcessModel : NUnitProcessModel
      /// Default: true
      ShowLabels : bool
      /// Default: ""
      WorkingDir : string
      /// Default: ""
      XsltTransformFile : string
      /// Default: 5 minutes
      TimeOut : TimeSpan
      /// Default: false
      DisableShadowCopy : bool
      /// Default: [NUnitDomainModel](fake-nunitcommon-nunitdomainmodel.html).DefaultDomainModel
      Domain : NUnitDomainModel
      /// Default: [TestRunnerErrorLevel](fake-unittestcommon-testrunnererrorlevel.html).Error
      ErrorLevel : NUnitErrorLevel 
      /// Default: ""
      Fixture: string}

/// The [NUnit](http://www.nunit.org/) default parameters. FAKE tries to locate nunit-console.exe in any subfolder.
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
      ErrorLevel = Error 
      Fixture = ""}

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
    |> appendIfNotNullOrEmpty parameters.Fixture "-fixture:"
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
