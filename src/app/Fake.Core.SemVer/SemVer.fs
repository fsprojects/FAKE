namespace Fake.Core

// This implementation is taken from Paket

open System
open System.Globalization
open System.Text.RegularExpressions

/// <summary>
/// Contains active patterns which allow to deal with <a href="http://semver.org/">Semantic Versioning (SemVer)</a>.
/// </summary>
module SemVerActivePattern =
    let (|ParseRegex|_|) pattern input  =
        let m = Regex.Match(input, pattern, RegexOptions.ExplicitCapture)

        match m.Success with
        | true ->
            Some (List.tail [ for g in m.Groups -> g.Value ])
        | false ->
            None

    [<Literal>]
    let Pattern =
        @"^(?<major>\d+)" +
        @"(\.(?<minor>\d+))?" +
        @"(\.(?<patch>\d+))?" +
        @"(\-(?<pre>[0-9A-Za-z\-\.]+))?" +
        @"(\+(?<build>[0-9A-Za-z\-\.]+))?$"

    let (|SemVer|_|) version =

        match version with
        | ParseRegex Pattern [major; minor; patch; pre; build] ->
            Some [major; minor; patch; pre; build]
        | _ ->
            None

    let (|ValidVersion|_|) = function
        | null | "" -> None
        | ver when ver.Length > 1 && ver.StartsWith("0") -> None
        | _ -> Some ValidVersion


[<CustomEquality; CustomComparison>]
type PreReleaseSegment = 
    | AlphaNumeric of string
    | Numeric of bigint

    member x.CompareTo(y) =
        match x, y with
        | AlphaNumeric a, AlphaNumeric b -> compare a b
        | Numeric a, Numeric b -> compare a b
        | AlphaNumeric _, Numeric _ -> 1
        | Numeric _, AlphaNumeric _ -> -1

    interface IComparable with
        member x.CompareTo yobj =
            match yobj with
            | :? PreReleaseSegment as y -> x.CompareTo(y)                
            | _ -> invalidArg "yobj" "can't compare to other types of objects."
            
    override x.GetHashCode() = hash x
    
    member x.Equals(y) =
        match x, y with
        | AlphaNumeric a, AlphaNumeric b -> a = b
        | Numeric a, Numeric b -> a = b
        | AlphaNumeric _, Numeric _ -> false
        | Numeric _, AlphaNumeric _ -> false

    override x.Equals yobj = 
        match yobj with 
        | :? PreReleaseSegment as y -> x.Equals(y)
        | _ -> false

/// <summary>
/// Information about PreRelease packages.
/// </summary>
[<CustomEquality; CustomComparison>]
type PreRelease = 
    { Origin : string
      Name : string
      Values : PreReleaseSegment list }
      
    static member TryParse (str : string) = 
        if String.IsNullOrEmpty str then None
        else
            let getName fromList =
                match fromList with
                | AlphaNumeric(a)::_ -> a
                | _::AlphaNumeric(a)::_ -> a // fallback to 2nd
                | _ -> ""
                
            let parse (segment: string) =
                match bigint.TryParse segment with
                | true, number when number >= 0I -> Numeric number
                | _ -> AlphaNumeric segment
                
            let notEmpty = StringSplitOptions.RemoveEmptyEntries
            let name, values = 
                match str.Split([|'.'|],notEmpty) with
                | [|one|] -> 
                    let list = one.Split([|'-'|],notEmpty) |> Array.map parse |> List.ofArray
                    
                    // semver1-like embedded / inlined prerelease numbers
                    let name =
                        match Regex("^(?<name>[a-zA-Z]+)").Match(one) with 
                        | ex when ex.Success -> ex.Value
                        | _ -> getName list
                        
                    name, list
                                    
                | multiple -> //semver2: dashes are ok, inline numbers not
                
                    let list = multiple |> Array.map parse |> List.ofArray
                    getName list, list
                    
            Some { Origin = str; Name = name; Values = values }

    member x.Equals(y) = x.Origin = y.Origin

    override x.Equals(yobj) = 
        match yobj with
        | :? PreRelease as y -> x.Equals(y)
        | _ -> false
        
    override x.ToString() = x.Origin
    
    override x.GetHashCode() = hash x.Origin
    
    member x.CompareTo(yobj) = 
        let rec cmp item count xlist ylist = 
            if item < count then
                let res = compare (List.head xlist) (List.head ylist)
                if res = 0 then 
                    cmp (item + 1) count (List.tail xlist) (List.tail ylist)
                else
                    res // result given by first difference
            else
                sign xlist.Length - ylist.Length // https://semver.org/#spec-item-11
        let len = min x.Values.Length yobj.Values.Length // compare up to common len
        cmp 0 len x.Values yobj.Values
        
    interface IComparable with
        member x.CompareTo yobj =
            match yobj with
            | :? PreRelease as y -> x.CompareTo(y)
            | _ -> invalidArg "yobj" "PreRelease: cannot compare to values of different types"


