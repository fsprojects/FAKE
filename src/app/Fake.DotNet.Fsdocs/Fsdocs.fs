namespace Fake.DotNet

open Fake.Core

/// <summary>
/// Contains tasks to interact with <a href="https://fsprojects.github.io/FSharp.Formatting/">fsdocs</a> tool to
/// process F# script files, markdown and for generating API documentation.
/// </summary>
[<RequireQualifiedAccess>]
module Fsdocs =

    /// <summary>
    /// Fsdocs build command parameters and options
    /// </summary>
    type BuildCommandParams =
        {
            /// Input directory of content (default: docs)
            Input: string option

            /// Project files to build API docs for outputs, defaults to all packable projects
            Projects: seq<string> option

            /// Output Directory (default <c>output</c> for <c>build</c> and <c>tmp/watch</c> for <c>watch</c>)
            Output: string option

            /// Disable generation of API docs
            NoApiDocs: bool option

            /// Evaluate F# fragments in scripts
            Eval: bool option

            /// Save images referenced in docs
            SaveImages: bool option

            /// Add line numbers
            LineNumbers: bool option

            /// Additional substitution parameters for templates
            Parameters: seq<string * string> option

            /// Disable project cracking.
            IgnoreProjects: bool option

            ///  In API doc generation qualify the output by the collection name, e.g. 'reference/FSharp.Core/...' instead of 'reference/...' .
            Qualify: bool option

            /// The tool will also generate documentation for non-public members
            NoPublic: bool option

            /// Do not copy default content styles, javascript or use default templates
            NoDefaultContent: bool option

            /// Clean the output directory
            Clean: bool option

            /// Display version information
            Version: bool option

            /// Provide properties to dotnet msbuild, e.g. <c>--properties Configuration=Release Version=3.4</c>
            Properties: string option

            /// Additional arguments passed down as otherflags to the F# compiler when the API is being generated.
            /// Note that these arguments are trimmed, this is to overcome a limitation in the command line argument
            /// processing. A typical use-case would be to pass an addition assembly reference.
            /// Example <c>--fscoptions " -r:MyAssembly.dll"</c>
            FscOptions: string option

            /// Fail if docs are missing or can't be generated
            Strict: bool option

            /// Source folder at time of component build (<c>&lt;FsDocsSourceFolder&gt;</c>)
            SourceFolder: string option

            /// Source repository for github links (<c>&lt;FsDocsSourceRepository&gt;</c>)
            SourceRepository: string option

            /// Assume comments in F# code are markdown (<c>&lt;UsesMarkdownComments&gt;</c>)
            MdComments: bool option
        }

        /// Parameter default values.
        static member Default =
            { Input = None
              Projects = None
              Output = None
              NoApiDocs = None
              Eval = None
              SaveImages = None
              LineNumbers = None
              Parameters = None
              IgnoreProjects = None
              Qualify = None
              NoPublic = None
              NoDefaultContent = None
              Clean = None
              Version = None
              Properties = None
              FscOptions = None
              Strict = None
              SourceFolder = None
              SourceRepository = None
              MdComments = None }

    /// <summary>
    /// Fsdocs watch command parameters and options
    /// </summary>
    type WatchCommandParams =
        {
            /// Do not serve content when watching.
            NoServer: bool option

            /// Do not launch a browser window.
            NoLaunch: bool option

            /// URL extension to launch <c>http://localhost:/%s</c>.
            Open: string option

            /// Port to serve content for <c>http://localhost</c> serving.
            Port: int option

            /// Build Commands
            BuildCommandParams: BuildCommandParams option
        }

        /// Parameter default values.
        static member Default =
            { NoServer = None
              NoLaunch = None
              Open = None
              Port = None
              BuildCommandParams = None }

    let internal buildBuildCommandParams (buildParams: BuildCommandParams) =
        let buildSubstitutionParameters (subParameters: seq<string * string>) =
            let subParameters =
                subParameters
                |> Seq.map (fun (key, value) -> (sprintf "%s %s" key value))
                |> String.concat " "

            sprintf "--parameters %s" subParameters

        System.Text.StringBuilder()
        |> StringBuilder.appendIfSome buildParams.Input (sprintf "--input %s")
        |> StringBuilder.appendIfSome buildParams.Projects (fun projects ->
            sprintf "--projects %s" (projects |> String.concat " "))
        |> StringBuilder.appendIfSome buildParams.Output (sprintf "--output %s")
        |> StringBuilder.appendIfSome buildParams.NoApiDocs (fun _ -> "--noapidocs")
        |> StringBuilder.appendIfSome buildParams.Eval (fun _ -> "--eval")
        |> StringBuilder.appendIfSome buildParams.SaveImages (fun _ -> "--saveimages")
        |> StringBuilder.appendIfSome buildParams.LineNumbers (fun _ -> "--linenumbers")
        |> StringBuilder.appendIfSome buildParams.Parameters (fun parameters -> buildSubstitutionParameters parameters)
        |> StringBuilder.appendIfSome buildParams.IgnoreProjects (fun _ -> "--ignoreprojects")
        |> StringBuilder.appendIfSome buildParams.Qualify (fun _ -> "--qualify")
        |> StringBuilder.appendIfSome buildParams.NoPublic (fun _ -> "--nonpublic")
        |> StringBuilder.appendIfSome buildParams.NoDefaultContent (fun _ -> "--nodefaultcontent")
        |> StringBuilder.appendIfSome buildParams.Clean (fun _ -> "--clean")
        |> StringBuilder.appendIfSome buildParams.Version (fun _ -> "--version")
        |> StringBuilder.appendIfSome buildParams.Properties (sprintf "--properties %s")
        |> StringBuilder.appendIfSome buildParams.FscOptions (sprintf "--fscoptions %s")
        |> StringBuilder.appendIfSome buildParams.Strict (fun _ -> "--strict")
        |> StringBuilder.appendIfSome buildParams.SourceFolder (sprintf "--sourcefolder %s")
        |> StringBuilder.appendIfSome buildParams.SourceRepository (sprintf "--sourcerepo %s")
        |> StringBuilder.appendIfSome buildParams.MdComments (fun _ -> "--mdcomments")
        |> StringBuilder.toText
        |> String.trim

    let internal buildWatchCommandParams (watchParams: WatchCommandParams) =
        System.Text.StringBuilder()
        |> StringBuilder.appendIfSome watchParams.NoServer (fun _ -> "--noserver")
        |> StringBuilder.appendIfSome watchParams.NoLaunch (fun _ -> "--nolaunch")
        |> StringBuilder.appendIfSome watchParams.Open (sprintf "--open %s")
        |> StringBuilder.appendIfSome watchParams.Port (sprintf "--port %i")
        |> StringBuilder.appendIfSome watchParams.BuildCommandParams buildBuildCommandParams
        |> StringBuilder.toText
        |> String.trim

    /// <summary>
    /// Build documentation using <c>fsdocs build</c> command
    /// </summary>
    ///
    /// <param name="setBuildParams">Function used to overwrite the build command default parameters.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Fsdocs.build (fun p -> { p with Clean = Some(true); Strict = Some(true) })
    /// </code>
    /// </example>
    let build setBuildParams =
        let buildParams = setBuildParams BuildCommandParams.Default
        let formattedParameters = buildBuildCommandParams buildParams

        let dotnetOptions = (fun (buildOptions: DotNet.Options) -> buildOptions)
        let result = DotNet.exec dotnetOptions "fsdocs build" formattedParameters

        if 0 <> result.ExitCode then
            failwithf "fsdocs build failed with exit code '%d'" result.ExitCode

    /// <summary>
    /// Watch documentation using <c>fsdocs watch</c> command
    /// </summary>
    ///
    /// <param name="setWatchParams">Function used to overwrite the watch command default parameters.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Fsdocs.watch (fun p -> { p with Port = Some(3005) })
    /// </code>
    /// </example>
    let watch setWatchParams =
        let watchParams = setWatchParams WatchCommandParams.Default
        let formattedParameters = buildWatchCommandParams watchParams

        let dotnetOptions = (fun (buildOptions: DotNet.Options) -> buildOptions)
        let result = DotNet.exec dotnetOptions "fsdocs watch" formattedParameters

        if 0 <> result.ExitCode then
            failwithf "fsdocs watch failed with exit code '%d'" result.ExitCode
