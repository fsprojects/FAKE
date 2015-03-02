[<AutoOpen>]
/// Contains a task to use [rsync](https://rsync.samba.org/) on Linux.
module Fake.RSync

/// Executes a rsync command.
/// ## Parameters
///  - `source` - The source directory
///  - `destination` - The target directory
let RSync (source:string) (destination:string) =
    let args =
          "-r " +
            (source |> toParam) +
            (destination |> toParam) + " -v"

    tracefn "%s" args
    let result = ExecProcess (fun info ->  
       info.FileName <- "rsync"
       info.Arguments <- args) System.TimeSpan.MaxValue
               
    if result <> 0 then 
        failwithf "Error during rsync From: %s To: %s - exit code: %d" source destination result