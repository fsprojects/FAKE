/// Contains options to run [Coverlet](https://github.com/tonerdo/coverlet) as part of dotnet test.
[<RequireQualifiedAccess>]
module Fake.DotNet.Testing.Coverlet

open Fake.DotNet

type OutputFormat =
    | Json
    | Lcov
    | OpenCover
    | Cobertura
    | TeamCity

type ThresholdType =
    | Line
    | Branch
    | Method

type ThresholdStat =
    | Minimum
    | Total
    | Average

/// Coverlet MSBuild parameters, for more details see: https://github.com/tonerdo/coverlet/blob/master/Documentation/MSBuildIntegration.md.
type CoverletParams =
    { /// (Required) Format of the generated output.
        OutputFormat : OutputFormat
        /// (Required) Path to the generated output file, or directory if it ends with a /.
        Output : string
        /// Namespaces to include, as (AssemblyName, Namespace) pairs. Supports `*` and `?` globbing.
        Include : (string * string) list
        /// Namespaces to exclude, as (AssemblyName, Namespace) pairs. Supports `*` and `?` globbing.
        Exclude : (string * string) list
        /// Exclude methods, types and assemblies annotated with these attributes.
        ExcludeByAttribute : string list
        /// Exclude these source files. Supports path globbing.
        ExcludeByFile : string list
        /// Coverlet json file to merge with the output of this run.
        MergeWith : string option
        /// Minimum coverage percent. Build fails if the result is below.
        Threshold : int option
        /// Type of coverage to check against the threshold.
        ThresholdType : ThresholdType
        /// Coverage statistic to check against the threshold.
        ThresholdStat : ThresholdStat
        /// Generate results with URL links from SourceLink instead of file paths.
        UseSourceLink : bool }

let Defaults =
    { OutputFormat = OutputFormat.Json
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

let private outputFormatToString = function
    | OutputFormat.Json -> "json"
    | OutputFormat.Lcov -> "lcov"
    | OutputFormat.OpenCover -> "opencover"
    | OutputFormat.Cobertura -> "cobertura"
    | OutputFormat.TeamCity -> "teamcity"

let private namespacesToString =
    Seq.map (fun (asm, ns) -> "[" + asm + "]" + ns)
    >> String.concat ","

let private thresholdTypeToString = function
    | ThresholdType.Line -> "line"
    | ThresholdType.Branch -> "branch"
    | ThresholdType.Method -> "method"

let private thresholdStatToString = function
    | ThresholdStat.Minimum -> "minimum"
    | ThresholdStat.Total -> "total"
    | ThresholdStat.Average -> "average"

let withMSBuildArguments (param: CoverletParams -> CoverletParams) (args: MSBuild.CliArguments) =
    let param = param Defaults
    let properties =
        [
            "CollectCoverage", "true"
            "OutputFormat", outputFormatToString param.OutputFormat
            "CoverletOutput", param.Output
            if not (List.isEmpty param.Include) then
                "Include", namespacesToString param.Include
            if not (List.isEmpty param.Exclude) then
                "Exclude", namespacesToString param.Exclude
            if not (List.isEmpty param.ExcludeByAttribute) then
                "ExcludeByAttribute", String.concat "," param.ExcludeByAttribute
            if not (List.isEmpty param.ExcludeByFile) then
                "ExcludeByFile", String.concat "," param.ExcludeByFile
            match param.MergeWith with
            | Some f -> "MergeWith", f
            | None -> ()
            match param.Threshold with
            | Some t ->
                "Threshold", string t
                "ThresholdType", thresholdTypeToString param.ThresholdType
                "ThresholdStat", thresholdStatToString param.ThresholdStat
            | None -> ()
            if param.UseSourceLink then
                "UseSourceLink", "true"
        ]
    { args with Properties = args.Properties @ properties }

let withDotNetTestOptions (param: CoverletParams -> CoverletParams) (options: DotNet.TestOptions) =
    { options with MSBuildParams = withMSBuildArguments param options.MSBuildParams }
