namespace System
open System.Runtime.CompilerServices

[<assembly: InternalsVisibleTo("Fake.Core.IntegrationTests")>]
[<assembly: InternalsVisibleTo("Fake.Core.UnitTests")>]

// For access to CreateProcess<'TRes>.Command
[<assembly: InternalsVisibleTo("Fake.DotNet.Cli")>]
do ()
