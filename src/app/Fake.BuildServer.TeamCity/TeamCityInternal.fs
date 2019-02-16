/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core

module internal TeamCityWriter =

    /// Encapsulates special chars
    let private encapsulateSpecialChars text =
        text
        |> String.replace "|" "||"
        |> String.replace "'" "|'"
        |> String.replace "\n" "|n"
        |> String.replace "\r" "|r"
        |> String.replace "[" "|["
        |> String.replace "]" "|]"

    let private singleLine = String.removeLineBreaks >> encapsulateSpecialChars

    type private TeamCityMessage = 
        | OneParamMultiLine of (Printf.StringFormat<string -> string>)*string
        | OneParamSingleLine of (Printf.StringFormat<string -> string>)*string
        | TwoParamMultiLine of (Printf.StringFormat<string -> string ->string>)*string*string
        | TwoParamSingleLineBoth of (Printf.StringFormat<string -> string -> string>)*string*string
        | TwoParamSingleLineParam1 of (Printf.StringFormat<string -> string -> string>)*string*string
        | ThreeParamSingleLineAll of (Printf.StringFormat<string -> string -> string -> string>)*string*string*string
        | ThreeParamSingleLineParam1 of (Printf.StringFormat<string -> string -> string -> string>)*string*string*string
        | FiveParamSingleLineParam1 of (Printf.StringFormat<string -> string -> string -> string -> string -> string>)*string*string*string*string*string

    /// Send message to TeamCity with single param
    let private sendToTeamCity (message:TeamCityMessage) =
        let content = match message with
                      | OneParamMultiLine (fmt, param1) -> sprintf fmt (encapsulateSpecialChars param1)
                      | OneParamSingleLine (fmt, param1) -> sprintf fmt (singleLine param1)
                      | TwoParamMultiLine (fmt, param1, param2) -> sprintf fmt (encapsulateSpecialChars param1) (encapsulateSpecialChars param2)
                      | TwoParamSingleLineBoth (fmt, param1, param2) -> sprintf fmt (singleLine param1) (singleLine param2)
                      | TwoParamSingleLineParam1 (fmt, param1, param2) -> sprintf fmt (singleLine param1) (encapsulateSpecialChars param2)
                      | ThreeParamSingleLineAll (fmt, param1, param2, param3) -> sprintf fmt (singleLine param1) (singleLine param2) (singleLine param3)
                      | ThreeParamSingleLineParam1 (fmt, param1, param2, param3) -> sprintf fmt (singleLine param1) (encapsulateSpecialChars param2) (encapsulateSpecialChars param3)
                      | FiveParamSingleLineParam1 (fmt, param1, param2, param3, param4, param5) -> sprintf fmt (singleLine param1) (encapsulateSpecialChars param2) (encapsulateSpecialChars param3) (encapsulateSpecialChars param4) (encapsulateSpecialChars param5)

        // printf is racing with others in parallel mode
        System.Console.WriteLine("{0}", content)
  
    /// Open Named Block
    let internal sendOpenBlock name description = sendToTeamCity (TeamCityMessage.TwoParamSingleLineBoth("##teamcity[blockOpened name='%s' description='%s']", name, description))

    /// Close Named Block
    let internal sendCloseBlock name = sendToTeamCity (TeamCityMessage.OneParamSingleLine("##teamcity[blockClosed name='%s']", name))

    /// Build status
    let internal sendBuildStatus status text = sendToTeamCity (TeamCityMessage.TwoParamSingleLineParam1("##teamcity[buildStatus status='%s' text='%s']", status, text))

    /// Build Problem
    let internal sendBuildProblem description = sendToTeamCity (TeamCityMessage.OneParamMultiLine("##teamcity[buildProblem description='%s']", description))

    // Import Data
    let internal sendImportData typ file = sendToTeamCity(TeamCityMessage.TwoParamSingleLineBoth("##teamcity[importData type='%s' file='%s']", typ, file))

    // Import Data With Tool
    let internal sendImportDataWithTool typ tool path = sendToTeamCity(TeamCityMessage.ThreeParamSingleLineAll("##teamcity[importData type='%s' tool='%s' path='%s']", typ, tool, path))

    // Import Data With Tool
    let internal sendDotNetCoverage typ value = sendToTeamCity(TeamCityMessage.TwoParamSingleLineBoth("##teamcity[dotNetCoverage %s='%s']", typ, value))

    /// Test Started
    let internal sendTestStarted name = sendToTeamCity(TeamCityMessage.OneParamMultiLine("##teamcity[testStarted name='%s' captureStandardOutput='true']", name))

    /// Test Finshed
    let internal sendTestFinished name duration = sendToTeamCity(TeamCityMessage.TwoParamMultiLine("##teamcity[testFinished name='%s' duration='%s']", name, duration))

    /// Test Ignored
    let internal sendTestIgnored name message = sendToTeamCity(TeamCityMessage.TwoParamMultiLine("##teamcity[testIgnored name='%s' message='%s']", name, message))

    /// Test Std Out
    let internal sendTestStdOut name out = sendToTeamCity(TeamCityMessage.TwoParamMultiLine("##teamcity[testStdOut name='%s' out='%s']", name, out))

    /// Test Std Error
    let internal sendTestStdError name out = sendToTeamCity(TeamCityMessage.TwoParamMultiLine("##teamcity[testStdErr name='%s' out='%s']", name, out))

    /// Test Suite Finished
    let internal sendTestSuiteFinished name = sendToTeamCity(TeamCityMessage.OneParamSingleLine("##teamcity[testSuiteFinished name='%s']", name))

    /// Test Suite Started
    let internal sendTestSuiteStarted name = sendToTeamCity(TeamCityMessage.OneParamSingleLine("##teamcity[testSuiteStarted name='%s']", name))

    /// Progress Message
    let internal sendProgressMessage message = sendToTeamCity(TeamCityMessage.OneParamSingleLine("##teamcity[progressMessage '%s']", message))

    /// Progress Start
    let internal sendProgressStart message = sendToTeamCity(TeamCityMessage.OneParamSingleLine("##teamcity[progressStart '%s']", message))

    /// Progress Finish
    let internal sendProgressFinish message = sendToTeamCity(TeamCityMessage.OneParamSingleLine("##teamcity[progressFinish '%s']", message))

    /// Publish Artifact
    let internal sendPublishArtifact path = sendToTeamCity(TeamCityMessage.OneParamSingleLine("##teamcity[publishArtifacts '%s']", path))

    /// Publish Named Artifact
    let internal sendPublishNamedArtifact name path = sendToTeamCity(TeamCityMessage.TwoParamSingleLineBoth("##teamcity[publishArtifacts '%s => %s']", path, name))

    /// Build Number
    let internal sendBuildNumber buildNumber = sendToTeamCity(TeamCityMessage.OneParamSingleLine("##teamcity[buildNumber '%s']", buildNumber))

    /// Build Statistic
    let internal sendBuildStatistic key value = sendToTeamCity(TeamCityMessage.TwoParamSingleLineBoth("##teamcity[buildStatisticValue key='%s' value='%s']", key, value))

    /// Set Parameter
    let internal sendSetParameter name value = sendToTeamCity(TeamCityMessage.TwoParamSingleLineBoth("##teamcity[setParameter name='%s' value='%s']", name, value))

    /// Test Failure
    let internal sendTestFailed name message details = sendToTeamCity(TeamCityMessage.ThreeParamSingleLineParam1("##teamcity[testFailed name='%s' message='%s' details='%s']", name, message, details))
    
    /// Comparison Failed
    let internal sendComparisonFailed name message details expected actual = sendToTeamCity(TeamCityMessage.FiveParamSingleLineParam1("##teamcity[testFailed type='comparisonFailure' name='%s' message='%s' details='%s' expected='%s' actual='%s']", name, message, details, expected, actual))

    /// Message
    let internal sendMessage status text = sendToTeamCity(TeamCityMessage.TwoParamSingleLineParam1("##teamcity[message status='%s' text='%s']", status, text))
    
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

