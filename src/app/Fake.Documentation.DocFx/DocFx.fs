/// Contains helper functions to use DocFx https://dotnet.github.io/docfx/.

[<RequireQualifiedAccess>]
module Fake.Documentation.DocFx

open System
open System.IO
open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators


let private docFxPath = 
    let docFxExe = "docfx.exe"
    let toolPath = Tools.findToolInSubPath docFxExe (Directory.GetCurrentDirectory() @@ "tools" @@ "docfx.console" @@ "tools")
    if File.exists toolPath then toolPath
    else 
        match Process.tryFindFileOnPath docFxExe with
        | Some docFx when File.Exists docFx -> docFx
        | _ -> toolPath

type CommonParams =
    {
        /// The tool path - FAKE tries to find docfx.exe automatically in any sub folder.
        DocFxPath : string

        /// Specify a timeout for DocFx. Default: 5 min.
        Timeout : TimeSpan

        /// Specify the process working directory
        WorkingDirectory : string
    }
    /// Common default parameters
    static member Create() =
        {
            DocFxPath = docFxPath
            WorkingDirectory = ""
            Timeout = TimeSpan.FromMinutes 5.
        }
       
let exec setParams command args= 
    let commandArgs = sprintf "%s %s" command args
    use __ = Trace.traceTask "DocFx" commandArgs
    let p = CommonParams.Create() |> setParams
    if 0 <> Process.execSimple ((fun info ->
            { info with
                FileName = p.DocFxPath
                WorkingDirectory = p.WorkingDirectory
                Arguments = commandArgs })) p.Timeout
    then failwithf "DocFx command %s failed." commandArgs
    __.MarkSuccess()

/// Init-Command parameters
type InitParams = 
    {
        /// Specify common docFx options
        Common : CommonParams

        /// Specify if the current file will be overwritten if it exists.
        Overwrite : bool

        /// Specify the output folder of the config file. Defaults to docfx_project.
        OutputFolder : string

        /// Generate config file docfx.json only, no project folder will be generated.
        OnlyConfigFile : bool

        /// Specify the source project files' glob pattern to generate metadata.
        ApiSourceGlobPattern : string

        /// Specify the source working folder for source project files to start glob search.
        ApiSourceFolder : string
    }
    /// Changes the "Common" parameters according to the given function
    member inline x.WithCommon f =
        {x with Common = f x.Common}

    /// Init default parameters
    static member Create() = 
        {
            Common = CommonParams.Create()
            Overwrite = false
            OutputFolder = ""
            OnlyConfigFile = false
            ApiSourceGlobPattern = ""
            ApiSourceFolder = ""
        }

let private stringify value = 
    sprintf "%A" value

let private seperated xs = xs |> String.separated ","
let private stringifyList (paramValue) = 
    match paramValue with
    | [] -> ""
    | xs -> seperated paramValue

let private stringifyOption option =
    match option with
    | Some value -> stringify value
    | None -> ""

let private stringifyParams parameters = 
    let stringify (name, value) = sprintf "%s=%s" name value

    let args = 
        parameters 
        |> List.map stringify 
        |> Arguments.OfArgs

    args.ToWindowsCommandLine

let private serializeInitParams p = 
    let parameters = [  
        ("quiet", "true")
        ("overwrite", stringify p.Overwrite)
        ("output", p.OutputFolder)
        ("file", stringify p.OnlyConfigFile)
        ("apiGlobPattern", p.ApiSourceGlobPattern)
        ("apiSourceFolder", p.ApiSourceFolder) 
    ]

    stringifyParams parameters
    

/// Initialize a DocFx documentation.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default Init parameters. See `InitParams.Create()`
/// ## Sample
///
///         DocFx.init (fun p -> 
///          { p with 
///              Overwrite = true
///              Timeout = TimeSpan.FromMinutes 10.
///          })

