/// Contains tasks to run [NUnit](http://www.nunit.org/) unit tests.
///
/// ### Sample
///
///        Target.create "Test" (fun _ ->
///            !! (testDir + "/NUnit.Test.*.dll")
///              |> NUnit3.run (fun p ->
///                  {p with
///                        ShadowCopy = false })
///        )
///
module Fake.DotNet.Testing.NUnit3

open Fake.Testing.Common
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.Core
open System
open System.IO
open System.Text
open Fake.DotNet.Testing.NUnit.Common

/// Process model for NUnit 3 to use.
type NUnit3ProcessModel =
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

/// The --domain option controls of the creation of AppDomains for running tests. See [NUnit-Console Command Line Options](http://www.nunit.org/index.php?p=consoleCommandLine&r=2.6.4)
type NUnit3DomainModel =
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

/// The --framework option in running NUnit 3. There are three kinds - VXY, which means either .NET framework or Mono, NetXY (use .NET framework with given version)
/// and MonoXY (Mono framework with given version). You can use Net or Mono to let NUnit select the version.
/// You can pass any value using Other.
type NUnit3Runtime =
    /// Uses the runtime under which the assembly was built.
    | Default
    | V20
    | V30
    | V35
    | V40
    | V45
    /// NUnit should use .NET framework but can select its version
    | Net
    | Net20
    | Net30
    | Net35
    | Net40
    | Net45
    /// NUnit should use Mono framework but can select its version
    | Mono
    | Mono20
    | Mono30
    | Mono35
    | Mono40
    /// NUnit should use runtime specified by this value
    | Other of string with
    member x.ParamString =
        match x with
        | Default -> ""
        | V20 -> "v2.0"
        | V30 -> "v3.0"
        | V35 -> "v3.5"
        | V40 -> "v4.0"
        | V45 -> "v4.5"
        | Net -> "net"
        | Net20 -> "net-2.0"
        | Net30 -> "net-3.0"
        | Net35 -> "net-3.5"
        | Net40 -> "net-4.0"
        | Net45 -> "net-4.5"
        | Mono -> "mono"
        | Mono20 -> "mono-2.0"
        | Mono30 -> "mono-3.0"
        | Mono35 -> "mono-3.5"
        | Mono40 -> "mono-4.0"
        | Other(name) -> name

/// Option which allows to specify if a NUnit error should break the build.
type NUnit3ErrorLevel = TestRunnerErrorLevel

/// The --trace option in NUnit3 console runner. Specifies the internal nunit runner log level.
type NUnit3TraceLevel =
    | Default
    | Off
    | Error
    | Warning
    | Info
    | Verbose
    member x.ParamString =
        match x with
        | Default -> ""
        | Off -> "Off"
        | Error -> "Error"
        | Warning -> "Warning"
        | Info -> "Info"
        | Verbose -> "Verbose"

/// The --labels option in NUnit3 console runner. Specify whether to write test case names to the output.
type LabelsLevel =
    | Default
    | Off
    | On
    | All
    member x.ParamString =
        match x with
        | Default -> ""
        | Off -> "Off"
        | On -> "On"
        | All -> "All"

