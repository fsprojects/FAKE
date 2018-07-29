[<RequireQualifiedAccess>]
module Fake.Tools.GitVersion

open Newtonsoft.Json
open System
open System.IO
open Fake.IO.Globbing
open Fake.Core

type GitversionParams = {
    ToolPath : string
}

let private gitversionDefaults = {
    ToolPath = Tools.findToolInSubPath "GitVersion.exe" (Environment.environVarOrDefault "ChocolateyInstall" (Directory.GetCurrentDirectory()))
}

type GitVersionProperties = {
    Major : int
    Minor : int
    Patch : int
    PreReleaseTag : string
    PreReleaseTagWithDash : string
    PreReleaseLabel : string
    PreReleaseNumber : Nullable<int>
    BuildMetaData : string
    BuildMetaDataPadded : string
    FullBuildMetaData : string
    MajorMinorPatch : string
    SemVer : string
    LegacySemVer : string
    LegacySemVerPadded : string
    AssemblySemVer : string
    FullSemVer : string
    InformationalVersion : string
    BranchName : string
    Sha : string
    NuGetVersionV2 : string
    NuGetVersion : string
    CommitsSinceVersionSource : int
    CommitsSinceVersionSourcePadded : string
    CommitDate : string
}

/// Runs [GitVersion](https://gitversion.readthedocs.io/en/latest/) on a .NET project file.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the gitversionDefaults value.
///
/// ## Sample
///
///      generateProperties id // Use Defaults
///      generateProperties (fun p -> { p with ToolPath = "/path/to/directory" }
let generateProperties (setParams : GitversionParams -> GitversionParams) =
    let parameters = gitversionDefaults |> setParams
    let timespan =  TimeSpan.FromMinutes 1.

    let result = Process.execWithResult (fun info ->
        {info with FileName = parameters.ToolPath}
        |> Process.withFramework) timespan
    if result.ExitCode <> 0 then failwithf "GitVersion.exe failed with exit code %i and message %s" result.ExitCode (String.concat "" result.Messages)
    result.Messages |> String.concat "" |> fun j -> JsonConvert.DeserializeObject<GitVersionProperties>(j)