let init setParams =
    let p = InitParams.Create() |> setParams
    p
    |> serializeInitParams
    |> exec (fun _ -> p.Common) "init" 

type LogLevel = | Diagnostic
                | Verbose
                | Info
                | Warning
                | Error
/// Parameters for logging
type LogParams = 
    {
        /// Specify the file name to save processing log.
        LogFilePath : string

        /// Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.
        LogLevel : LogLevel option

        /// Specify the GIT repository root folder.
        RepoRoot : string

        /// Specify the correlation id used for logging.
        CorrelationId : string
    }
    /// Log default parameters
    static member Create() =
        {
            LogFilePath = ""
            LogLevel = None
            RepoRoot = ""
            CorrelationId = ""
        }

let private parseLogLevel = 
    Option.map (fun o ->
                match o with
                | Diagnostic -> "Diagnostic"
                | Verbose -> "Verbose"
                | Info -> "Info"
                | Warning -> "Warning"
                | Error -> "Error")

let private parseLogParams p = 
    [ ("log", p.LogFilePath)
      ("logLevel", sprintf "%A" (parseLogLevel p.LogLevel))
      ("repositoryRoot", p.RepoRoot)
      ("correlationId", p.CorrelationId) ]

/// Build-Command parameters
type BuildParams = 
    {
        /// Specify common docFx options
        Common : CommonParams

        /// Specify the output base directory.
        OutputFolder : string

        /// Specify the config file path.
        ConfigFile : string

        /// Specify content files for generating documentation.
        Content : string list

        /// Specify resources used by content files.
        Resource : string list 

        /// Specify overwrite files used by content files.
        Overwrite : string list

        /// Specify the urls of xrefmap used by content files.
        XRefMaps : string list

        /// Specify the template name to apply to. If not specified, output YAML file will not be transformed.
        Templates : string list

        /// Specify which theme to use. By default 'default' theme is offered.
        Themes : string list

        /// Host the generated documentation to a website.
        Serve : bool

        /// Specify the hostname of the hosted website (e.g., 'localhost' or '*').
        Host : string

        /// Specify the port of the hosted website.
        Port : int option

        /// Force re-build all the documentation.
        ForceRebuild : bool

        /// Run in debug mode. With debug mode, raw model and view model will be exported automatically when it encounters error when applying templates. If not specified, it is false.
        EnableDebugMode : bool

        /// The output folder for files generated for debugging purpose when in debug mode. If not specified, it is ${TempPath}/docfx.
        OutputFolderForDebugFiles : string

        /// Force to re-process the documentation in post processors. It will be cascaded from force option.
        ForcePostProcess : bool

        /// Specify global metadata key-value pair in json format. It overrides the globalMetadata settings from the config file.
        GlobalMetadata : string
            
        /// Specify a list of JSON file path containing globalMetadata settings, as similar to {\"key\":\"value\"}. It overrides the globalMetadata settings from the config file.
        GlobalMetadataFilePaths : string list
            
        /// Specify a list of JSON file path containing fileMetadata settings, as similar to {\"key\":\"value\"}. It overrides the fileMetadata settings from the config file.
        FileMetadataFilePaths : string list

        /// If set to true, data model to run template script will be extracted in .raw.model.json extension.
        ExportRawModel : bool

        /// Specify the output folder for the raw model. If not set, the raw model will be generated to the same folder as the output documentation.
        RawModelOutputFolder : string

        /// Specify the output folder for the view model. If not set, the view model will be generated to the same folder as the output documentation.
        ViewModelOutputFolder : string

        /// If set to true, data model to apply template will be extracted in .view.model.json extension.
        ExportViewModel : bool

        /// If set to true, template will not be actually applied to the documents. This option is always used with --exportRawModel or --exportViewModel is set so that only raw model files or view model files are generated.
        DryRun : bool

        /// Set the max parallelism, 0 is auto.
        MaxParallelism : int option

        /// Set the name of markdown engine, default is 'dfm'.
        MarkdownEngineName : string

        /// Set the parameters for markdown engine, value should be a JSON string.
        MarkdownEngineProperties : string

        /// Disable default lang keyword.
        NoLangKeyword : bool option

        /// Set folder for intermediate build results.
        IntermediateFolder : string

        /// Set changes file.
        ChangesFile : string

        /// Set the order of post processors in plugins.
        PostProcessors : string list

        /// Set the LRU cached model count (approximately the same as the count of input files). By default, it is 8192 for 64bit and 3072 for 32bit process. With LRU cache enabled, memory usage decreases and time consumed increases. If set to 0, Lru cache is disabled.
        LruSize : int option

        /// If set to true, docfx does not dereference (aka. copy) file to the output folder, instead, it saves a link_to_path property inside mainfiest.json to indicate the physical location of that file.
        KeepFileLink : bool

        /// If set to true, docfx create a new intermediate folder for cache files, historical cache data will be cleaned up.
        CleanupCacheHistory : bool

        /// Set the name of input file abstract layer builder.
        FALName : string

        /// Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.
        DisableGitFeatures : bool

        /// Please provide the license key for validating schema using NewtonsoftJson.Schema here.
        SchemaLicense : string

        /// Specify Logging parameter
        LogParams : LogParams
    }

    /// Build default parameters
    static member Create() =
        {   
            Common = CommonParams.Create()
            OutputFolder = ""
            ConfigFile = ""
            Content = []
            Resource = []
            Overwrite = []
            XRefMaps = []
            Templates = []
            Themes = []
            Serve = false
            Host = ""
            Port = None
            ForceRebuild = false
            EnableDebugMode = false
            OutputFolderForDebugFiles = ""
            ForcePostProcess = false
            GlobalMetadata = ""
            GlobalMetadataFilePaths = []
            FileMetadataFilePaths = []
            ExportRawModel = false
            RawModelOutputFolder = ""
            ViewModelOutputFolder = ""
            ExportViewModel = false
            DryRun = false
            MaxParallelism = None
            MarkdownEngineName = ""
            MarkdownEngineProperties = ""
            NoLangKeyword = None
            IntermediateFolder = ""
            ChangesFile = ""
            PostProcessors = []
            LruSize = None
            KeepFileLink = false
            CleanupCacheHistory = false
            FALName = ""
            DisableGitFeatures = false
            SchemaLicense = ""
            LogParams = LogParams.Create()
        }
    /// Changes the "LogParams" according to the given function
    member inline x.WithLogParams f =
        { x with LogParams = f x.LogParams }

    /// Changes the "Common" parameters according to the given function
    member inline x.WithCommon f =
        {x with Common = f x.Common}


