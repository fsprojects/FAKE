[<AutoOpen>]
module Fake.XCopyHelper

open System.IO
open System.Text

/// Performs a XCopy 
///  param source: The source directory (fileName)
///  param destination: The target directory (fileName)
let XCopy source destination =
    tracefn "XCopy %s %s" source destination
    
    let args =
          "/D /c XCOPY " +
            (source.TrimEnd('\\') |> FullName |> toParam) +
            (destination.TrimEnd('\\') |> FullName |> toParam) +
             "  /D /E /Y /I"

    tracefn " via: cmd.exe %s" args
    let result = ExecProcess (fun info ->  
       info.FileName <- "CMD.exe "
       info.Arguments <- args)
         
       
    if result <> 0 then failwithf "Error during XCopy From: %s To: %s" source destination