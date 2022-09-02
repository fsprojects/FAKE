namespace Fake.DotNet.Testing.NUnit

/// <namespacedoc>
/// <summary>
/// Testing.NUnit namespace contains tasks to interact with NUnit testing framework
/// </summary>
/// </namespacedoc>
/// 
/// <summary>
/// Contains types and utility functions related to running <a href="http://www.nunit.org/">NUnit</a> unit tests.
/// </summary>
module Common =

    open Fake.Testing.Common
    open Fake.IO.FileSystemOperators
    open Fake.Core
    open System
    open System.IO
    open System.Text

    /// Option which allows to specify if a NUnit error should break the build.
    type NUnitErrorLevel = TestRunnerErrorLevel // a type alias to keep backwards compatibility

    /// <summary>
    /// Process model for nunit to use, see
    /// <a href="http://www.nunit.org/index.php?p=projectEditor&amp;r=2.6.4">Project Editor</a>
    /// </summary>
    type NUnitProcessModel =
        | DefaultProcessModel
        | SingleProcessModel
        | SeparateProcessModel
        | MultipleProcessModel

        member x.ParamString =
            match x with
            | DefaultProcessModel -> ""
            | SingleProcessModel -> "Single"
            | SeparateProcessModel -> "Separate"
            | MultipleProcessModel -> "Multiple"

    /// <summary>
    /// The <c>/domain</c> option controls of the creation of AppDomains for running tests. See
    /// <a href="http://www.nunit.org/index.php?p=consoleCommandLine&amp;r=2.6.4">NUnit-Console Command Line Options</a>
    /// </summary>
    type NUnitDomainModel =
        /// The default is to use multiple domains if multiple assemblies are listed on the command line. Otherwise a
        /// single domain is used.
        | DefaultDomainModel
        /// No domain is created - the tests are run in the primary domain. This normally requires copying the NUnit
        /// assemblies into the same directory as your tests.
        | NoDomainModel
        /// A test domain is created - this is how NUnit worked prior to version 2.4
        | SingleDomainModel
        /// A separate test domain is created for each assembly
        | MultipleDomainModel

        member x.ParamString =
            match x with
            | DefaultDomainModel -> ""
            | NoDomainModel -> "None"
            | SingleDomainModel -> "Single"
            | MultipleDomainModel -> "Multiple"

    /// <summary>
    /// The <a href="http://www.nunit.org/">NUnit</a> Console Parameters type.
    /// FAKE will use `NUnitDefaults` for values not provided.
    /// </summary>
    /// <remarks>
    /// For reference, see:
    /// <a href="http://www.nunit.org/index.php?p=consoleCommandLine&amp;r=2.6.4">NUnit-Console Command Line Options</a>
    /// </remarks>
    type NUnitParams =
        {
            /// The <a href="http://www.nunit.org/index.php?p=category&amp;r=2.6.4">Categories</a> to be included in a
            /// test run. Multiple categories may be specified on either option, by using commas to separate them.
            IncludeCategory: string

            /// The <a href="http://www.nunit.org/index.php?p=category&amp;r=2.6.4">Categories</a> to be excluded in
            /// a test run. Multiple categories may be specified on either option, by using commas to separate them.
            ExcludeCategory: string

            /// The path to the NUnit console runner: `nunit-console.exe`
            ToolPath: string

            /// NUnit console runner name. ( <c>nunit-console.exe</c>)
            ToolName: string

            /// Suppresses use of a separate thread for running the tests and uses the main thread instead.
            DontTestInNewThread: bool

            /// Causes execution of the test run to terminate immediately on the first test failure or error.
            StopOnError: bool

            /// Gives ability to not error if an assembly with no tests is passed into nunit
            SkipNonTestAssemblies: bool

            /// The output path of the nUnit XML report.
            OutputFile: string

            /// Redirects output created by the tests from standard output (console) to the file specified as value.
            Out: string

            /// Redirects error output created by the tests from standard error output (console) to the file specified
            /// as value.
            ErrorOutputFile: string

            /// Allows you to specify the version of the runtime to be used in executing tests.
            Framework: string

            /// Controls how NUnit loads tests in processes.
            /// See: <a href="/guide/fake-nunitcommon-nunitprocessmodel.html">NUnitProcessModel</a>.
            ProcessModel: NUnitProcessModel

            /// Causes an identifying label to be displayed at the start of each test case.
            ShowLabels: bool

            /// The working directory.
            WorkingDir: string

            /// The path to a custom XSLT transform file to be used to process the XML report.
            XsltTransformFile: string

            /// The default timeout to be used for test cases. If any test exceeds the timeout value, it is cancelled
            /// and reported as an error.
            TimeOut: TimeSpan

            /// Disables shadow copying of the assembly in order to provide improved performance.
            DisableShadowCopy: bool

            /// See <c>NUnitDomainModel</c> type
            Domain: NUnitDomainModel
            /// Default: <c>TestRunnerErrorLevel.Error</c>
            ErrorLevel: NUnitErrorLevel
            /// Default: ""
            Fixture: string
        }
        
    let internal toolPath toolName =
        let toolPath =
            ProcessUtils.tryFindLocalTool "TOOL" toolName [ Directory.GetCurrentDirectory() ]

        match toolPath with
        | Some path -> path
        | None -> toolName

    /// <summary>
    /// The <c>NUnitParams</c> default parameters.
    /// </summary>
    /// <list type="number">
    /// <item>
    /// <c>IncludeCategory</c> - <c>""</c>
    /// </item>
    /// <item>
    /// <c>ToolPath</c> - The <c>nunit-console.exe</c> path if it exists in a subdirectory of the current directory
    /// </item>
    /// <item>
    /// <c>DontTestInNewThread</c>- <c>false</c>
    /// </item>
    /// <item>
    /// <c>StopOnError</c> - <c>false</c>
    /// </item>
    /// <item>
    /// <c>OutputFile</c> - <c>"TestResult.xml"</c>
    /// </item>
    /// <item>
    /// <c>Out</c> - <c>""</c>
    /// </item>
    /// <item>
    /// <c>ErrorOutputFile</c> - <c>""</c>
    /// </item>
    /// <item>
    /// <c>WorkingDir</c> - <c>""</c>
    /// </item>
    /// <item>
    /// <c>Framework</c> - <c>""</c>
    /// </item>
    /// <item>
    /// <c>ProcessModel</c> - <c>DefaultProcessModel</c>
    /// </item>
    /// <item>
    /// <c>ShowLabels</c> - <c>true</c>
    /// </item>
    /// <item>
    /// <c>XsltTransformFile</c> - <c>""</c>
    /// </item>
    /// <item>
    /// <c>TimeOut</c> - 5 minute
    /// </item>
    /// <item>
    /// <c>DisableShadowCopy</c> - <c>false</c>
    /// </item>
    /// <item>
    /// <c>Domain</c> - <c>DefaultDomainModel</c>
    /// </item>
    /// <item>
    /// <c>SkipNonTestAssemblies</c> - <c>false</c>
    /// </item>
    /// <item>
    /// <c>ErrorLevel</c> - <c>Error</c>
    /// </item>
    /// <item>
    /// <c>Fixture</c> - <c>""</c>
    /// </item>
    /// </list>
    let NUnitDefaults =
        let toolName = "nunit-console.exe"

        { IncludeCategory = ""
          ExcludeCategory = ""
          ToolPath = toolPath toolName
          ToolName = toolName
          DontTestInNewThread = false
          StopOnError = false
          SkipNonTestAssemblies = false
          OutputFile = Fake.IO.Shell.pwd () @@ "TestResult.xml"
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
          Fixture = "" }

    /// <summary>
    /// Builds the command line arguments from the given parameter record and the given assemblies.
    /// </summary>
    let buildArgs (parameters: NUnitParams) (assemblies: string seq) =
        StringBuilder()
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

    /// <summary>
    /// Tries to detect the working directory as specified in the parameters or via TeamCity settings
    /// </summary>
    /// [omit]
    let getWorkingDir parameters =
        Seq.find
            String.isNotNullOrEmpty
            [ parameters.WorkingDir
              Fake.Core.Environment.environVar "teamcity.build.workingDir"
              "." ]
        |> Path.GetFullPath

    /// <summary>
    /// NUnit console returns negative error codes for errors and sum of failed, ignored and exceptional tests
    /// otherwise. Zero means that all tests passed.
    /// </summary>
    let (|OK|TestsFailed|FatalError|) errorCode =
        match errorCode with
        | 0 -> OK
        | -1 -> FatalError "InvalidArg"
        | -2 -> FatalError "FileNotFound"
        | -3 -> FatalError "FixtureNotFound"
        | -100 -> FatalError "UnexpectedError"
        | x when x < 0 -> FatalError "FatalError"
        | _ -> TestsFailed
