[<AutoOpen>]
/// Contains helper functions which allow FAKE to communicate with a TeamCity agent
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity instead")>]
module Fake.TeamCityHelper

/// Encapsulates special chars
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the Trace module instead")>]
let inline EncapsulateSpecialChars text =
    text
    |> replace "|" "||"
    |> replace "'" "|'"
    |> replace "\n" "|n"
    |> replace "\r" "|r"
    |> replace "[" "|["
    |> replace "]" "|]"

[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the Trace module instead")>]
let scrub = RemoveLineBreaks >> EncapsulateSpecialChars

/// Send message to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the Trace module instead")>]
let sendToTeamCity format message =
    if buildServer = TeamCity then
        message
        |> scrub
        |> sprintf format
        |> fun m -> postMessage (LogMessage(m, true))

/// Send message to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the Trace module instead")>]
let sendStrToTeamCity s =
    if buildServer = TeamCity then postMessage (LogMessage(RemoveLineBreaks s, true))

/// Open Named Block
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the TeamCity.block instead")>]
let sendOpenBlock = sendToTeamCity "##teamcity[blockOpened name='%s']"

/// Close Named Block
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the TeamCity.block instead")>]
let sendCloseBlock = sendToTeamCity "##teamcity[blockClosed name='%s']"

/// Open Named Block that will be closed when the block is disposed
/// Usage: `use __ = teamCityBlock "My Block"`
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the TeamCity.block instead")>]
let teamCityBlock name =
    sendOpenBlock name
    { new System.IDisposable
        with member __.Dispose() = sendCloseBlock name }

/// Sends an error to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use the TeamCity.<> instead")>]
let sendTeamCityError error = sendToTeamCity "##teamcity[buildStatus status='FAILURE' text='%s']" error

/// Sends an NUnit results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityNUnitImport path = sendToTeamCity "##teamcity[importData type='nunit' file='%s']" path

/// Sends an FXCop results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityFXCopImport path = sendToTeamCity "##teamcity[importData type='FxCop' path='%s']" path

/// Sends an JUnit Ant task results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityJUnitImport path = sendToTeamCity "##teamcity[importData type='junit' path='%s']" path

/// Sends an Maven Surefire results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCitySurefireImport path = sendToTeamCity "##teamcity[importData type='surefire' path='%s']" path

/// Sends an MSTest results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityMSTestImport path = sendToTeamCity "##teamcity[importData type='mstest' path='%s']" path

/// Sends an Google Test results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityGTestImport path = sendToTeamCity "##teamcity[importData type='gtest' path='%s']" path

/// Sends an Checkstyle results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityCheckstyleImport path = sendToTeamCity "##teamcity[importData type='checkstyle' path='%s']" path

/// Sends an FindBugs results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityFindBugsImport path = sendToTeamCity "##teamcity[importData type='findBugs' path='%s']" path

/// Sends an JSLint results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityJSLintImport path = sendToTeamCity "##teamcity[importData type='jslint' path='%s']" path

/// Sends an ReSharper inspectCode.exe results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityReSharperInspectCodeImport path = sendToTeamCity "##teamcity[importData type='ReSharperInspectCode' path='%s']" path

/// Sends an FxCop inspection results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityFxCopImport path = sendToTeamCity "##teamcity[importData type='FxCop' path='%s']" path

/// Sends an PMD inspections results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityPmdImport path = sendToTeamCity "##teamcity[importData type='pmd' path='%s']" path

/// Sends an PMD Copy/Paste Detector results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityPmdCpdImport path = sendToTeamCity "##teamcity[importData type='pmdCpd' path='%s']" path

/// Sends an ReSharper dupfinder.exe results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityDotNetDupFinderImport path = sendToTeamCity "##teamcity[importData type='DotNetDupFinder' path='%s']" path

/// Sends an dotcover, partcover, ncover or ncover3 results filename to TeamCity
[<System.Obsolete("This function does not specify the type of coverage tool used to generate the report.  Use 'sendTeamCityDotNetCoverageImportForTool' instead")>]
let sendTeamCityDotNetCoverageImport path = sendToTeamCity "##teamcity[importData type='dotNetCoverage' path='%s']" path

[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
type TeamCityDotNetCoverageTool = | DotCover | PartCover | NCover | NCover3 with override x.ToString() = match x with | DotCover -> "dotcover" | PartCover -> "partcover" | NCover -> "ncover" | NCover3 -> "ncover3"
/// Sends an dotcover, partcover, ncover or ncover3 results filename to TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let sendTeamCityDotNetCoverageImportForTool path (tool : TeamCityDotNetCoverageTool) =
    sprintf "##teamcity[importData type='dotNetCoverage' tool='%s' path='%s']" (string tool |> scrub) (path |> scrub)
    |> sendStrToTeamCity

/// Sends the full path to the dotCover home folder to override the bundled dotCover to TeamCity
let sendTeamCityDotCoverHome = sendToTeamCity "##teamcity[dotNetCoverage dotcover_home='%s']"

/// Sends the full path to NCover installation folder to TeamCity
let sendTeamCityNCover3Home = sendToTeamCity "##teamcity[dotNetCoverage ncover3_home='%s']"

/// Sends arguments for the NCover report generator to TeamCity
let sendTeamCityNCover3ReporterArgs = sendToTeamCity "##teamcity[dotNetCoverage ncover3_reporter_args='%s']"

/// Sends the path to NCoverExplorer to TeamCity
let sendTeamCityNCoverExplorerTool = sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_tool='%s']"

/// Sends additional arguments for NCover 1.x to TeamCity
let sendTeamCityNCoverExplorerToolArgs = sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_tool_args='%s']"

