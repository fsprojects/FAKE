namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Experimental Lib")>]
[<assembly: GuidAttribute("5AA28AED-B9D8-4158-A594-32FE5ABC5713")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("4.34.5")>]
[<assembly: AssemblyInformationalVersionAttribute("4.34.5")>]
[<assembly: AssemblyFileVersionAttribute("4.34.5")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "4.34.5"
    let [<Literal>] InformationalVersion = "4.34.5"
