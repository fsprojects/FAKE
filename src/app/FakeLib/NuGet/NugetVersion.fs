module Fake.NuGetVersion

open System
open System.Net
open Newtonsoft.Json
open Fake.SemVerHelper

type NuGetSearchItemResult =
    { Version:string
      Published:DateTime }
type NuGetSearchResult = 
    { results:NuGetSearchItemResult list }
type NuGetSearchResponse = 
    { d:NuGetSearchResult }
type NuGetVersionIncrement = SemVerInfo -> SemVerInfo

/// Increment patch version
let IncPatch:NuGetVersionIncrement = 
    fun (v:SemVerInfo) ->
        { v with Build=""; Patch=(v.Patch+1) }

/// Increment minor version
let IncMinor:NuGetVersionIncrement = 
    fun (v:SemVerInfo) ->
        { v with Build=""; Patch=0; Minor=(v.Minor+1) }

/// Increment major version
let IncMajor:NuGetVersionIncrement = 
    fun (v:SemVerInfo) ->
        { v with Build=""; Patch=0; Minor=0; Major=(v.Major+1) }

/// Arguments for the next NuGet version number computing
type NuGetVersionArg =
    { Server:string
      PackageName:string
      Increment:NuGetVersionIncrement
      DefaultVersion:string }
    /// Default arguments to compute next NuGet version number
    static member Default() =
        { Server="https://www.nuget.org/api/v2"
          PackageName=""
          Increment=IncMinor
          DefaultVersion="1.0" }

/// Retrieve current NuGet version number
let getLastNuGetVersion server (packageName:string) = 
    let escape = Uri.EscapeDataString
    let url = 
        sprintf "%s/Packages()?$filter=%s%s%s&$orderby=%s"
            server
            (escape "Id eq '")
            packageName
            (escape "'")
            (escape "IsLatestVersion desc")
    let client = new WebClient()
    client.Headers.Add("Accept", "application/json")
    let text = client.DownloadString url
    let json = JsonConvert.DeserializeObject<NuGetSearchResponse>(text)
    json.d.results
    |> Seq.sortByDescending (fun i -> i.Published)
    |> Seq.tryHead
    |> fun i -> 
        match i with 
        | Some v -> Some (SemVerHelper.parse v.Version)
        | None -> None

/// Compute next NuGet version number
let nextVersion (f : NuGetVersionArg -> NuGetVersionArg) =
    let arg = f (NuGetVersionArg.Default())
    match getLastNuGetVersion arg.Server arg.PackageName with
    | Some v -> (arg.Increment v).ToString()
    | None -> arg.DefaultVersion