let private parseBuildParams p =
    let buildParams = [   
        ("output", p.OutputFolder)
        ("content", stringifyList p.Content)
        ("resource", stringifyList p.Resource)
        ("overwrite", stringifyList p.Overwrite)
        ("xref", stringifyList p.XRefMaps)
        ("template", stringifyList p.Templates)
        ("theme", stringifyList p.Themes)
        ("serve", stringify p.Serve)
        ("hostname", p.Host)
        ("port", stringifyOption p.Port)
        ("force", stringify p.ForceRebuild)
        ("debug", stringify p.EnableDebugMode)
        ("debugOutput", p.OutputFolderForDebugFiles)
        ("forcePostProcess", stringify p.ForcePostProcess)
        ("globalMetadata", p.GlobalMetadata)
        ("globalMetadataFiles", stringifyList p.GlobalMetadataFilePaths)
        ("fileMetadataFiles", stringifyList p.FileMetadataFilePaths)
        ("exportRawModel", stringify p.ExportRawModel)
        ("rawModelOutputFolder", p.RawModelOutputFolder)
        ("viewModelOutputFolder", p.ViewModelOutputFolder)
        ("exportViewModel", stringify p.ExportViewModel)
        ("dryRun", stringify p.DryRun)
        ("maxParallelism", stringifyOption p.MaxParallelism)
        ("markdownEngineName", p.MarkdownEngineName)
        ("markdownEngineProperties", p.MarkdownEngineProperties)
        ("noLangKeyword", stringifyOption p.NoLangKeyword)
        ("intermediateFolder", p.IntermediateFolder)
        ("changeFile", p.ChangesFile)
        ("postProcessors", stringifyList p.PostProcessors)
        ("lruSize", stringifyOption p.LruSize)
        ("keepFileLink", stringify p.KeepFileLink)
        ("cleanupCacheHistory", stringify p.CleanupCacheHistory)
        ("falName", p.FALName)
        ("disableGitFeatures", stringify p.DisableGitFeatures)
        ("schemaLicense", p.SchemaLicense)
    ]
    List.append buildParams (parseLogParams p.LogParams)

