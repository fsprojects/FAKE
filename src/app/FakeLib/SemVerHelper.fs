/// Contains helpers which allow to deal with [Semantic Versioning](http://semver.org/) (SemVer).
module Fake.SemVerHelper

open System
open System.Text.RegularExpressions

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
      PreRelease : string
      /// The optional build no.
      Build: string }
    override x.ToString() =
        sprintf "%d.%d.%d" x.Major x.Minor x.Patch +
         (if isNotNullOrEmpty x.PreRelease || isNotNullOrEmpty x.Build then "-" + x.PreRelease else "") +
         (if isNotNullOrEmpty x.Build then "." + x.Build else "")

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
                if x.PreRelease = "" && x.Build = "" then 1 else
                if y.PreRelease = "" && y.Build = "" then -1 else
                if x.PreRelease <> y.PreRelease then compare x.PreRelease y.PreRelease else
                if x.Build <> y.Build then 
                    match Int32.TryParse x.Build, Int32.TryParse y.Build with
                    | (true,b1),(true,b2) -> compare b1 b2
                    | _ -> compare x.Build y.Build 
                else
                    0
            | _ -> invalidArg "yobj" "cannot compare values of different types"


let parse version =
    let splitted = split '.' version
    let l = splitted.Length
    let patch,preRelease =
        if l <= 2 then 0,"" else
        let splitted' = split '-' splitted.[2]
        Int32.Parse splitted'.[0],if splitted'.Length > 1 then splitted'.[1] else ""


    { Major = if l > 0 then Int32.Parse splitted.[0] else 0
      Minor = if l > 1 then Int32.Parse splitted.[1] else 0
      Patch = patch
      PreRelease = preRelease
      Build = if l > 3 then splitted.[3] else ""
    }