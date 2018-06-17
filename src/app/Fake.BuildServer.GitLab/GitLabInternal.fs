/// Contains support for various build servers
namespace Fake.BuildServer

open Fake.Core

module internal GitLabInternal =
    let environVar = Environment.environVar
    let getJobId () = environVar "CI_JOB_ID"
    