
namespace Fake.Core

open System
open System.Text.RegularExpressions
open Fake.Core

/// Contains active patterns which allow to deal with [Semantic Versioning](http://semver.org/) (SemVer).
module SemVerActivePattern =
    let (|ParseRegex|_|) pattern input  =
        let m = Regex.Match(input, pattern, RegexOptions.ExplicitCapture)

        match m.Success with
        | true ->
            Some (List.tail [ for g in m.Groups -> g.Value ])
        | false ->
            None

    let (|SemVer|_|) version =
        let pattern =
            @"^(?<major>\d+)" +
            @"(\.(?<minor>\d+))?" +
            @"(\.(?<patch>\d+))?" +
            @"(\-(?<pre>[0-9A-Za-z\-\.]+))?" +
            @"(\+(?<build>[0-9A-Za-z\-\.]+))?$"

        match version with
        | ParseRegex pattern [major; minor; patch; pre; build] ->
            Some [major; minor; patch; pre; build]
        | _ ->
            None

    let (|ValidVersion|_|) = function
        | null | "" -> None
        | ver when ver.Length > 1 && ver.StartsWith("0") -> None
        | _ -> Some ValidVersion

module internal InternalHelper =

    let ComparePreRelease a b =
        let (|Int|_|) str =
           match System.Int32.TryParse(str) with
           | (true, int) -> Some(int)
           | _ -> None

        let comp a b = 
            match (a, b) with
            | (Int a, Int b) -> a.CompareTo(b)
            | (Int a, _) -> -1
            | (_, Int b) -> 1
            | _ -> match String.CompareOrdinal(a, b) with
                   | i when not (i = 0) -> i
                   | _ -> 0

        let aEmpty = String.IsNullOrEmpty(a)
        let bEmpty = String.IsNullOrEmpty(b)

        match (aEmpty, bEmpty) with
        | (true, true) -> 0
        | (true, false) -> 1
        | (false, true) -> -1
        | _ -> Seq.compareWith comp (a.Split '.') (b.Split '.')

open SemVerActivePattern
open InternalHelper

[<CustomEquality; CustomComparison>]
type PreRelease = 
    { Origin: string
      Name: string }
    static member TryParse str = 
        match str with
        | ParseRegex "^(?<name>[0-9A-Za-z\-\.]+)$" [name] ->
            Some { Origin = str; Name = name }
        | _ ->
            None

    override x.Equals(yobj) =
        match yobj with
        | :? PreRelease as y -> x.Origin = y.Origin
        | _ -> false
    override x.GetHashCode() = hash x.Origin
    interface System.IComparable with
        member x.CompareTo yobj =
            match yobj with
            | :? PreRelease as y ->
                ComparePreRelease x.Name y.Name
            | _ -> invalidArg "yobj" "cannot compare values of different types"

/// Contains the version information.
[<CustomEquality; CustomComparison>]
type SemVerInfo =
    { /// MAJOR version when you make incompatible API changes.
      Major: int
      /// MINOR version when you add functionality in a backwards-compatible manner.
      Minor: int
      /// PATCH version when you make backwards-compatible bug fixes.
      Patch: int
      /// The optional PreRelease version
      PreRelease : PreRelease option
      /// The optional build no.
      Build: string }
    override x.ToString() =
        sprintf "%d.%d.%d%s%s" x.Major x.Minor x.Patch
            (match x.PreRelease with
             | Some preRelease -> sprintf "-%s"preRelease.Name 
             | _ -> "")
            (match String.isNotNullOrEmpty x.Build with
             | true -> sprintf "+%s" x.Build
             | _ -> "")

    override x.Equals(yobj) =
        match yobj with
        | :? SemVerInfo as y -> 
            (x.Minor,x.Minor,x.Patch,x.PreRelease,x.Build) = 
              (y.Minor,y.Minor,y.Patch,y.PreRelease,y.Build)
        | _ -> false
 
    override x.GetHashCode() = hash (x.Minor,x.Minor,x.Patch,x.PreRelease,x.Build)
    interface System.IComparable with
        member x.CompareTo yobj =
            match yobj with
            | :? SemVerInfo as y ->
                if x.Major <> y.Major then compare x.Major y.Major else
                if x.Minor <> y.Minor then compare x.Minor y.Minor else
                if x.Patch <> y.Patch then compare x.Patch y.Patch else
                if x.PreRelease = y.PreRelease && x.Build = y.Build  then 0 else
                if x.PreRelease.IsNone && x.Build = "" then 1 else
                if y.PreRelease.IsNone && y.Build = "" then -1 else
                if x.PreRelease <> y.PreRelease then compare x.PreRelease y.PreRelease else
                if x.Build <> y.Build then 
                    match Int32.TryParse x.Build, Int32.TryParse y.Build with
                    | (true,b1),(true,b2) -> compare b1 b2
                    | _ -> compare x.Build y.Build 
                else
                    0
            | _ -> invalidArg "yobj" "cannot compare values of different types"


/// Contains helpers which allow to deal with [Semantic Versioning](http://semver.org/) (SemVer).
module SemVer =
    /// Returns true if input appears to be a parsable semver string
    let isValidSemVer version =
        match version with
        | SemVer [ValidVersion major; ValidVersion minor; ValidVersion patch; pre; build] ->
            true
        | _ ->
            false

    /// Parses the given version string into a SemVerInfo which can be printed using ToString() or compared
    /// according to the rules described in the [SemVer docs](http://semver.org/).
    /// ## Sample
    ///
    ///     parse "1.0.0-rc.1"     < parse "1.0.0"          // true
    ///     parse "1.2.3-alpha"    > parse "1.2.2"          // true
    ///     parse "1.2.3-alpha2"   > parse "1.2.3-alpha"    // true
    ///     parse "1.2.3-alpha002" > parse "1.2.3-alpha1"   // false
    ///     parse "1.5.0-beta.2"   > parse "1.5.0-rc.1"     // false
    let parse version =
        match version with
        | SemVer [major; minor; patch; pre; build] ->
            {
                Major = if String.isNullOrEmpty major then 1 else Int32.Parse major
                Minor = if String.isNullOrEmpty minor then 0 else Int32.Parse minor
                Patch = if String.isNullOrEmpty patch then 0 else Int32.Parse patch
                PreRelease = PreRelease.TryParse pre
                Build = build
            }
        | _ ->
            failwithf "Unable to parse version %s" version

