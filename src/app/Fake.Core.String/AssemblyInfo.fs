﻿namespace System
open System.Reflection

[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("1.0.0")>]
[<assembly: AssemblyInformationalVersionAttribute("1.0.0-alpha9")>]
[<assembly: AssemblyFileVersionAttribute("1.0.0")>]
[<assembly: AssemblyTitleAttribute("FAKE - F# Fake.Core.String")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0.0"
    let [<Literal>] InformationalVersion = "1.0.0-alpha9"
