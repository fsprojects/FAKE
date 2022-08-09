namespace Fake.DotNet

open Fake.Core

/// Contains tasks to interact with [fsdocs](https://fsprojects.github.io/FSharp.Formatting/) tool to
/// process F# script files, markdown and for generating API documentation.
[<RequireQualifiedAccess>]
module Fsdocs =

    /// Fsdocs build command parameters and options
    type BuildCommandParams =
        {
            /// Input directory of content (default: docs)
            Input: string option

            /// Project files to build API docs for outputs, defaults to all packable projects
            Projects: seq<string> option

            /// Output Directory (default 'output' for 'build' and 'tmp/watch' for 'watch')
            Output: string option

            /// Disable generation of API docs
            NoApiDocs: bool option

            /// Evaluate F# fragments in scripts
            Eval: bool option

            /// Save images referenced in docs
            SaveImages: bool option

            /// Don't add line numbers, default is to add line number.
            NoLineNumbers: bool option

            /// Additional substitution parameters for templates
            Parameters: seq<string * string> option

            /// The tool will also generate documentation for non-public members
            NoPublic: bool option

            /// Do not copy default content styles, javascript or use default templates
            NoDefaultContent: bool option

            /// Clean the output directory
            Clean: bool option

            /// Display version information
            Version: bool option

            /// Provide properties to dotnet msbuild, e.g. --properties Configuration=Release Version=3.4
            Properties: string option

            /// Additional arguments passed down as otherflags to the F# compiler when the API is being generated.
            /// Note that these arguments are trimmed, this is to overcome a limitation in the command line argument processing.
            /// A typical use-case would be to pass an addition assembly reference.
            /// Example --fscoptions " -r:MyAssembly.dll"
            FscOptions: string option

            /// Fail if docs are missing or can't be generated
            Strict: bool option

            /// Source folder at time of component build (<FsDocsSourceFolder>)
            SourceFolder: string option

            /// Source repository for github links (<FsDocsSourceRepository>)
            SourceRepository: string option

            /// Assume comments in F# code are markdown (<UsesMarkdownComments>)
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
              NoLineNumbers = None
              Parameters = None
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

    /// Fsdocs watch command parameters and options
    type WatchCommandParams =
        {
            /// Do not serve content when watching.
            NoServer: bool option

            /// Do not launch a browser window.
            NoLaunch: bool option

            /// URL extension to launch http://localhost:/%s.
            Open: string option

            /// Port to serve content for http://localhost serving.
            Port: int option
        }

        /// Parameter default values.
        static member Default =
            { NoServer = None
              NoLaunch = None
              Open = None
              Port = None }

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
            sprintf "--projects %s" (projects |> String.concat ","))
        |> StringBuilder.appendIfSome buildParams.Output (sprintf "--output %s")
        |> StringBuilder.appendIfSome buildParams.NoApiDocs (fun _ -> "--noapidocs")
        |> StringBuilder.appendIfSome buildParams.Eval (fun _ -> "--eval")
        |> StringBuilder.appendIfSome buildParams.SaveImages (fun _ -> "--saveimages")
        |> StringBuilder.appendIfSome buildParams.NoLineNumbers (fun _ -> "--nolinenumbers")
        |> StringBuilder.appendIfSome buildParams.Parameters (fun parameters -> buildSubstitutionParameters parameters)
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
        |> StringBuilder.toText
        |> String.trim

    /// Build documentation using `fsdocs build` command
    ///
    /// ## Parameters
    ///  - `setBuildParams` - Function used to overwrite the build command default parameters.
    ///
    /// ## Sample
    ///   Fsdocs.build (fun p -> { p with Clean = Some(true); Strict = Some(true) })
    let build setBuildParams =
        let buildParams = setBuildParams BuildCommandParams.Default
        let formattedParameters = buildBuildCommandParams buildParams
        
        let dotnetOptions = (fun (buildOptions:DotNet.Options) -> buildOptions);
        let result = DotNet.exec dotnetOptions "fsdocs build" formattedParameters
        
        if 0 <> result.ExitCode
        then failwithf "fsdocs build failed with exit code '%d'" result.ExitCode
    
    /// Watch documentation using `fsdocs watch` command
    ///
    /// ## Parameters
    ///  - `setWatchParams` - Function used to overwrite the watch command default parameters.
    ///
    /// ## Sample
    ///   Fsdocs.watch (fun p -> { p with Port = Some(3005) })
    let watch setWatchParams =
        let watchParams = setWatchParams WatchCommandParams.Default
        let formattedParameters = buildBuildCommandParams watchParams
        
        let dotnetOptions = (fun (buildOptions:DotNet.Options) -> buildOptions);
        let result = DotNet.exec dotnetOptions "fsdocs watch" formattedParameters
        
        if 0 <> result.ExitCode
        then failwithf "fsdocs watch failed with exit code '%d'" result.ExitCode 
