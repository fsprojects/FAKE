/// Contains types and utility functions relaited to running [NUnit](http://www.nunit.org/) unit tests.
module Fake.DotNet.Testing.NUnit.Common

open Fake.Testing.Common
open Fake.IO.FileSystemOperators
open Fake.Core
open System
open System.IO
open System.Text
open System
open System.IO
open System.Linq
open System.Text
open Fake

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
    /// The default is to use multiple domains if multiple assemblies are listed on the command line. Otherwise a single domain is used.
    | DefaultDomainModel
    /// No domain is created - the tests are run in the primary domain. This normally requires copying the NUnit assemblies into the same directory as your tests.
    | NoDomainModel
    /// A test domain is created - this is how NUnit worked prior to version 2.4
    | SingleDomainModel
    /// A separate test domain is created for each assembly
    | MultipleDomainModel with
    member x.ParamString =
        match x with
        | DefaultDomainModel -> ""
        | NoDomainModel -> "None"
        | SingleDomainModel -> "Single"
        | MultipleDomainModel -> "Multiple"

/// The [NUnit](http://www.nunit.org/) Console Parameters type.
/// FAKE will use [NUnitDefaults](fake-nunitcommon.html) for values not provided.
///
/// For reference, see: [NUnit-Console Command Line Options](http://www.nunit.org/index.php?p=consoleCommandLine&r=2.6.4)
type NUnitParams =
    {
      /// The [Categories](http://www.nunit.org/index.php?p=category&r=2.6.4) to be included in a test run. Multiple categories may be specified on either option, by using commas to separate them.
      IncludeCategory : string

      /// The [Categories](http://www.nunit.org/index.php?p=category&r=2.6.4) to be excluded in a test run. Multiple categories may be specified on either option, by using commas to separate them.
      ExcludeCategory : string

      /// The path to the NUnit console runner: `nunit-console.exe`
      ToolPath : string

      /// NUnit console runner name. ( `nunit-console.exe`)
      ToolName : string

      /// Suppresses use of a separate thread for running the tests and uses the main thread instead.
      DontTestInNewThread : bool

      /// Causes execution of the test run to terminate immediately on the first test failure or error.
      StopOnError : bool

      /// Gives ability to not error if an assembly with no tests is passed into nunit
      SkipNonTestAssemblies : bool

      /// The output path of the nUnit XML report.
      OutputFile : string

      /// Redirects output created by the tests from standard output (console) to the file specified as value.
      Out : string

      /// Redirects error output created by the tests from standard error output (console) to the file specified as value.
      ErrorOutputFile : string

      /// Allows you to specify the version of the runtime to be used in executing tests.
      Framework : string

      /// Controls how NUnit loads tests in processes. See: [NUnitProcessModel](fake-nunitcommon-nunitprocessmodel.html).
      ProcessModel : NUnitProcessModel

      /// Causes an identifying label to be displayed at the start of each test case.
      ShowLabels : bool

      /// The working directory.
      WorkingDir : string

      /// The path to a custom XSLT transform file to be used to process the XML report.
      XsltTransformFile : string

      /// The default timeout to be used for test cases. If any test exceeds the timeout value, it is cancelled and reported as an error.
      TimeOut : TimeSpan

      /// Disables shadow copying of the assembly in order to provide improved performance.
      DisableShadowCopy : bool

      /// See [NUnitDomainModel](fake-nunitcommon-nunitdomainmodel.html).
      Domain : NUnitDomainModel
      /// Default: [TestRunnerErrorLevel](fake-unittestcommon-testrunnererrorlevel.html).Error
      ErrorLevel : NUnitErrorLevel
      /// Default: ""
      Fixture: string}

/// The [NUnitParams](fake-nunitcommon-nunitparams.html) default parameters.
///
/// ## Defaults
/// - `IncludeCategory` - `""`
/// - `ExcludeCategory` - `""`
/// - `ToolPath` - The `nunit-console.exe` path if it exists in a subdirectory of the current directory.
/// - `ToolName` - `"nunit-console.exe"`
/// - `DontTestInNewThread`- `false`
/// - `StopOnError` - `false`
/// - `SkipNonTestAssemblies` - `false`
/// - `OutputFile` - `"TestResult.xml"`
/// - `Out` - `""`
/// - `ErrorOutputFile` - `""`
/// - `WorkingDir` - `""`
/// - `Framework` - `""`
/// - `ProcessModel` - `DefaultProcessModel`
/// - `ShowLabels` - `true`
/// - `XsltTransformFile` - `""`
/// - `TimeOut` - 5 minutes
/// - `DisableShadowCopy` - `false`
/// - `Domain` - `DefaultDomainModel`
/// - `ErrorLevel` - `Error`
/// - `Fixture` - `""`
let NUnitDefaults =
    let toolname = "nunit-console.exe"
    { IncludeCategory = ""
      ExcludeCategory = ""
      ToolPath = Fake.IO.Globbing.Tools.findToolFolderInSubPath toolname (Fake.IO.Shell.pwd() @@ "tools" @@ "Nunit")
      ToolName = toolname
      DontTestInNewThread = false
      StopOnError = false
      SkipNonTestAssemblies = false
      OutputFile = Fake.IO.Shell.pwd() @@ "TestResult.xml"
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
let buildArgs (parameters: NUnitParams) (assemblies: string seq) =
    new StringBuilder()
    |> StringBuilder.append "-nologo"
    |> StringBuilder.appendIfTrue parameters.DisableShadowCopy "-noshadow"
    |> StringBuilder.appendIfTrue parameters.ShowLabels "-labels"
    |> StringBuilder.appendIfTrue parameters.DontTestInNewThread "-nothread"
    |> StringBuilder.appendIfTrue parameters.StopOnError "-stoponerror"
    |> StringBuilder.appendIfTrue parameters.SkipNonTestAssemblies "-skipnontestassemblies"
    |> StringBuilder.appendFileNamesIfNotNull assemblies
    |> StringBuilder.appendIfNotNullOrEmpty parameters.IncludeCategory "-include:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.ExcludeCategory "-exclude:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.XsltTransformFile "-transform:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.OutputFile "-xml:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Out "-out:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Framework "-framework:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.ProcessModel.ParamString "-process:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.ErrorOutputFile "-err:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Domain.ParamString "-domain:"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Fixture "-fixture:"
    |> StringBuilder.toText

/// Tries to detect the working directory as specified in the parameters or via TeamCity settings
/// [omit]
let getWorkingDir parameters =
    Seq.find String.isNotNullOrEmpty [ parameters.WorkingDir
                                       Fake.Core.Environment.environVar ("teamcity.build.workingDir")
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
