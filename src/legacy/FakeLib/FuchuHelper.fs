[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.FuchuHelper

/// Execute Fuchu tests from one or more assemblies.
/// Multiple assemblies are run concurrently.
/// ## Parameters
/// 
///  - `testExes` - The paths of the executables containing Fuchu tests to run.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Fuchu testExes = 
    let errorCode =
        testExes
        |> Seq.map (fun program -> if not isMono
                                   then program, null
                                   else "mono", program)
        |> Seq.map (fun (program, args) -> asyncShellExec { defaultParams with Program = program; CommandLine = args })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.sum
    if errorCode <> 0
    then failwith "Unit tests failed"
