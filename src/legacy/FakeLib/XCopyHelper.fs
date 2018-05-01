[<AutoOpen>]
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
/// Contains a task to use [XCOPY](http://en.wikipedia.org/wiki/XCOPY) on Windows.
module Fake.XCopyHelper

/// Executes a XCopy command
/// ## Parameters
///  - `source` - The source directory
///  - `destination` - The target directory
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let XCopy (source:string) (destination:string) =
    let args =
          "/D /c XCOPY " +
            (source.TrimEnd('\\') |> FullName |> toParam) +
            (destination.TrimEnd('\\') |> FullName |> toParam) +
             "  /D /E /Y /I"

    let result = ExecProcess (fun info ->  
       info.FileName <- "CMD.exe"
       info.Arguments <- args) System.TimeSpan.MaxValue
               
    if result <> 0 then failwithf "Error during XCopy From: %s To: %s" source destination
