namespace System
open System.Runtime.CompilerServices

[<assembly: InternalsVisibleTo("Fake.Core.UnitTests")>]
[<assembly: InternalsVisibleTo("Fake.Core.IntegrationTests")>]
do ()
