namespace System
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Deploy Web")>]
[<assembly: InternalsVisibleToAttribute("Test.FAKECore")>]
[<assembly: GuidAttribute("9B1A0CF2-FE31-4649-AD2D-F061A7B8AE14")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]

()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.1"