/// <summary>
/// Contains the version information. For parsing use <a href="guide/fake-core-semver.html"><c>SemVer.parse</c></a>
/// </summary>
/// <remarks>
/// Note: If you use <c>{ version with Patch = myPath; Original = None }</c> to overwrite some parts of this
/// string make sure to overwrite <c>Original</c> to <c>None</c> in order to recalculate the version string.
/// </remarks>
/// <remarks>
/// For overwriting the <c>PreRelease</c> part use:
/// <c>{ Version with Original = None; PreRelease = PreRelease.TryParse "alpha.1" }</c>
/// </remarks>
[<CustomEquality; CustomComparison; StructuredFormatDisplay("{AsString}")>]
type SemVerInfo = 
    {
      /// MAJOR version when you make incompatible API changes.
      Major : uint32
      /// MINOR version when you add functionality in a backwards-compatible manner.
      Minor : uint32
      /// PATCH version when you make backwards-compatible bug fixes.
      Patch : uint32
      /// The optional PreRelease version
      PreRelease : PreRelease option
      /// The optional build no.
      Build : bigint
      BuildMetaData : string
      // The original version text
      Original : string option }
    
    member x.Normalize() = 
        let build = 
            if x.Build > 0I then ("." + x.Build.ToString("D")) else ""
                        
        let pre = 
            match x.PreRelease with
            | Some preRelease -> ("-" + preRelease.Origin)
            | None -> ""

        sprintf "%d.%d.%d%s%s" x.Major x.Minor x.Patch build pre

    member x.NormalizeToShorter() = 
        let s = x.Normalize()
        let s2 = sprintf "%d.%d" x.Major x.Minor
        if s = s2 + ".0" then s2 else s

    override x.ToString() = 
        match x.Original with
        | Some version -> version.Trim()
        | None -> x.Normalize()
    
    member x.AsString
        with get() = x.ToString()
        
    member x.Equals(y) =
        x.Major = y.Major && x.Minor = y.Minor && x.Patch = y.Patch && x.Build = y.Build && x.PreRelease = y.PreRelease

    override x.Equals(yobj) = 
        match yobj with
        | :? SemVerInfo as y -> x.Equals(y)
        | _ -> false
    
    override x.GetHashCode() = hash (x.Major, x.Minor, x.Patch, x.Build, x.PreRelease)
    
    member x.CompareTo(y) =
        let comparison =  
            match compare x.Major y.Major with 
            | 0 ->
                match compare x.Minor y.Minor with
                | 0 ->
                    match compare x.Patch y.Patch with
                    | 0 ->  
                        match compare x.Build y.Build with 
                        | 0 -> 
                            match x.PreRelease, y.PreRelease with
                            | None, None -> 0
                            | Some _, None -> -1
                            | None, Some _ -> 1
                            | Some p, Some p2 when p.Origin = "prerelease" && p2.Origin = "prerelease" -> 0
                            | Some p, _ when p.Origin = "prerelease" -> -1
                            | _, Some p when p.Origin = "prerelease" -> 1
                            | Some left, Some right -> compare left right
                        | c -> c
                    | c -> c
                | c -> c
            | c -> c
        comparison
    
    interface IComparable with
        member x.CompareTo yobj = 
            match yobj with
            | :? SemVerInfo as y -> x.CompareTo(y)
            | _ -> invalidArg "yobj" "SemVerInfo: cannot compare to values of different types"

