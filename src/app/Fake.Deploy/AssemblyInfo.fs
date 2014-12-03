namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Deploy tool")>]
[<assembly: GuidAttribute("413E2050-BECC-4FA6-87AA-5A74ACE9B8E1")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("3.10.1")>]
[<assembly: AssemblyInformationalVersionAttribute("3.10.1")>]
[<assembly: AssemblyFileVersionAttribute("3.10.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "3.10.1"
