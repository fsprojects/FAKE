module Fake.NuGetVersion

open System
open System.Net
open Newtonsoft.Json
open Fake.SemVerHelper
open System.Xml
open System.Xml.Linq

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
      sprintf "%s/Search()?$filter=IsLatestVersion&searchTerm='%s'&includePrerelease=false"
        server packageName
    let client = new WebClient()
    client.Headers.Add("Accept", "application/json")
    let text = client.DownloadString url
    let hasContentType = client.ResponseHeaders.AllKeys |> Seq.contains "Content-Type"
    let version =
      if hasContentType && client.ResponseHeaders.Item("Content-Type").Contains "application/json"
      then
        let json = JsonConvert.DeserializeObject<NuGetSearchResponse>(text)
        json.d.results
        |> Seq.sortByDescending (fun i -> i.Published)
        |> Seq.tryHead
        |> fun i -> 
            match i with 
            | Some v -> Some (SemVerHelper.parse v.Version)
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
                          | Some n -> Some (SemVerHelper.parse n.Value)
                          | None -> None
              | None -> None
    version
    

/// Compute next NuGet version number
let nextVersion (f : NuGetVersionArg -> NuGetVersionArg) =
    let arg = f (NuGetVersionArg.Default())
    match getLastNuGetVersion arg.Server arg.PackageName with
    | Some v -> (arg.Increment v).ToString()
    | None -> arg.DefaultVersion

