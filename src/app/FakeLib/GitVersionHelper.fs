module Fake.GitVersionHelper

open FSharp.Data
open FSharp.Data.JsonExtensions
open System

type GitVersionProperties = {
                                Major : int;
                                Minor : int;
                                Patch : int;
                                PreReleaseTag : string;
                                PreReleaseTagWithDash : string;
                                PreReleaseLabel : string;
                                PreReleaseNumber : int;
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

                            static member FromJson (j : JsonValue) =
                                {
                                 Major = j?Major.AsInteger();
                                 Minor = j?Minor.AsInteger();
                                 Patch = j?Patch.AsInteger();
                                 PreReleaseTag = j?PreReleaseTag.AsString();
                                 PreReleaseTagWithDash= j?PreReleaseTagWithDash.AsString();
                                 PreReleaseLabel= j?PreReleaseLabel.AsString();
                                 PreReleaseNumber= j?PreReleaseNumber.AsInteger();
                                 BuildMetaData = j?BuildMetaData.AsString();
                                 BuildMetaDataPadded= j?BuildMetaDataPadded.AsString();
                                 FullBuildMetaData = j?FullBuildMetaData.AsString();
                                 MajorMinorPatch = j?MajorMinorPatch.AsString();
                                 SemVer = j?SemVer.AsString();
                                 LegacySemVer= j?LegacySemVer.AsString();
                                 LegacySemVerPadded = j?LegacySemVerPadded.AsString();
                                 AssemblySemVer = j?AssemblySemVer.AsString();
                                 FullSemVer= j?FullSemVer.AsString();
                                 InformationalVersion = j?InformationalVersion.AsString();
                                 BranchName = j?BranchName.AsString();
                                 Sha = j?Sha.AsString();
                                 NuGetVersionV2= j?NuGetVersionV2.AsString();
                                 NuGetVersion = j?NuGetVersion.AsString();
                                 CommitsSinceVersionSource = j?CommitsSinceVersionSource.AsInteger();
                                 CommitsSinceVersionSourcePadded = j?CommitsSinceVersionSourcePadded.AsString();
                                 CommitDate = j?CommitDate.AsString();
                               }

let GitVersion () =
    let dirs = [ (environVar "ProgramData")  @@ "chocolatey" @@ "bin" ]
    let file = "GitVersion.exe"
    let timespan =  TimeSpan.FromMinutes 1.
    let usage = "To install GitVersion.exe, start PowerShell as Administrator and run choco install gitversion.portable -s https://chocolatey.org/api/v2"

    let run executable =
        let result = ExecProcessAndReturnMessages (fun info ->
            info.FileName <- executable ) timespan
        if result.ExitCode <> 0 then failwithf "%s failed with exit code %i" executable result.ExitCode
        result.Messages |> String.concat ""


    match tryFindFile dirs file with
        | Some executable -> run executable  |> JsonValue.Parse |> GitVersionProperties.FromJson // |> GitVersionJson.Parse
        | None -> failwithf  "%s is not installed. %s" file usage
