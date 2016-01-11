[<AutoOpen>]
module Fake.NugetVersion

open System
open System.Net
open Newtonsoft.Json
open Fake.SemVerHelper

type NugetSearchItemResult =
    { Version:string
      Published:DateTime }
type NugetSearchResult = 
    { results:NugetSearchItemResult list }
type NugetSearchResponse = 
    { d:NugetSearchResult }
type NugetVersionIncrement = SemVerInfo -> SemVerInfo

/// Increment patch version
let IncPatch:NugetVersionIncrement = 
    fun (v:SemVerInfo) ->
        { v with Build=""; Patch=(v.Patch+1) }

/// Increment minor version
let IncMinor:NugetVersionIncrement = 
    fun (v:SemVerInfo) ->
        { v with Build=""; Patch=0; Minor=(v.Minor+1) }

/// Increment major version
let IncMajor:NugetVersionIncrement = 
    fun (v:SemVerInfo) ->
        { v with Build=""; Patch=0; Minor=0; Major=(v.Major+1) }

/// Arguments for the next nuget version number computing
type NugetVersionArg =
    { Server:string
      PackageName:string
      Increment:NugetVersionIncrement
      DefaultVersion:string }
    /// Default arguments to compute next nuget version number
    static member Default() =
        { Server="https://www.nuget.org/api/v2"
          PackageName=""
          Increment=IncMinor
          DefaultVersion="1.0" }

/// Retrieve current nuget version number
let getlastNugetVersion server (packageName:string) = 
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
    let json = JsonConvert.DeserializeObject<NugetSearchResponse>(text)
    json.d.results
    |> Seq.sortByDescending (fun i -> i.Published)
    |> Seq.tryHead
    |> fun i -> 
        match i with 
        | Some v -> Some (SemVerHelper.parse v.Version)
        | None -> None

/// Compute next nuget version number
let nextVersion (f : NugetVersionArg -> NugetVersionArg) =
    let arg = f (NugetVersionArg.Default())
    match getlastNugetVersion arg.Server arg.PackageName with
    | Some v -> (arg.Increment v).ToString()
    | None -> arg.DefaultVersion

