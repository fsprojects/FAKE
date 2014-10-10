namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Deploy Lib")>]
[<assembly: GuidAttribute("AA284C42-1396-42CB-BCAC-D27F18D14AC7")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("3.5.9")>]
[<assembly: AssemblyInformationalVersionAttribute("3.5.9")>]
[<assembly: AssemblyFileVersionAttribute("3.5.9")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "3.5.9"