/// <summary>
/// Parser which allows to deal with <a href="http://semver.org/">Semantic Versioning (SemVer)</a>.
/// Make sure to read the documentation in the <a href="guide/fake-core-semverinfo.html">SemVerInfo</a>
/// record as well if you manually create versions.
/// </summary>
[<RequireQualifiedAccess>]
module SemVer =
    open System.Numerics
    open SemVerActivePattern
  
    /// <summary>
    /// Returns true if input appears to be a parsable semver string
    /// </summary>
    ///
    /// <param name="version">The string version to check</param>
    let isValid version =
        match version with
        | SemVer [ValidVersion major; ValidVersion minor; ValidVersion patch; pre; build] ->
            true
        | _ ->
            false

    /// <summary>
    /// Matches if str is convertible to Int and not less than zero, and returns the value as UInt.
    /// </summary>
    let inline private (|Int|_|) (str: string) =
        match Int32.TryParse (str, NumberStyles.Integer, null) with
        | true, num when num > -1 -> Some num
        | _ -> None
        
    /// <summary>
    /// Matches if str is convertible to big int and not less than zero, and returns the bigint value.
    /// </summary>
    let inline private (|Big|_|) (str: string) =
        match BigInteger.TryParse (str, NumberStyles.Integer, null) with
        | true, big when big > -1I -> Some big
        | _ -> None

    /// <summary>
    /// Splits the given version string by possible delimiters but keeps them as parts of resulting list.
    /// </summary>
    let private expand delimiter (text : string) =
        let sb = Text.StringBuilder()
        let res = seq {
            for ch in text do
                match List.contains ch delimiter with
                | true -> 
                    yield sb.ToString()
                    sb.Clear() |> ignore
                    yield ch.ToString()
                | false ->
                    sb.Append(ch) |> ignore
            if sb.Length > 0 then
                yield sb.ToString()
                sb.Clear() |> ignore
            }
        res |> Seq.toList
        
    let private validContent = Regex(@"(?in)^[a-z0-9-]+(\.[a-z0-9-]+)*")

    /// <summary>
    /// Parses the given version string into a SemVerInfo which can be printed using ToString() or compared
    /// according to the rules described in the <a href="http://semver.org/">SemVer docs</a>.
    /// </summary>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// parse "1.0.0-rc.1"     &lt; parse "1.0.0"          // true
    /// parse "1.2.3-alpha"    &gt; parse "1.2.2"          // true
    /// parse "1.2.3-alpha2"   &gt; parse "1.2.3-alpha"    // true
    /// parse "1.2.3-alpha002" &gt; parse "1.2.3-alpha1"   // false
    /// parse "1.5.0-beta.2"   &gt; parse "1.5.0-rc.1"     // false
    /// </code>
    /// </example>
    let parse (version : string) = 
        try
            // sanity check to make sure that all of the integers in the string are positive.
            // because we use raw substrings with dashes this is very complex :(
            for s in version.Split([|'.'|]) do
                match Int32.TryParse s with 
                | true, s when s < 0 -> failwith "no negatives!" 
                | _ -> ()  // non-numeric parts are valid

            if version.Contains("!") then 
                failwithf "Invalid character found in %s" version
            if version.Contains("..") then 
                failwithf "Empty version part found in %s" version

            let plusIndex = version.IndexOf("+")

            let versionStr = 
                match plusIndex with
                | n when n < 0 -> version
                | n -> version.Substring(0, n)

            /// there can only be one piece of build metadata, and it is signified by + sign
            /// and then any number of dot-separated alpha-numeric groups.
            let buildMeta =
                match plusIndex with
                | -1 -> ""
                | n when n = version.Length - 1 -> ""
                | n -> 
                    let content = validContent.Match(version.Substring(n + 1))
                    if content.Success then content.Value else ""

            let fragments = expand [ '.'; '-' ] versionStr
            /// matches over list of the version fragments *and* delimiters
            let major, minor, patch, revision, suffix =
                match fragments with
                | Int M::"."::Int m::"."::Int p::"."::Big b::tail -> M, m, p, b, tail
                | Int M::"."::Int m::"."::Int p::tail -> M, m, p, 0I, tail
                | Int M::"."::Int m::tail -> M, m, 0, 0I, tail
                | Int M::tail -> M, 0, 0, 0I, tail
                | _ -> raise(ArgumentException("SemVer.Parse", "version"))
                //this is expected to fail, for now :/
                //| [text] -> 0, 0, 0, 0I, [text] 
                //| [] | _ -> 0, 0, 0, 0I, []
            
            /// recreate the remaining string to parse as prerelease segments
            let prerelease =
                if suffix.IsEmpty || suffix.Tail.IsEmpty then ""
                else String.Concat(suffix.Tail).TrimEnd([|'.'; '-'|])

            { Major = uint32 major
              Minor = uint32 minor
              Patch = uint32 patch
              Build = revision
              PreRelease = PreRelease.TryParse prerelease
              BuildMetaData = buildMeta
              Original = Some version }

        with
        | e ->
            raise <| exn(sprintf "Can't parse \"%s\"." version, e)