let private serializeBuildParams p =
    p |> parseBuildParams |> stringifyParams |> sprintf "%s %s" p.ConfigFile


/// Builds a DocFx documentation.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default build parameters. See `BuildParams.Create()`
/// ## Sample
///
///        DocFx.build (fun p -> 
///         { p with 
///             OutputFolder = "build" @@ "docs"
///             ConfigFile = "docs" @@ "docfx.json"    
///         })

let build setParams =
    let p = BuildParams.Create() |> setParams
    p
    |> serializeBuildParams
    |> exec (fun _ -> p.Common) "build" 

/// Pdf-Command parameters
type PdfParams = 
    {
        /// Specify build parameters.
        BuildParams : BuildParams
        /// Specify the name of the generated pdf.
        Name : string

        /// Specify the path for the css to generate pdf, default value is styles/default.css.
        CssFilePath : string

        /// Specify whether or not to generate appendices for not-in-TOC articles.
        GeneratesAppendices : bool option

        /// generatesExternalLink", HelpText = "Specify whether or not to generate external links for PDF.
        GeneratesExternalLink : bool option

        /// Specify whether or not to keep the intermediate html files that used to generate the PDF file. It it usually used in debug purpose. By default the value is false.
        KeepRawFiles : bool option

        /// Specify how to handle pdf pages that fail to load: abort, ignore or skip(default abort), it is the same input as wkhtmltopdf --load-error-handling options.
        LoadErrorHandling : string

        /// Specify the output folder for the raw files, if not specified, raw files will by default be saved to _raw subfolder under output folder if keepRawFiles is set to true.
        RawOutputFolder : string

        /// Specify the hostname to link not-in-TOC articles.
        Host : string

        /// Specify the locale of the pdf file.
        Locale : string

        /// Specify the toc files to be excluded.
        ExcludedTocs : string list

        /// Specify the base path to generate external link, {host}/{locale}/{basePath}.
        BasePath : string
 
    }
    /// Pdf default parameters
    static member Create() =
        {   
            BuildParams = BuildParams.Create()
            Name = ""
            CssFilePath = ""
            GeneratesAppendices = None
            GeneratesExternalLink = None
            KeepRawFiles = None
            LoadErrorHandling = ""
            RawOutputFolder = ""
            Host = ""
            Locale = ""
            ExcludedTocs = []
            BasePath = ""
        }

    /// Changes the "BuildParams" according to the given function
    member inline x.WithBuildParams f =
        { x with BuildParams = f x.BuildParams }
    /// Changes the "LogParams" according to the given function
    member inline x.WithLogParams f =
        x.WithBuildParams (fun b -> b.WithLogParams f)
    /// Changes the "Common" parameters according to the given function
    member inline x.WithCommon f =
        x.WithBuildParams (fun b -> b.WithCommon f)
