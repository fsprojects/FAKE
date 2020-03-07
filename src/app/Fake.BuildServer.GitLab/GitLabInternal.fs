/// Contains support for various build servers
namespace Fake.BuildServer

open Fake.Core

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Fake.Core.UnitTests")>]
do ()

module internal GitLabInternal =
    let environVar = Environment.environVar
    let getJobId () = environVar "CI_JOB_ID"
    