module internal TeamCityBuildParameters =
    let private get (fileName: string option) =
        match fileName with
        | Some fileName when not (isNull fileName) && (File.Exists fileName) ->
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

    let private systemFile = Environment.environVarOrNone "TEAMCITY_BUILD_PROPERTIES_FILE"
    let private system = lazy(get (systemFile))

    let getAllSystem () = system.Value

    let private configurationFile = lazy (getAllSystem() |> Map.tryFind "teamcity.configuration.properties.file")
    let private configuration = lazy (get configurationFile.Value)

    let getAllConfiguration () = configuration.Value

    let private runnerFile = lazy (getAllSystem() |> Map.tryFind "teamcity.runner.properties.file")
    let private runner = lazy (get runnerFile.Value)
    
    let getAllRunner () = runner.Value

    let private all = lazy (
        if BuildServer.buildServer = BuildServer.TeamCity then
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

    let getAll () = all.Value


module internal TeamCityRest =
    open Fake.Net

    /// [omit]
    let prepareURL restURL (serverURL : string) = serverURL.Trim '/' + restURL

    /// Returns the REST version of the TeamCity server
    let getRESTVersion serverURL username password =
        serverURL
        |> prepareURL "/httpAuth/app/rest/version"
        |> Http.get username password

    /// Record type which stores VCSRoot properties
    type VCSRoot =
        { URL : string
          Properties : Map<string, string>
          VCSName : string
          Name : string }

    /// Record type which stores Build properties
    type Build =
        { ID : string
          Number : string
          Status : string
          WebURL : string }

    /// Record type which stores Build configuration properties
    type BuildConfiguration =
        { ID : string
          Name : string
          WebURL : string
          ProjectID : string
          Paused : bool
          Description : string
          Builds : Build seq }

    /// Record type which stores TeamCity project properties
    type Project =
        { ID : string
          Name : string
          Description : string
          WebURL : string
          Archived : bool
          BuildConfigs : string seq }

    /// [omit]
    let getFirstNode serverURL username password url =
        serverURL
        |> prepareURL url
        |> Http.get username password
        |> Xml.createDoc
        |> Xml.getDocElement

    let private parseBooleanOrFalse s =
        let ok, parsed = System.Boolean.TryParse s
        if ok then parsed else false

    /// Gets information about a build configuration from the TeamCity server.
    let getBuildConfig serverURL username password id =
        sprintf "/httpAuth/app/rest/buildTypes/id:%s" id
        |> getFirstNode serverURL username password
        |> Xml.parse "buildType" (fun n ->
               { ID = Xml.getAttribute "id" n
                 Name = Xml.getAttribute "name" n
                 Description = Xml.getAttribute "description" n
                 WebURL = Xml.getAttribute "webUrl" n
                 Paused = Xml.getAttribute "paused" n |> parseBooleanOrFalse
                 ProjectID = Xml.parseSubNode "project" (Xml.getAttribute "id") n
                 Builds = [] })

    /// Gets informnation about a project from the TeamCity server.
    let getProject serverURL username password id =
        sprintf "/httpAuth/app/rest/projects/id:%s" id
        |> getFirstNode serverURL username password
        |> Xml.parse "project" (fun n ->
               { ID = Xml.getAttribute "id" n
                 Name = Xml.getAttribute "name" n
                 Description = Xml.getAttribute "description" n
                 WebURL = Xml.getAttribute "webUrl" n
                 Archived = Xml.getAttribute "archived" n |> parseBooleanOrFalse
                 BuildConfigs = Xml.parseSubNode "buildTypes" Xml.getChilds n |> Seq.map (Xml.getAttribute "id") })

    /// Gets all projects on the TeamCity server.
    let getProjects serverURL username password =
        getFirstNode serverURL username password "/httpAuth/app/rest/projects"
        |> Xml.parse "projects" Xml.getChilds
        |> Seq.map (Xml.getAttribute "id")
