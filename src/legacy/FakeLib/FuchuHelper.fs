﻿[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.FuchuHelper

/// Execute Fuchu tests from one or more assemblies.
/// Multiple assemblies are run concurrently.
/// ## Parameters
///
///  - `testExes` - The paths of the executables containing Fuchu tests to run.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Fuchu testExes =
    let errorCode =
        testExes
        |> Seq.map (fun program -> if not isMono then program, null else "mono", program)
        |> Seq.map (fun (program, args) -> asyncShellExec { defaultParams with Program = program; CommandLine = args })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.sum

    if errorCode <> 0 then
        failwith "Unit tests failed"
