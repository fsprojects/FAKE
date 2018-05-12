/// Contains support for various build servers
namespace Fake.BuildServer

open System.IO
open Fake.Core
open Fake.IO
open Fake.Net

module internal GitLabInternal =
    let environVar = Environment.environVar
    let getJobId () = environVar "CI_JOB_ID"
    