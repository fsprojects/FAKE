
namespace System
open System.Runtime.CompilerServices

[<assembly: InternalsVisibleTo("Fake.DotNet.Cli")>]
[<assembly: InternalsVisibleTo("Fake.Core.IntegrationTests")>]
do ()