let private parsePdfParams p = 
    let pdfParams = [
        ("name", p.Name)
        ("css", p.CssFilePath)
        ("generatesAppendices", stringifyOption p.GeneratesAppendices)
        ("generatesExternalLink", stringifyOption p.GeneratesExternalLink)
        ("keepRawFiles", stringifyOption p.KeepRawFiles)
        ("errorHandling", p.LoadErrorHandling)
        ("rawOutputFolder", p.RawOutputFolder)
        ("host", p.Host)
        ("locale", p.Locale)
        ("excludedTocs", stringifyList p.ExcludedTocs)
        ("basePath", p.BasePath)
    ]
    List.append pdfParams (parseBuildParams p.BuildParams)

let private serializePdfParams p =
    p |> parsePdfParams |> stringifyParams |> sprintf "%s %s" p.BuildParams.ConfigFile

/// Builds a Pdf-File from a DocFx documentation.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default pdf parameters. See `PdfParams.Create()`
/// ## Sample
///
///        DocFx.pdf (fun p ->
///            { p with
///                    Name = "Docs.pdf" }
///              .WithBuildParams (fun b -> 
///                   { b with 
///                       OutputFolder = "build" @@ "docs"
///                       ConfigFile = "docs" @@ "docfx.json"})
///                    )   
let pdf setParams =
    let p = PdfParams.Create() |> setParams
    p
    |> serializePdfParams
    |> exec (fun _ -> p.BuildParams.Common) "pdf" 


           
/// ExportTemplate-Command parameters
type ExportTemplateParams =
    {
    
        /// Specify common docFx options
        Common : CommonParams

        /// Specify templates to export.
        Templates : string list

        /// If specified, all the available templates will be exported.
        All : bool

        /// Specify the output folder path for the exported templates.
        OutputFolder : string
    }
    /// ExportTemplate default parameters
    static member Create()= 
        {
            Common = CommonParams.Create()
            Templates = []
            All = false
            OutputFolder = ""
        }
    /// Changes the "Common" parameters according to the given function
    member inline x.WithCommon f =
        {x with Common = f x.Common}



let private serializeExportTemplateParams p =
    [
        ("all", stringify p.All)
        ("output", p.OutputFolder)
    ] 
    |> stringifyParams
    |> sprintf "export %s %s" (p.Templates |> seperated)

/// Exports template files.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default exportTemplate parameters. See `ExportTemplateParams.Create()`
/// ## Sample
///
///         DocFx.exportTemplate (fun p -> 
///             { p with 
///                     All = true
///                     OutputFolder = "templates"
///             })
let exportTemplate setParams =
    let p = ExportTemplateParams.Create() |> setParams
    p
    |> serializeExportTemplateParams
    |> exec (fun _ -> p.Common) "template"

/// Download-Command parameters
type DownloadParams = 
    {
        /// Specify common docFx options
        Common : CommonParams
        /// Specify the output xref archive.
        ArchiveFile : string

        /// Specify the url of xrefmap.
        Uri : string
    }
    /// Download default parameters
    static member Create()= 
        {
            Common = CommonParams.Create()
            ArchiveFile = ""
            Uri = ""
        }
    /// Changes the "Common" parameters according to the given function
    member inline x.WithCommon f =
        {x with Common = f x.Common}

let private serializeDownloadParams p =
    [
        ("xref", p.Uri)
    ] 
    |> stringifyParams
    |> sprintf "%s %s" p.ArchiveFile

/// Download xref archive.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default download parameters. See `DownloadParams.Create()`
/// ## Sample
///
///         DocFx.download (fun p -> 
///             { p with 
///                     ArchiveFile = "archive"
///                     Uri = "uri"
///             })
let download setParams =
    let p = DownloadParams.Create() |> setParams
    p
    |> serializeDownloadParams
    |> exec (fun _ -> p.Common) "download"

