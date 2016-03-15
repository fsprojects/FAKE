namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Experimental Lib")>]
[<assembly: GuidAttribute("5AA28AED-B9D8-4158-A594-32FE5ABC5713")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("4.22.2")>]
[<assembly: AssemblyInformationalVersionAttribute("4.22.2")>]
[<assembly: AssemblyFileVersionAttribute("4.22.2")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "4.22.2"
