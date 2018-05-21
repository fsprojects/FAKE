/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO
open Microsoft.FSharp.Reflection
open System.Text.RegularExpressions

module internal TeamCityWriter =

    // Probably too slow...
(*
    // From https://gist.github.com/mausch/465668
    // I think we need some cache here...
    let PrintfFormatProc (worker: string * obj list -> 'd)  (query: PrintfFormat<'a, _, _, 'd>) : 'a = 
        if not (FSharpType.IsFunction typeof<'a>) then 
            unbox (worker (query.Value, [])) 
        else 
            let rec getFlattenedFunctionElements (functionType: Type) = 
                let domain, range = FSharpType.GetFunctionElements functionType 
                if not (FSharpType.IsFunction range) 
                then domain::[range] 
                else domain::getFlattenedFunctionElements(range) 
            let types = getFlattenedFunctionElements typeof<'a> 
            let rec proc (types: Type list) (values: obj list) (a: obj) : obj = 
                let values = a::values 
                match types with 
                | [x;_] -> 
                    let result = worker (query.Value, List.rev values) 
                    box result 
                | x::y::z::xs -> 
                    let cont = proc (y::z::xs) values 
                    let ft = FSharpType.MakeFunctionType(y,z) 
                    let cont = FSharpValue.MakeFunction(ft, cont) 
                    box cont 
                | _ -> failwith "shouldn't happen" 
            let handler = proc types [] 
            unbox (FSharpValue.MakeFunction(typeof<'a>, handler))
    let processor (format: string, values: obj list) =
        let stripFormatting s =
            let i = ref -1
            let eval (rxMatch: Match) =
                incr i
                sprintf "{%d}" !i
            Regex.Replace(s, "%.", eval)
        let newFormat = stripFormatting format
        let args =
            values
            |> List.map (sprintf "%O" >> scrub)
            |> List.toArray
        String.Format(newFormat, args)
        |> printfn "%s"

    /// Send message to TeamCity
    let sendToTeamCity format =
        PrintfFormatProc processor format
*)

    /// Encapsulates special chars
    let inline encapsulateSpecialChars text =
        text
        |> String.replace "|" "||"
        |> String.replace "'" "|'"
        |> String.replace "\n" "|n"
        |> String.replace "\r" "|r"
        |> String.replace "[" "|["
        |> String.replace "]" "|]"

    let scrub = String.removeLineBreaks >> encapsulateSpecialChars

    /// Send message to TeamCity
    let sendToTeamCity (format:Printf.StringFormat<string -> string>) message =
        sprintf format (scrub message)
        // printf is racing with others in parallel mode
        |> fun s -> System.Console.WriteLine("\n{0}", s)

    let sendToTeamCity2 (format:Printf.StringFormat<string -> string -> string>) param1 param2 =
        sprintf format (scrub param1) (scrub param2)
        // printf is racing with others in parallel mode
        |> fun s -> System.Console.WriteLine("\n{0}", s)

    let sendStrToTeamCity str =
        sprintf "%s" str
        // printf is racing with others in parallel mode
        |> fun s -> System.Console.WriteLine("\n{0}", s)

    /// Open Named Block
    let sendOpenBlock name description = sendToTeamCity2 "##teamcity[blockOpened name='%s' description='%s']" name description

    /// Close Named Block
    let sendCloseBlock = sendToTeamCity "##teamcity[blockClosed name='%s']"


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
    /// See [Predefined Build Parameters documentation](https://confluence.jetbrains.com/display/TCD10/Predefined+Build+Parameters) for more information
    module TeamCityBuildParameters =
        open System
        open System.IO

        let private get (fileName: string option) =
            match fileName with
            | Some fileName when not (isNull fileName) && (File.exists fileName) ->
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

        /// Get all parameters
        /// System ones are prefixed with 'system.', runner ones with 'runner.' and environment variables with 'env.'
        let getAll () = all.Value

        /// Get the value of a parameter by name
        /// System ones are prefixed with 'system.', runner ones with 'runner.' and environment variables with 'env.'
        let tryGet name = all.Value |> Map.tryFind name

    /// Get files changed between builds in TeamCity
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
            | Some file when File.exists file ->
                Some [
                    for line in File.read file do
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
        /// See [the documentation](https://confluence.jetbrains.com/display/TCD10/Risk+Tests+Reordering+in+Custom+Test+Runner) for more information
        let get () = fileChanges.Value

    let private getRecentlyFailedTests' () =
        match TeamCityBuildParameters.tryGetSystem "teamcity.tests.recentlyFailedTests.file" with
        | Some file when File.exists file -> Some(File.read file)
        | _ -> None

    let private recentlyFailedTests = lazy (getRecentlyFailedTests' ())

    /// Name of recently failing tests
    /// See [the documentation](https://confluence.jetbrains.com/display/TCD10/Risk+Tests+Reordering+in+Custom+Test+Runner) for more information
    let getTeamCityRecentlyFailedTests () = recentlyFailedTests.Value

    /// Get the branch of the main VCS root
    let getTeamCityBranch () = TeamCityBuildParameters.tryGetConfiguration "vcsroot.branch"

    /// Get the display name of the branch as shown in TeamCity
    /// See [the documentation](https://confluence.jetbrains.com/display/TCD10/Working+with+Feature+Branches#WorkingwithFeatureBranches-branchSpec) for more information
    let getTeamCityBranchName () =
        match TeamCityBuildParameters.tryGetConfiguration "teamcity.build.branch" with
        | Some _  as branch -> branch
        | None -> TeamCityBuildParameters.tryGetConfiguration "vcsroot.branch"

    /// Get if the current branch is the one configured as default
    let getTeamCityBranchIsDefault () =
        if BuildServer.buildServer = BuildServer.TeamCity then
            match TeamCityBuildParameters.tryGetConfiguration "teamcity.build.branch.is_default" with
            | Some "true" -> true
            | Some _ -> false
            | None ->
                // When only one branch is configured, TeamCity doesn't emit this parameter
                getTeamCityBranch().IsSome
        else
            false

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
        
