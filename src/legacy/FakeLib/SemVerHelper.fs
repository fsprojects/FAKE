/// Contains helpers which allow to deal with [Semantic Versioning](http://semver.org/) (SemVer).
[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
module Fake.SemVerHelper

open System
open System.Text.RegularExpressions

[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
let identRE = Regex("[0-9A-Za-z-]+", RegexOptions.Compiled)
[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
[<CustomEquality;CustomComparison>]
type Ident = 
    | AlphaNumeric of string | Numeric of int64
    override x.Equals(yobj) = 
        match yobj with
        | :? Ident as y -> 
            match x,y with
            | AlphaNumeric a, AlphaNumeric b -> a = b
            | AlphaNumeric _, Numeric _ -> false
            | Numeric _, AlphaNumeric _ -> false
            | Numeric a, Numeric b -> a = b
        | _ -> false
    override x.ToString() = match x with | AlphaNumeric a -> a | Numeric b -> string b
    override x.GetHashCode() = match x with | AlphaNumeric a -> hash a | Numeric b -> hash b
    interface IComparable with
        member x.CompareTo yobj =
            match yobj with
            // spec says that alpha is always greater than numeric, alpha segments are compared lexicographically, which in .Net is the ordinal comparer
            | :? Ident as y ->
                match x,y with
                | AlphaNumeric a, AlphaNumeric b -> StringComparer.Ordinal.Compare(a, b)
                | AlphaNumeric _, Numeric _ -> 1
                | Numeric _, AlphaNumeric _ -> -1
                | Numeric a, Numeric b -> compare a b
            | _ -> invalidArg "yobj" "cannot compare values of different types"

[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
let parseIdent s = 
    match Int64.TryParse s with 
    | true, i -> Numeric i
    | false, _ -> AlphaNumeric s

[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
[<CustomEquality; CustomComparison>]
type PreRelease = 
    { Origin: string
      Name: string
      Number: int option
      Parts : Ident list }
    static member TryParse str = 
        let idents = splitRemove '.' str
        match idents |> List.forall (identRE.IsMatch) with
        | false -> None
        | true -> 
            let namedPrereleaseRE = Regex("^(?<name>[a-zA-Z]+)(?<number>\d*)$", RegexOptions.Compiled)        
            let m = namedPrereleaseRE.Match(str)
            let parts = idents |> List.map parseIdent
            match m.Success, m.Groups.["name"].Value, m.Groups.["number"].Value with
            | true, name, "" -> Some { Origin = str; Name = name; Number = None; Parts = parts }
            | true, name, number -> Some { Origin = str; Name = name; Number = Some (int number); Parts = parts }
            | false, _, _ when idents.Length = 1 -> 
                Some { Origin = idents.[0]; Name = ""; Parts = parts; Number = match Int32.TryParse idents.[0] with | true, n -> Some n | false, _ -> None; }
            | false, _, _ -> 
                Some { Origin = str; Name = ""; Number = None; Parts = parts}
    override x.ToString() = String.Join(".", x.Parts |> List.map string)
    override x.Equals(yobj) =
        match yobj with
        | :? PreRelease as y -> x.Origin = y.Origin
        | _ -> false
    override x.GetHashCode() = hash x.Origin
    interface IComparable with
        member x.CompareTo yobj =
            match yobj with
            // spec says that longer prereleases are bigger
            | :? PreRelease as y ->
                if x.Parts.Length <> y.Parts.Length then compare x.Parts.Length y.Parts.Length
                else 
                    List.zip x.Parts y.Parts
                    |> List.fold (fun cmp (x,y) -> if cmp <> 0 then cmp else compare x y) 0
                    
            | _ -> invalidArg "yobj" "cannot compare values of different types"

/// Contains the version information.
[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
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
      Build: string 
      BuildIdentifiers : Ident list}
    override x.ToString() =
        sprintf "%d.%d.%d%s%s" x.Major x.Minor x.Patch 
         (match x.PreRelease with
          | Some preRelease -> "-" + string preRelease 
          | None -> "")
         (if isNotNullOrEmpty x.Build then "+" + x.Build else "")

    override x.Equals(yobj) =
        match yobj with
        | :? SemVerInfo as y -> 
            (x.Minor,x.Minor,x.Patch,x.PreRelease,x.Build) = 
              (y.Minor,y.Minor,y.Patch,y.PreRelease,y.Build)
        | _ -> false
 
    override x.GetHashCode() = hash (x.Minor,x.Minor,x.Patch,x.PreRelease,x.Build)
    interface IComparable with
        member x.CompareTo yobj =
            match yobj with
            | :? SemVerInfo as y ->
                if x.Major <> y.Major then compare x.Major y.Major else
                if x.Minor <> y.Minor then compare x.Minor y.Minor else
                if x.Patch <> y.Patch then compare x.Patch y.Patch else
                if x.PreRelease = y.PreRelease && x.Build = y.Build  then 0 else
                if x.PreRelease.IsNone && x.Build = "" then 1 else
                if y.PreRelease.IsNone && y.Build = "" then -1 else
                if x.PreRelease <> y.PreRelease then compare x.PreRelease y.PreRelease
                // spec says that build should have no impact on comparisons
                else
                    0
            | _ -> invalidArg "yobj" "cannot compare values of different types"


let private SemVerPattern = "^(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?(?:\+[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?$"

/// Returns true if input appears to be a parsable semver string
[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
let isValidSemVer input =
    let m = Regex.Match(input, SemVerPattern)
    if m.Success then true
    else false

/// Parses the given version string into a SemVerInfo which can be printed using ToString() or compared
/// according to the rules described in the [SemVer docs](http://semver.org/).
/// ## Sample
///
///     parse "1.0.0-rc.1"     < parse "1.0.0"          // true
///     parse "1.2.3-alpha"    > parse "1.2.2"          // true
///     parse "1.2.3-alpha2"   > parse "1.2.3-alpha"    // true, but only because of lexical compare
///     parse "1.2.3-alpha002" > parse "1.2.3-alpha1"   // false, due to lexical compare
///     parse "1.5.0-beta.2"   > parse "1.5.0-rc.1"     // false, due to lexical compare of first prerelease identitifer
///     parse "1.5.0-beta.2"   > parse "1.5.0-beta.3"   // true, due to numeric compare of second prerelease identitifer
///     parse "1.5.0-0123.001" < parse "1.5.0-0123.002" // true, due to numeric compare of second prerelease identifier
///     parse "1.0.0+lol"      = parse "1.0.0"          // true, because build identifiers do not influence comparison
[<System.Obsolete("FAKE0001 Use the Fake.Core.SemVer module instead")>]
let parse version =
    let startPos (c : char) (s :string) = match s.IndexOf(c) with | -1 -> None | n -> Some n
    let buildPartStart = startPos '+' version
    let prereleasePartStart = startPos '-' version
    let main, pre, build = 
        match prereleasePartStart, buildPartStart with
        | None, None -> version, None, None
        | Some n, None -> version.[0..n-1], Some version.[n+1..], None
        | None, Some n -> version.[0..n-1], None, Some version.[n+1..]
        | Some n, Some m -> version.[0..n-1], Some version.[n+1..m-1], Some version.[m+1..]
    
    let maj, minor, patch = 
        match split '.' main with
        // odd case for sillies that use non-standard semver? that's why xs instead of []
        | maj::min::pat::xs -> Int32.Parse maj, Int32.Parse min, Int32.Parse pat
        | maj::min::[] -> Int32.Parse maj, Int32.Parse min, 0
        | maj::[] -> Int32.Parse maj, 0, 0
        | [] -> 0,0,0
    
    
    let buildParts = Option.map (fun b -> splitRemove '.' b) build
    if buildParts.IsSome && buildParts.Value |> List.exists (not << identRE.IsMatch) then failwith "unknown semver build format"

    { Major = maj
      Minor = minor
      Patch = patch
      PreRelease = Option.bind PreRelease.TryParse pre
      Build = defaultArg build "" 
      BuildIdentifiers = defaultArg (buildParts |> Option.map (List.map parseIdent)) List.empty
    }



