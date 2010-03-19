[<AutoOpen>]
module Fake.XCopyHelper

open System.IO
open System.Text

/// Performs a XCopy 
///  param source: The source directory (fileName)
///  param destination: The target directory (fileName)
let XCopy source destination =
  tracefn "XCopy %s %s" source destination
  let result = ExecProcess (fun info ->  
     info.FileName <- "CMD.exe "
     info.Arguments <- 
       "/D /c XCOPY " +
         (source.TrimEnd('\\') |> FullName |> toParam) +
         (destination.TrimEnd('\\') |> FullName |> toParam) +
         "  /D /E /Y /I")
         
  if result <> 0 then failwithf "Error during XCopy From: %s To: %s" source destination