/// Serve-Command parameters
type ServeParams = 
    {
        /// Specify common docFx options
        Common : CommonParams

        /// folder path
        Folder : string

        /// Specify the hostname of the hosted website [localhost].
        Host : string

        /// Specify the port of the hosted website [8080]
        Port : int option

    }
    /// Serve default parameters
    static member Create()= 
        {
            Common = CommonParams.Create()
            Folder = ""
            Host = ""
            Port = None
        }
    /// Changes the "Common" parameters according to the given function
    member inline x.WithCommon f =
        {x with Common = f x.Common}

let private serializeServeParams p =
    [
        ("hostname", p.Host)
        ("port", stringifyOption p.Port)
    ] 
    |> stringifyParams
    |> sprintf "%s %s" p.Folder

/// Serves a DocFx documentation.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default serve parameters. See `ServeParams.Create()`
/// ## Sample
///
///         DocFx.serve (fun p -> 
///             { p with 
///                     Host = "localhost"
///                     Port = Some 80
///                     Folder = "docs"
///             })

let serve setParams =
    let p = ServeParams.Create() |> setParams
    p
    |> serializeServeParams
    |> exec (fun _ -> p.Common) "serve"

/// Metadata-Command parameters
type MetadataParams = 
    {
        /// Specify common docFx options
        Common : CommonParams

        /// Force re-generate all the metadata.
        ForceRebuild : bool

        /// Skip to markup the triple slash comments.
        ShouldSkipMarkup : bool

        /// Specify the output base directory.
        OutputFolder : string

        /// Preserve the existing xml comment tags inside 'summary' triple slash comments.
        PreserveRawInlineComments : bool

        /// Specify project files.
        Projects : string list

        /// Specify the config file path.
        ConfigFile : string

        /// Specify the filter config file.
        FilterConfigFile : string

        /// Specify the name to use for the global namespace.
        GlobalNamespaceId : string

        /// --property <n1>=<v1>;<n2>=<v2> An optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to msbuild via the /property:<n1>=<v1>;<n2>=<v2> command line argument.
        MSBuildProperties : string

        /// Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.
        DisableGitFeatures : bool

        /// Specify Logging parameter
        LogParams : LogParams

    } 
    /// Metadata default parameters
    static member Create()= 
            {
                Common = CommonParams.Create()
                ForceRebuild = false
                ShouldSkipMarkup = false
                OutputFolder = ""
                PreserveRawInlineComments = false
                Projects = []
                ConfigFile = ""
                FilterConfigFile = ""
                GlobalNamespaceId = ""
                MSBuildProperties = ""
                DisableGitFeatures = false
                LogParams = LogParams.Create()
            }
    /// Changes the "LogParams" according to the given function
    member inline x.WithLogParams f =
        { x with LogParams = f x.LogParams }

    /// Changes the "Common" parameters according to the given function
    member inline x.WithCommon f =
        {x with Common = f x.Common}


let private serializeMetadataParams p =
    let metadata = [
        ("force", stringify p.ForceRebuild)
        ("shouldSkipMarkup", stringify p.ShouldSkipMarkup)
        ("output" , p.OutputFolder)
        ("raw", stringify p.PreserveRawInlineComments)
        ("filter", p.FilterConfigFile)
        ("globalNamespaceId", p.GlobalNamespaceId)
        ("property", p.MSBuildProperties)
        ("disableGitFeatures", stringify p.DisableGitFeatures)
    ] 
    List.append metadata (parseLogParams p.LogParams)
    |> stringifyParams
    |> sprintf "%s %s %s" p.ConfigFile (seperated p.Projects)

/// Serves a DocFx documentation.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default serve parameters. See `MetadataParams.Create()`
/// ## Sample
///
///         DocFx.metadata (fun p -> 
///             { p with 
///                     ConfigFile = "docs" @@ "docfx.json"
///                     DisableGitFeatures = true
///             })

let metadata setParams =
    let p = MetadataParams.Create() |> setParams
    p
    |> serializeMetadataParams
    |> exec (fun _ -> p.Common) "metadata"
