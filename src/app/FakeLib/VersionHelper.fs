[<AutoOpen>]
module Fake.VersionHelper

open System
open System.Reflection

/// Contains the version information.
[<CustomEquality; CustomComparison>]
type VerInfo =
    { /// MAJOR version when you make incompatible API changes.
      Major: int
      /// MINOR version when you add functionality in a backwards-compatible manner.
      Minor: int
      /// PATCH version when you make backwards-compatible bug fixes.
      Patch: int
     }

    override x.Equals(yobj) =
        match yobj with
        | :? VerInfo as y -> (x.Minor,x.Minor,x.Patch) = (y.Minor,y.Minor,y.Patch)
        | _ -> false
 
    override x.GetHashCode() = hash (x.Minor,x.Minor,x.Patch)
    
    interface System.IComparable with
        member x.CompareTo yobj =
            match yobj with
            | :? VerInfo as y ->
                if x.Major <> y.Major then compare x.Major y.Major else
                if x.Minor <> y.Minor then compare x.Minor y.Minor else
                if x.Patch <> y.Patch then compare x.Patch y.Patch else                
                    0
            | _ -> invalidArg "yobj" "cannot compare values of different types"

let parseVersion version =
    let splitted = split '.' version
    let l = splitted.Length
    
    { Major = if l > 0 then Int32.Parse splitted.[0] else 0
      Minor = if l > 1 then Int32.Parse splitted.[1] else 0
      Patch = if l > 2 then Int32.Parse splitted.[2] else 0
    }

/// Gets file assembly version.
/// ## Parameters
///  - `assemblyFile` - The assembly file path.
let GetAssemblyVersion (assemblyFile: string) = 
    AssemblyName.GetAssemblyName(assemblyFile).Version

/// Gets file assembly version in form of major.minor.build.revision.
/// ## Parameters
///  - `assemblyFile` - The assembly file path.
let GetAssemblyVersionString (assemblyFile: string) = 
    GetAssemblyVersion(assemblyFile).ToString()

