[<AutoOpen>]
module Fake.NugetVersion

open System
open System.Net
open Newtonsoft.Json

type NugetSearchItemResult =
    { Version:string
      Published:DateTime }
type NugetSearchResult = 
    { results:NugetSearchItemResult list }
type NugetSearchResponse = 
    { d:NugetSearchResult }
type NugetVersionIncrement = string -> Version
    
let private positive i = Math.Max(0, i)

/// Increment build number of a version
let IncBuild:NugetVersionIncrement = 
    fun (version:string) ->
        let v = Version version
        sprintf "%d.%d.%d" (positive v.Major) (positive v.Minor) (positive v.Build+1)
        |> Version

/// Increment minor version
let IncMinor:NugetVersionIncrement = 
    fun (version:string) ->
        let v = Version version
        let n = sprintf "%d.%d.0" (positive v.Major) (positive v.Minor+1)
        Version n

/// Increment major version
let IncMajor:NugetVersionIncrement = 
    fun (version:string) ->
        let v = Version version
        sprintf "%d.0.0" (positive v.Major+1)
        |> Version

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
          Increment=IncBuild
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
    |> fun i -> match i with | Some v -> Some v.Version | None -> None

/// Compute next nuget version number
let nextVersion (f : NugetVersionArg -> NugetVersionArg) =
    let arg = f (NugetVersionArg.Default())
    match getlastNugetVersion arg.Server arg.PackageName with
    | Some v -> (arg.Increment v).ToString()
    | None -> arg.DefaultVersion