/// The NUnit 3 Console Parameters type. FAKE will use [NUnit3Defaults](fake-testing-nunit3.html) for values not provided.
///
/// For reference, see: [NUnit3 command line options](https://github.com/nunit/docs/wiki/Console-Command-Line)
type NUnit3Params =
    { /// The path to the NUnit3 console runner: `nunit3-console.exe`
      ToolPath : string

      /// The name (or path) of a file containing a list of tests to run or explore, one per line.
      Testlist : string

      /// An expression indicating which tests to run. It may specify test names, classes, methods,
      /// catgories or properties comparing them to actual values with the operators ==, !=, =~ and !~.
      /// See [NUnit documentation](https://github.com/nunit/docs/wiki/Test-Selection-Language) for a full description of the syntax.
      Where : string

      /// Name of a project configuration to load (e.g.: Debug)
      Config : string

      /// Controls how NUnit loads tests in processes. See [NUnit3ProcessModel](fake-testing-nunit3-nunit3processmodel.html)
      ProcessModel : NUnit3ProcessModel

      /// Number of agents that may be allowed to run simultaneously assuming you are not running inprocess.
      /// If not specified, all agent processes run tests at the same time, whatever the number of assemblies.
      /// This setting is used to control running your assemblies in parallel.
      Agents : int option

      /// Controls how NUnit loads tests in processes. See: [NUnit3ProcessModel](fake-testing-nunit3-nunit3domainmodel.html).
      Domain : NUnit3DomainModel

      /// Allows you to specify the version of the runtime to be used in executing tests.
      /// Default value is runtime under which the assembly was built. See: [NUnit3Runtime](fake-testing-nunit3-nunit3runtime.html).
      Framework : NUnit3Runtime

      /// Run tests in a 32-bit process on 64-bit systems.
      Force32bit : bool

      /// Dispose each test runner after it has finished running its tests
      DisposeRunners : bool

      /// The default timeout to be used for test cases. If any test exceeds the timeout value, it is cancelled and reported as an error.
      TimeOut : TimeSpan

      /// Set the random seed used to generate test cases
      Seed : int

      /// Specify the NUMBER of worker threads to be used in running tests.
      /// This setting is used to control running your tests in parallel and is used in conjunction with the Parallelizable Attribute.
      /// If not specified, workers defaults to the number of processors on the machine, or 2, whichever is greater.
      Workers : int option

      /// Causes execution of the test run to terminate immediately on the first test failure or error.
      StopOnError : bool

      /// Path of the directory to use for output files.
      WorkingDir : string

      /// File path to contain text output from the tests.
      OutputDir : string

      /// File path to contain error output from the tests.
      ErrorDir : string

      /// Output specs for saving the test results. Default value is TestResult.xml
      /// Passing empty list does not save any result (--noresult option in NUnit)
      /// For more information, see: [NUnit3 command line options](https://github.com/nunit/docs/wiki/Console-Command-Line)
      ResultSpecs : string list

      /// Tells .NET to copy loaded assemblies to the shadowcopy directory.
      ShadowCopy : bool

      /// Turns on use of TeamCity service messages.
      TeamCity : bool

      /// Specify whether to write test case names to the output.
      Labels: LabelsLevel

      /// Default: [TestRunnerErrorLevel](fake-unittestcommon-testrunnererrorlevel.html).Error
      ErrorLevel : NUnit3ErrorLevel

      /// Controls the trace logs NUnit3 will output, defaults to Off
      TraceLevel : NUnit3TraceLevel

      /// Skips assemblies that do not contain tests or assemblies that contain the NUnit.Framework.NonTestAssemblyAttribute without raising an error
      SkipNonTestAssemblies : bool

      /// A test parameter specified in the form name=value. Multiple parameters may be specified, separated by semicolons
      Params : string
    }

/// The [NUnit3Params](fake-testing-nunit3-nunit3params.html) default parameters.
///
/// - `ToolPath` - The `nunit-console.exe` path if it exists in `tools/Nunit/`.
/// - `Testlist` - `""`
/// - `Where` - `""`
/// - `Config` - `""`
/// - `ProcessModel` - `DefaultProcessModel`
/// - `Agents` - `None`
/// - `Domain` - `DefaultDomainModel`
/// - `Framework` - `""`
/// - `Force32bit` - `false`
/// - `DisposeRunners` - `false`
/// - `Timeout` - `2147483647 milliseconds`
/// - `Seed` - `-1` (negative seed is ignored by NUnit and is not sent to it)
/// - `Workers` - `None`
/// - `StopOnError` - `false`
/// - `WorkingDir` - `""`
/// - `OutputDir` - `""`
/// - `ErrorDir` - `""`
/// - `ResultSpecs` - `"TestResult.xml"`
/// - `ShadowCopy` - `false`
/// - `TeamCity` - `false`
/// - `ErrorLevel` - `Error`
/// - `TraceLevel` - `Default` (By default NUnit3 sets this to off internally)
/// - `SkipNonTestAssemblies` - `false`
/// - `Params` - `""`
/// ## Defaults
let NUnit3Defaults =
    {
      ToolPath = Fake.IO.Globbing.Tools.findToolInSubPath  "nunit3-console.exe" (Shell.pwd() @@ "tools" @@ "Nunit")
      Testlist = ""
      Where = ""
      Config = ""
      ProcessModel = DefaultProcessModel
      Agents = None
      Domain = DefaultDomainModel
      Framework = NUnit3Runtime.Default
      Force32bit = false
      DisposeRunners = false
      TimeOut = TimeSpan.FromMilliseconds((float)Int32.MaxValue)
      Seed = -1
      Workers = None
      StopOnError = false
      WorkingDir = ""
      OutputDir = ""
      ErrorDir = ""
      ResultSpecs = [Shell.pwd() @@ "TestResult.xml"]
      ShadowCopy = false
      TeamCity = false
      Labels = LabelsLevel.Default
      ErrorLevel = NUnit3ErrorLevel.Error
      TraceLevel= NUnit3TraceLevel.Default
      SkipNonTestAssemblies = false
      Params = ""
    }

