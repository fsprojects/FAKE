/// Containts helper function for GitVersion - a tool to help you achieve Semantic Versioning on your project.
///
/// To install GitVersion.exe on Windows, start PowerShell as Administrator and run choco install gitversion.portable -s https://chocolatey.org/api/v2"
/// For Mac and Unix, install the NuGet version.

[<System.Obsolete("FAKE0001 Use the Fake.Tools.GitVersion module instead")>]
module Fake.GitVersionHelper

open FSharp.Data
open Newtonsoft.Json
open System

[<CLIMutable>]
[<System.Obsolete("FAKE0001 Use the Fake.Tools.GitVersion module instead")>]
type GitversionParams = {
    ToolPath : string
}

[<System.Obsolete("FAKE0001 Use the Fake.Tools.GitVersion module instead")>]
let GitversionDefaults = {
    ToolPath = findToolInSubPath "GitVersion.exe" (environVarOrDefault "ChocolateyInstall" currentDirectory)
}

[<System.Obsolete("FAKE0001 Use the Fake.Tools.GitVersion module instead")>]
type GitVersionProperties = {
                                Major : int;
                                Minor : int;
                                Patch : int;
                                PreReleaseTag : string;
                                PreReleaseTagWithDash : string;
                                PreReleaseLabel : string;
                                PreReleaseNumber : Nullable<int>;
                                BuildMetaData : string;
                                BuildMetaDataPadded : string;
                                FullBuildMetaData : string;
                                MajorMinorPatch : string;
                                SemVer : string;
                                LegacySemVer : string;
                                LegacySemVerPadded : string;
                                AssemblySemVer : string;
                                FullSemVer : string;
                                InformationalVersion : string;
                                BranchName : string;
                                Sha : string;
                                NuGetVersionV2 : string;
                                NuGetVersion : string;
                                CommitsSinceVersionSource : int;
                                CommitsSinceVersionSourcePadded : string;
                                CommitDate : string;
                            }

/// Runs [GitVersion](https://gitversion.readthedocs.io/en/latest/) on a .NET project file.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the GitversionDefaults value.
///
/// ## Sample
///
///      GitVersion id // Use Defaults
///      GitVersion (fun p -> { p with ToolPath = currentDirectory @@ "tools" }
[<System.Obsolete("FAKE0001 Use the Fake.Tools.GitVersion module instead. Function GitVersion.generateProperties")>]
let GitVersion (setParams : GitversionParams -> GitversionParams) =
    let parameters = GitversionDefaults |> setParams
    let timespan =  TimeSpan.FromMinutes 1.

    let result = ExecProcessAndReturnMessages (fun info ->
        info.FileName <- parameters.ToolPath) timespan
    if result.ExitCode <> 0 then failwithf "GitVersion.exe failed with exit code %i and message %s" result.ExitCode (String.concat "" result.Messages)
    result.Messages |> String.concat "" |> fun j -> JsonConvert.DeserializeObject<GitVersionProperties>(j)
