module Fake.DotNet.NuGet.Version

open Fake.IO.FileSystem.Operators
open Fake.DotNet.NuGet.NuGet
open Fake.Core.String
open Fake.Core.BuildServer
open Fake.Core.Process
open Fake.Core
open Fake.Core.SemVer
open Fake.DotNet.NuGet.NuGet
open Newtonsoft.Json
open System
open System.Xml.Linq

type NuGetSearchItemResult =
    { Id:string
      Version:string
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
      sprintf "%s/Search()?$filter=IsLatestVersion&searchTerm='%s'&includePrerelease=false"
        server packageName
    let client = new WebClient()
    addHeader client "Accept" "application/json, application/xml"
    let text, headers = client.DownloadStringAndHeaders url
    let hasContentType = headers.ContainsKey "Content-Type"
    let version =
      if hasContentType && headers.["Content-Type"] |> List.exists (fun e -> e.Contains "application/json")
      then
        let json = JsonConvert.DeserializeObject<NuGetSearchResponse>(text)
        json.d.results
        |> Seq.filter (fun i -> i.Id = packageName)
        |> Seq.sortByDescending (fun i -> i.Published)
        |> Seq.tryHead
        |> fun i ->
            match i with
            | Some v -> Some (SemVer.parse v.Version)
            | None -> None
      else
        let xml = XDocument.Parse text
        let xmlns = "http://www.w3.org/2005/Atom"
        let xmlnsd="http://schemas.microsoft.com/ado/2007/08/dataservices"
        let xmlnsm="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"
        xml.Descendants(XName.Get("entry", xmlns))
          |> Seq.filter (
              fun entry ->
                entry.Elements(XName.Get("title", xmlns))
                  |> Seq.exists (
                      fun t ->
                        t.Attribute(XName.Get "type").Value = "text"
                        && t.Value = packageName
                     )
             )
          |> Seq.tryHead
          |> function
              | Some e ->
                  e.Descendants(XName.Get ("properties", xmlnsm))
                  |> fun props ->
                      props.Elements(XName.Get ("Version", xmlnsd))
                      |> Seq.tryHead
                      |> function
                          | Some n -> Some (SemVer.parse n.Value)
                          | None -> None
              | None -> None
    version


/// Compute next NuGet version number
let nextVersion (f : NuGetVersionArg -> NuGetVersionArg) =
    let arg = f (NuGetVersionArg.Default())
    match getLastNuGetVersion arg.Server arg.PackageName with
    | Some v -> (arg.Increment v).ToString()
    | None -> arg.DefaultVersion