/// Sends the value for NCover /report: argument to TeamCity
let sendTeamCityNCoverReportType : int -> unit = string >> sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_report_type='%s']"

/// Sends the value for NCover  /sort: argument to TeamCity
let sendTeamCityNCoverReportOrder : int -> unit = string >> sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_report_order='%s']"

/// Send the PartCover xslt transformation rules (Input xlst and output files) to TeamCity
let sendTeamCityPartCoverReportXslts : seq<string * string> -> unit =
    Seq.map (fun (xslt, output) -> sprintf "%s=>%s" xslt output)
    >> Seq.map EncapsulateSpecialChars
    >> String.concat "|n"
    >> sprintf "##teamcity[dotNetCoverage partcover_report_xslts='%s']"
    >> sendStrToTeamCity

/// Starts the test case.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let StartTestCase testCaseName =
    sendToTeamCity "##teamcity[testStarted name='%s' captureStandardOutput='true']" testCaseName

/// Finishes the test case.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let FinishTestCase testCaseName (duration : System.TimeSpan) =
    let duration =
        duration.TotalMilliseconds
        |> round
        |> string
    sprintf "##teamcity[testFinished name='%s' duration='%s']" (EncapsulateSpecialChars testCaseName) duration
    |> sendStrToTeamCity

/// Ignores the test case.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let IgnoreTestCase name message =
    StartTestCase name
    sprintf "##teamcity[testIgnored name='%s' message='%s']" (EncapsulateSpecialChars name)
        (EncapsulateSpecialChars message) |> sendStrToTeamCity


/// Ignores the test case.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let IgnoreTestCaseWithDetails name message details =
    IgnoreTestCase name (message + " " + details)

/// Finishes the test suite.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let FinishTestSuite testSuiteName =
    EncapsulateSpecialChars testSuiteName |> sendToTeamCity "##teamcity[testSuiteFinished name='%s']"

/// Starts the test suite.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let StartTestSuite testSuiteName =
    EncapsulateSpecialChars testSuiteName |> sendToTeamCity "##teamcity[testSuiteStarted name='%s']"

/// Reports the progress.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.<> instead")>]
let ReportProgress message = EncapsulateSpecialChars message |> sendToTeamCity "##teamcity[progressMessage '%s']"

/// Reports the progress start.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.<> instead")>]
let ReportProgressStart message = EncapsulateSpecialChars message |> sendToTeamCity "##teamcity[progressStart '%s']"

