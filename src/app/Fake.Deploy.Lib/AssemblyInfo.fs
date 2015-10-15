namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Deploy Lib")>]
[<assembly: GuidAttribute("AA284C42-1396-42CB-BCAC-D27F18D14AC7")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("4.7.0")>]
[<assembly: AssemblyInformationalVersionAttribute("4.7.0")>]
[<assembly: AssemblyFileVersionAttribute("4.7.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "4.7.0"
