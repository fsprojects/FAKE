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
        [x.Build; x.PreRelease; x.Patch.ToString(); x.Minor.ToString(); x.Major.ToString()]
        |> Seq.skipWhile ((=) "")
        |> Seq.toList
        |> List.rev
        |> separated "."

let parse version =
    let splitted = split '.' version
    let l = splitted.Length

    { Major = if l > 0 then Int32.Parse splitted.[0] else 0
      Minor = if l > 1 then Int32.Parse splitted.[1] else 0
      Patch = if l > 2 then Int32.Parse splitted.[2] else 0
      PreRelease = ""
      Build = ""  
    }