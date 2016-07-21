module Fake.GitVersionHelper

open FSharp.Data
open Newtonsoft.Json
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
        | Some executable -> run executable  |> fun j -> JsonConvert.DeserializeObject<GitVersionProperties>(j)
        | None -> failwithf  "%s is not installed. %s" file usage
