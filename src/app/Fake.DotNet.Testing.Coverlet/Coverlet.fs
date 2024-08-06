namespace Fake.DotNet.Testing

/// <namespacedoc>
/// <summary>
/// DotNet.Testing namespace contains tasks to interact with a test tools and runners for DotNet applications,
/// like NUnit, Expecto and Coverlet
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains options to run <a href="https://github.com/tonerdo/coverlet">Coverlet</a> as part of dotnet test.
/// </summary>
[<RequireQualifiedAccess>]
module Coverlet =

    open Fake.DotNet

    /// The coverage report file format.
    type OutputFormat =
        | Json
        | Lcov
        | OpenCover
        | Cobertura
        | TeamCity

    /// The type of coverage to use when failing under a threshold.
    type ThresholdType =
        | Line
        | Branch
        | Method

    /// The statistic to use when failing under a threshold.
    type ThresholdStat =
        | Minimum
        | Total
        | Average

    /// <summary>
    /// Coverlet MSBuild parameters. For more details
    /// see: <a href="https://github.com/tonerdo/coverlet/blob/master/Documentation/MSBuildIntegration.md">
    /// documentation</a>
    /// </summary>
    type CoverletParams =
        {
            /// (Required) Format of the generated output.
            OutputFormat: OutputFormat list
            /// (Required) Path to the generated output file, or directory if it ends with a <c>/</c>.
            Output: string
            /// Namespaces to include, as (AssemblyName, Namespace) pairs. Supports <c>*</c> and <c>?</c> globbing.
            Include: (string * string) list
            /// Namespaces to exclude, as (AssemblyName, Namespace) pairs. Supports <c>*</c> and <c>?</c> globbing.
            Exclude: (string * string) list
            /// Exclude methods, types and assemblies annotated with these attributes.
            ExcludeByAttribute: string list
            /// Exclude these source files. Supports path globbing.
            ExcludeByFile: string list
            /// Coverlet json file to merge with the output of this run.
            MergeWith: string option
            /// Minimum coverage percent. Build fails if the result is below.
            Threshold: int option
            /// Type of coverage to check against the threshold.
            ThresholdType: ThresholdType
            /// Coverage statistic to check against the threshold.
            ThresholdStat: ThresholdStat
            /// Generate results with URL links from SourceLink instead of file paths.
            UseSourceLink: bool
        }

    /// The default parameters.
    let private defaults =
        { OutputFormat = [ OutputFormat.Json ]
          Output = "./"
          Include = []
          Exclude = []
          ExcludeByAttribute = []
          ExcludeByFile = []
          MergeWith = None
          Threshold = None
          ThresholdType = ThresholdType.Line
          ThresholdStat = ThresholdStat.Minimum
          UseSourceLink = false }

    let private outputFormatToString =
        function
        | OutputFormat.Json -> "json"
        | OutputFormat.Lcov -> "lcov"
        | OutputFormat.OpenCover -> "opencover"
        | OutputFormat.Cobertura -> "cobertura"
        | OutputFormat.TeamCity -> "teamcity"

    let private outputFormatListToString =
        List.map outputFormatToString >> String.concat ","

    let private namespacesToString =
        Seq.map (fun (asm, ns) -> "[" + asm + "]" + ns) >> String.concat ","

    let private thresholdTypeToString =
        function
        | ThresholdType.Line -> "line"
        | ThresholdType.Branch -> "branch"
        | ThresholdType.Method -> "method"

    let private thresholdStatToString =
        function
        | ThresholdStat.Minimum -> "minimum"
        | ThresholdStat.Total -> "total"
        | ThresholdStat.Average -> "average"

    /// <summary>
    /// Add Coverlet parameters to the MSBuild command.
    /// </summary>
    ///
    /// <param name="param">The coverlet parameters</param>
    /// <param name="args">MSBuild arguments</param>
    let withMSBuildArguments (param: CoverletParams -> CoverletParams) (args: MSBuild.CliArguments) =
        let param = param defaults

        let properties =
            [ yield "CollectCoverage", "true"
              yield "CoverletOutputFormat", outputFormatListToString param.OutputFormat
              yield "CoverletOutput", param.Output
              if not (List.isEmpty param.Include) then
                  yield "Include", namespacesToString param.Include
              if not (List.isEmpty param.Exclude) then
                  yield "Exclude", namespacesToString param.Exclude
              if not (List.isEmpty param.ExcludeByAttribute) then
                  yield "ExcludeByAttribute", String.concat "," param.ExcludeByAttribute
              if not (List.isEmpty param.ExcludeByFile) then
                  yield "ExcludeByFile", String.concat "," param.ExcludeByFile
              match param.MergeWith with
              | Some f -> yield "MergeWith", f
              | None -> ()
              match param.Threshold with
              | Some t ->
                  yield "Threshold", string t
                  yield "ThresholdType", thresholdTypeToString param.ThresholdType
                  yield "ThresholdStat", thresholdStatToString param.ThresholdStat
              | None -> ()
              if param.UseSourceLink then
                  yield "UseSourceLink", "true" ]

        { args with Properties = args.Properties @ properties }

    /// <summary>
    /// Add Coverlet parameters to the dotnet test command.
    /// </summary>
    ///
    /// <param name="param">The coverlet parameters</param>
    /// <param name="options">DotNet test cli command options</param>
    let withDotNetTestOptions (param: CoverletParams -> CoverletParams) (options: DotNet.TestOptions) =
        { options with
            MSBuildParams = withMSBuildArguments param options.MSBuildParams }