/// Tries to detect the working directory as specified in the parameters or via TeamCity settings
/// [omit]
let getWorkingDir parameters =
    Seq.find String.isNotNullOrEmpty [ parameters.WorkingDir
                                       Environment.environVar ("teamcity.build.workingDir")
                                       "." ]
    |> Path.GetFullPath

/// Builds the command line arguments from the given parameter record and the given assemblies.
let buildArgs (parameters:NUnit3Params) (assemblies: string seq) =
    let appendResultString results sb =
        match results, sb with
        | [], sb -> StringBuilder.append "--noresult" sb
        | x, sb when x = NUnit3Defaults.ResultSpecs -> sb
        | results, sb -> (sb, results) ||> Seq.fold (fun builder str -> StringBuilder.append (sprintf "--result=%s" str) builder)

    StringBuilder()
    |> StringBuilder.append "--noheader"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Testlist "--testlist="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Where "--where="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Config "--config="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.ProcessModel.ParamString "--process="
    |> StringBuilder.appendIfSome parameters.Agents (sprintf "--agents=%i")
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Domain.ParamString "--domain="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Framework.ParamString "--framework="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Labels.ParamString "--labels="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.TraceLevel.ParamString "--trace="
    |> StringBuilder.appendIfTrue parameters.Force32bit "--x86"
    |> StringBuilder.appendIfTrue parameters.DisposeRunners "--dispose-runners"
    |> StringBuilder.appendIfTrue (parameters.TimeOut <> NUnit3Defaults.TimeOut) (sprintf "--timeout=%i" (int parameters.TimeOut.TotalMilliseconds))
    |> StringBuilder.appendIfTrue (parameters.Seed >= 0) (sprintf "--seed=%i" parameters.Seed)
    |> StringBuilder.appendIfSome parameters.Workers (sprintf "--workers=%i")
    |> StringBuilder.appendIfTrue parameters.StopOnError "--stoponerror"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.WorkingDir "--work="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.OutputDir "--output="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.ErrorDir "--err="
    |> appendResultString parameters.ResultSpecs
    |> StringBuilder.appendIfTrue parameters.ShadowCopy "--shadowcopy"
    |> StringBuilder.appendIfTrue parameters.TeamCity "--teamcity"
    |> StringBuilder.appendIfTrue parameters.SkipNonTestAssemblies "--skipnontestassemblies"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Params "--params="
    |> StringBuilder.appendFileNamesIfNotNull assemblies
    |> StringBuilder.toText

let internal createProcess createTempFile (setParams : NUnit3Params -> NUnit3Params) (assemblies : string[]) =
    let parameters = NUnit3Defaults |> setParams
    if Array.isEmpty assemblies then failwith "NUnit: cannot run tests (the assembly list is empty)."
    let tool = parameters.ToolPath
    let generatedArgs = buildArgs parameters assemblies
    //let processTimeout = TimeSpan.MaxValue // Don't set a process timeout. The timeout is per test.
    
    let path = createTempFile()
    let argLine = Args.toWindowsCommandLine [ (sprintf "@%s" path) ]
    CreateProcess.fromRawCommandLine tool argLine
    |> CreateProcess.withFramework
    |> CreateProcess.withWorkingDirectory (getWorkingDir parameters)
    //|> CreateProcess.withTimeout processTimeout
    |> CreateProcess.addOnSetup (fun () ->
        File.WriteAllText(path, generatedArgs)
        Trace.trace(sprintf "Saved args to '%s' with value: %s" path generatedArgs)
    )
    |> CreateProcess.addOnFinally (fun () ->
        File.Delete(path)
    )
    |> CreateProcess.addOnExited (fun result exitCode ->
        let errorDescription error =
            match error with
            | OK -> "OK"
            | TestsFailed -> sprintf "NUnit test failed (%d)." error
            | FatalError x -> sprintf "NUnit test failed. Process finished with exit code %s (%d)." x error

        match parameters.ErrorLevel with
        | NUnit3ErrorLevel.DontFailBuild ->
            match exitCode with
            | OK | TestsFailed -> ()
            | _ -> raise (FailedTestsException(errorDescription exitCode))
        | NUnit3ErrorLevel.Error | FailOnFirstError ->
            match exitCode with
            | OK -> ()
            | _ -> raise (FailedTestsException(errorDescription exitCode))
    )

let run (setParams : NUnit3Params -> NUnit3Params) (assemblies : string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details = assemblies |> String.separated ", "
    use __ = Trace.traceTask "NUnit" details
    createProcess Path.GetTempFileName setParams assemblies
    |> Proc.run

    __.MarkSuccess()