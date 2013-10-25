/// Contains helpers which allow to deal with [Semantic Versioning](http://semver.org/) (SemVer).
module Fake.SemVerHelper

open System
open System.Text.RegularExpressions

/// Contains the version information.
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