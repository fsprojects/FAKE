[<AutoOpen>]
module Fake.XCopyHelper

/// <summary>Performs a XCopy.</summary>
/// <param name="source">The source directory (fileName)</param>
/// <param name="destination">The target directory (fileName)</param>
let XCopy source destination =
    tracefn "XCopy %s %s" source destination
    
    let args =
          "/D /c XCOPY " +
            (source.TrimEnd('\\') |> FullName |> toParam) +
            (destination.TrimEnd('\\') |> FullName |> toParam) +
             "  /D /E /Y /I"

    tracefn " via: cmd.exe %s" args
    let result = ExecProcess (fun info ->  
       info.FileName <- "CMD.exe"
       info.Arguments <- args) System.TimeSpan.MaxValue
               
    if result <> 0 then failwithf "Error during XCopy From: %s To: %s" source destination