/// Reports the progress end.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.<> instead")>]
let ReportProgressFinish message = EncapsulateSpecialChars message |> sendToTeamCity "##teamcity[progressFinish '%s']"

/// Create  the build status.
/// [omit]
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.<> instead")>]
let buildStatus status message =
    sprintf "##teamcity[buildStatus status='%s' text='%s']" (EncapsulateSpecialChars status) (EncapsulateSpecialChars message)

/// Reports the build status.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.<> instead")>]
let ReportBuildStatus status message = buildStatus status message |> sendStrToTeamCity

/// Publishes an artifact on the TeamcCity build server.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let PublishArtifact path = EncapsulateSpecialChars path |> sendToTeamCity "##teamcity[publishArtifacts '%s']"

[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.publish instead")>]
let PublishArticfact path = PublishArtifact path

/// Sets the TeamCity build number.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.setBuildNumber instead")>]
let SetBuildNumber buildNumber = EncapsulateSpecialChars buildNumber |> sendToTeamCity "##teamcity[buildNumber '%s']"

/// Reports a build statistic.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.<> instead")>]
let SetBuildStatistic key value =
    sprintf "##teamcity[buildStatisticValue key='%s' value='%s']" (EncapsulateSpecialChars key)
        (EncapsulateSpecialChars value) |> sendStrToTeamCity

/// Reports a parameter value
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.<> instead")>]
let SetTeamCityParameter name value =
    sprintf "##teamcity[setParameter name='%s' value='%s']" (EncapsulateSpecialChars name)
        (EncapsulateSpecialChars value) |> sendStrToTeamCity

/// Reports a failed test.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let TestFailed name message details =
    sprintf "##teamcity[testFailed name='%s' message='%s' details='%s']" (EncapsulateSpecialChars name)
        (EncapsulateSpecialChars message) (EncapsulateSpecialChars details) |> sendStrToTeamCity

/// Reports a failed comparison.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use Trace.<> instead")>]
let ComparisonFailure name message details expected actual =
    sprintf
        "##teamcity[testFailed type='comparisonFailure' name='%s' message='%s' details='%s' expected='%s' actual='%s']"
        (EncapsulateSpecialChars name) (EncapsulateSpecialChars message) (EncapsulateSpecialChars details)
        (EncapsulateSpecialChars expected) (EncapsulateSpecialChars actual) |> sendStrToTeamCity

/// The Version of the TeamCity server. This property can be used to determine the build is run within TeamCity.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.Version instead")>]
let TeamCityVersion = environVarOrNone "TEAMCITY_VERSION"

/// The Name of the project the current build belongs to or None if it's not on TeamCity.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.ProjectName instead")>]
let TeamCityProjectName = environVarOrNone "TEAMCITY_PROJECT_NAME"

/// The Name of the Build Configuration the current build belongs to or None if it's not on TeamCity.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.BuildConfigurationName instead")>]
let TeamCityBuildConfigurationName = environVarOrNone "TEAMCITY_BUILDCONF_NAME"

/// Is set to true if the build is a personal one.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.BuildIsPersonal instead")>]
let TeamCityBuildIsPersonal =
    match environVarOrNone "BUILD_IS_PERSONAL" with
    | Some _ -> true
    | None -> false

/// The Build number assigned to the build by TeamCity using the build number format or None if it's not on TeamCity.
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.BuildNumber instead")>]
let TeamCityBuildNumber = environVarOrNone "BUILD_NUMBER"

module private JavaPropertiesFile =
    open System.Text
    open System.IO
    open System.Globalization

    type PropertiesFileEntry =
    | Comment of text : string
    | KeyValue of key : string * value : string

    module private Parser =
        type CharReader = unit -> char option

        let inline (|IsWhitespace|_|) c =
            match c with
            | Some c -> if c = ' ' || c = '\t' || c = '\u00ff' then Some c else None
            | None -> None

        type IsEof =
            | Yes = 1y
            | No = 0y

        let rec readToFirstChar (c: char option) (reader: CharReader) =
            match c with
            | IsWhitespace _ ->
                readToFirstChar (reader ()) reader
            | Some '\r'
            | Some '\n' ->
                None, IsEof.No
            | Some _ -> c, IsEof.No
            | None -> None, IsEof.Yes

        let inline (|EscapeSequence|_|) c =
            match c with
            | Some c ->
                if c = 'r' || c = 'n' || c = 'u' || c = 'f' || c = 't' || c = '"' || c = ''' || c = '\\' then
                    Some c
                else
                    None
            | None -> None

        let inline isHex c = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')

        let readEscapeSequence (c: char) (reader: CharReader) =
            match c with
            | 'r' -> '\r'
            | 'n' -> '\n'
            | 'f' -> '\f'
            | 't' -> '\t'
            | 'u' ->
                match reader(), reader(), reader(), reader() with
                | Some c1, Some c2, Some c3, Some c4 when isHex c1 && isHex c2 && isHex c3 && isHex c4 ->
                    let hex = System.String([|c1;c2;c3;c4|])
                    let value = System.UInt16.Parse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
                    char value
                | _ ->
                    failwith "Invalid unicode escape"
            | _ -> c

        let inline readKey (c: char option) (reader: CharReader) (buffer: StringBuilder) =
            let rec recurseEnd (result: string) =
                match reader () with
                | Some ':'
                | Some '='
                | IsWhitespace _ -> recurseEnd result
                | Some '\r'
                | Some '\n' -> result, false, None, IsEof.No
                | None -> result, false, None, IsEof.Yes
                | Some c -> result, true, Some c, IsEof.No
            let rec recurse (c: char option) (buffer: StringBuilder) (escaping: bool) =
                match c with
                | EscapeSequence c when escaping ->
                    let realChar = readEscapeSequence c reader
                    recurse (reader()) (buffer.Append(realChar)) false
                | Some ' ' -> recurseEnd (buffer.ToString())
                | Some ':'
                | Some '=' when not escaping -> recurseEnd (buffer.ToString())
                | Some '\r'
                | Some '\n' -> buffer.ToString(), false, None, IsEof.No
                | None -> buffer.ToString(), false, None, IsEof.Yes
                | Some '\\' -> recurse (reader ()) buffer true
                | Some c -> recurse (reader ()) (buffer.Append(c)) false

            recurse c buffer false

        let rec readComment (reader: CharReader) (buffer: StringBuilder) =
            match reader () with
            | Some '\r'
            | Some '\n' ->
                Some (Comment (buffer.ToString())), IsEof.No
            | None ->
                Some(Comment (buffer.ToString())), IsEof.Yes
            | Some c ->
                readComment reader (buffer.Append(c))

        let inline readValue (c: char option) (reader: CharReader) (buffer: StringBuilder) =
            let rec recurse (c: char option) (buffer: StringBuilder) (escaping: bool) (cr: bool) (lineStart: bool) =
                match c with
                | EscapeSequence c when escaping ->
                    let realChar = readEscapeSequence c reader
                    recurse (reader()) (buffer.Append(realChar)) false false false
                | Some '\r'
                | Some '\n' ->
                    if escaping || (cr && c = Some '\n') then
                        recurse (reader ()) buffer false (c = Some '\r') true
                    else
                        buffer.ToString(), IsEof.No
                | None ->
                    buffer.ToString(), IsEof.Yes
                | Some _ when lineStart ->
                    let firstChar, _ = readToFirstChar c reader
                    recurse firstChar buffer false false false
                | Some '\\' -> recurse (reader ()) buffer true false false
                | Some c ->
                    recurse (reader()) (buffer.Append(c)) false false false

            recurse c buffer false false true

        let rec readLine (reader: CharReader) (buffer: StringBuilder) =
            match readToFirstChar (reader ()) reader with
            | Some '#', _
            | Some '!', _ ->
                readComment reader (buffer.Clear())
            | Some firstChar, _ ->
                let key, hasValue, c, isEof = readKey (Some firstChar) reader (buffer.Clear())
                let value, isEof =
                    if hasValue then
                        // We know that we aren't at the end of the buffer, but readKey can return None if it didn't need the next char
                        let firstChar = match c with | Some c -> Some c | None -> reader ()
                        readValue firstChar reader (buffer.Clear())
                    else
                        "", isEof
                Some (KeyValue(key, value)), isEof
            | None, isEof -> None, isEof

        let inline textReaderToReader (reader: TextReader) =
            let buffer = [| '\u0000' |]
            fun () ->
                let eof = reader.Read(buffer, 0, 1) = 0
                if eof then None else Some (buffer.[0])

        let parseWithReader reader =
            let buffer = StringBuilder(255)
            let mutable isEof = IsEof.No

            seq {
                while isEof <> IsEof.Yes do
                    let line, isEofAfterLine = readLine reader buffer
                    match line with
                    | Some line -> yield line
                    | None -> ()
                    isEof <- isEofAfterLine
            }

    let parseTextReader (reader: TextReader) =
        let reader = Parser.textReaderToReader reader
        Parser.parseWithReader reader

/// TeamCity build parameters
/// See [Predefined Build Parameters documentation](https://confluence.jetbrains.com/display/TCD18/Predefined+Build+Parameters) for more information
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.BuildParameters instead")>]
module TeamCityBuildParameters =
    open System
    open System.IO

    let private get (fileName: string option) =
        match fileName with
        | Some fileName when (fileName <> null) && (fileExists fileName) ->
            use stream = File.OpenRead(fileName)
            use reader = new StreamReader(stream)

            reader
            |> JavaPropertiesFile.parseTextReader
            |> Seq.choose(function
                | JavaPropertiesFile.Comment _ -> None
                | JavaPropertiesFile.KeyValue(k, v) -> Some (k,v))
            |> Map.ofSeq
        | _ ->
            Map.empty

    let private systemFile = Environment.GetEnvironmentVariable("TEAMCITY_BUILD_PROPERTIES_FILE")
    let private system = lazy(get (Some systemFile))

    /// Get all system parameters
    let getAllSystem () = system.Value

    /// Get the value of a system parameter by name
    let tryGetSystem name = system.Value |> Map.tryFind name

    let private configurationFile = lazy (tryGetSystem "teamcity.configuration.properties.file")
    let private configuration = lazy (get configurationFile.Value)

    /// Get all configuration parameters
    let getAllConfiguration () = configuration.Value

    /// Get the value of a configuration parameter by name
    let tryGetConfiguration name = configuration.Value |> Map.tryFind name

    let private runnerFile = lazy (tryGetSystem "teamcity.runner.properties.file")
    let private runner = lazy (get runnerFile.Value)

    /// Get all runner parameters
    let getAllRunner () = runner.Value

    /// Get the value of a runner parameter by name
    let tryGetRunner name = runner.Value |> Map.tryFind name

    let private all = lazy (
        if buildServer = TeamCity then
            seq {
                // Environment variables are available using 'env.foo' syntax in TeamCity configuration
                for pair in System.Environment.GetEnvironmentVariables() do
                    let pair = pair :?> System.Collections.DictionaryEntry
                    let key = pair.Key :?> string
                    let value = pair.Value :?> string
                    yield sprintf "env.%s" key, value

                // Runner variables aren't available in TeamCity configuration so we choose an arbitrary syntax of 'runner.foo'
                for pair in runner.Value do yield sprintf "runner.%s" pair.Key, pair.Value

                // System variables are prefixed with 'system.' as in TeamCity configuration
                for pair in system.Value do yield sprintf "system.%s" pair.Key, pair.Value

                for pair in configuration.Value do yield pair.Key, pair.Value
            }
            |> Map.ofSeq
        else
            Map.empty)

    /// Get all parameters
    /// System ones are prefixed with 'system.', runner ones with 'runner.' and environment variables with 'env.'
    let getAll () = all.Value

    /// Get the value of a parameter by name
    /// System ones are prefixed with 'system.', runner ones with 'runner.' and environment variables with 'env.'
    let tryGet name = all.Value |> Map.tryFind name

/// Get files changed between builds in TeamCity
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.ChangedFiles instead")>]
module TeamCityChangedFiles =
    /// The type of change that occured
    type ModificationType =
        | FileChanged
        | FileAdded
        | FileRemoved
        | FileNotChanged
        | DirectoryChanged
        | DirectoryAdded
        | DirectoryRemoved

    /// Describe a change between builds
    type FileChange = {
        /// Path of the file that changed, relative to the current checkout directory ('system.teamcity.build.checkoutDir')
        filePath: string
        /// Type of modification for the file
        modificationType: ModificationType
        ///
        revision: string option }

    let private getFileChanges' () =
        match TeamCityBuildParameters.tryGetSystem "teamcity.build.changedFiles.file" with
        | Some file when fileExists file ->
            Some [
                for line in ReadFile file do
                    let split = line.Split(':')
                    if split.Length = 3 then
                        let filePath = split.[0]
                        let modificationType =
                            match split.[1].ToUpperInvariant() with
                            | "CHANGED" -> FileChanged
                            | "ADDED" -> FileAdded
                            | "REMOVED" -> FileRemoved
                            | "NOT_CHANGED" -> FileNotChanged
                            | "DIRECTORY_CHANGED" -> DirectoryChanged
                            | "DIRECTORY_ADDED" -> DirectoryAdded
                            | "DIRECTORY_REMOVED" -> DirectoryRemoved
                            | _ -> failwithf "Unknown change type: %s" (split.[1])
                        let revision =
                            match split.[2] with
                            | "<personal>" -> None
                            | revision -> Some revision

                        yield { filePath = filePath; modificationType = modificationType; revision = revision }
                    else
                        failwithf "Unable to split change line: %s" line
            ]
        | _ -> None

    let private fileChanges = lazy (getFileChanges' ())

    /// Changed files (since previous build) that are included in this build
    /// See [the documentation](https://confluence.jetbrains.com/display/TCD18/Risk+Tests+Reordering+in+Custom+Test+Runner) for more information
    let get () = fileChanges.Value

let private getRecentlyFailedTests' () =
    match TeamCityBuildParameters.tryGetSystem "teamcity.tests.recentlyFailedTests.file" with
    | Some file when fileExists file -> Some(ReadFile file)
    | _ -> None

let private recentlyFailedTests = lazy (getRecentlyFailedTests' ())

/// Name of recently failing tests
/// See [the documentation](https://confluence.jetbrains.com/display/TCD18/Risk+Tests+Reordering+in+Custom+Test+Runner) for more information
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.RecentlyFailedTests instead")>]
let getTeamCityRecentlyFailedTests () = recentlyFailedTests.Value

/// Get the branch of the main VCS root
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.Branch instead")>]
let getTeamCityBranch () = TeamCityBuildParameters.tryGetConfiguration "vcsroot.branch"

/// Get the display name of the branch as shown in TeamCity
/// See [the documentation](https://confluence.jetbrains.com/display/TCD18/Working+with+Feature+Branches#WorkingwithFeatureBranches-branchSpec) for more information
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.BranchDisplayName instead")>]
let getTeamCityBranchName () =
    match TeamCityBuildParameters.tryGetConfiguration "teamcity.build.branch" with
    | Some _  as branch -> branch
    | None -> TeamCityBuildParameters.tryGetConfiguration "vcsroot.branch"

/// Get if the current branch is the one configured as default
[<System.Obsolete("please use nuget 'Fake.BuildServer.TeamCity', open Fake.BuildServer and use TeamCity.Environment.IsDefaultBranch instead")>]
let getTeamCityBranchIsDefault () =
    if buildServer = TeamCity then
        match TeamCityBuildParameters.tryGetConfiguration "teamcity.build.branch.is_default" with
        | Some "true" -> true
        | Some _ -> false
        | None ->
            // When only one branch is configured, TeamCity doesn't emit this parameter
            getTeamCityBranch().IsSome
    else
        false
