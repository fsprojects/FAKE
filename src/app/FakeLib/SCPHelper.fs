[<AutoOpen>]
module Fake.SCPHelper

/// <summary>Performs a SCP copy.</summary>
/// <param name="source">The source directory (fileName)</param>
/// <param name="destination">The target directory (fileName)</param>
let SCP scpTool source destination =
    tracefn "SCP %s %s" source destination
    
    let args = sprintf "-r \".\" %s" (destination |> toParam)

    tracefn "%s %s" scpTool args
    let result = 
        ExecProcess (fun info ->
            info.FileName <- scpTool
            info.WorkingDirectory <- source |> FullName
            info.Arguments <- args) System.TimeSpan.MaxValue
               
    if result <> 0 then failwithf "Error during SCP From: %s To: %s" source destination