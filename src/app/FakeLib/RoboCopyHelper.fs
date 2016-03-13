/// Contains a task to use [robocopy](https://en.wikipedia.org/wiki/Robocopy) on Windows.
module Fake.RoboCopyHelper

type RoboCopyOptions = { Mirror: bool }

/// Executes a RoboCopy command with options
/// ## Parameters
///  - `source` - The source directory
///  - `destination` - The target directory
///  - `options` - The options to pass to robocopy
let private roboCopyWithOptions (source:string) (destination:string) (options:RoboCopyOptions) =
    let args =
          "/D /c robocopy " +
            (source.TrimEnd('\\') |> FullName |> toParam) +
            (destination.TrimEnd('\\') |> FullName |> toParam) +
            if options.Mirror then " /MIR /IT"
            else " /IT"

    let exitcode = ExecProcess (fun info ->
       info.FileName <- "CMD.exe"
       info.Arguments <- args) System.TimeSpan.MaxValue
    
    // Exit codes from https://support.microsoft.com/en-us/kb/954404
    let exitCodeWithMessage = 
        match exitcode with
        | 0 -> (exitcode, "No files were copied. No failure was encountered. No files were mismatched. The files already exist in the destination directory; therefore, the copy operation was skipped.")
        | 1 -> (exitcode, "All files were copied successfully.")
        | 2 -> (exitcode, "There are some additional files in the destination directory that are not present in the source directory. No files were copied.")
        | 3 -> (exitcode, "Some files were copied. Additional files were present. No failure was encountered.")
        | 4 -> (exitcode, "Some Mismatched files or directories were detected. Examine the output log. Housekeeping might be required.")
        | 5 -> (exitcode, "Some files were copied. Some files were mismatched. No failure was encountered.")
        | 6 -> (exitcode, "Additional files and mismatched files exist. No files were copied and no failures were encountered. This means that the files already exist in the destination directory. ")
        | 7 -> (exitcode, "Files were copied, a file mismatch was present, and additional files were present.")
        | 8 -> (exitcode, "Several files did not copy.")
        | _ -> (exitcode, "UNKNOWN ERROR")

    match exitCodeWithMessage with
    | (exitcode, message) when exitcode < 2 -> tracefn "Succeeded in RoboCopy From: %s To: %s \n%s\n" source destination message |> ignore
    | (exitcode, message) when exitcode < 8 -> traceImportant <| sprintf "Important notice in RoboCopy From: %s To: %s \n%s\n" source destination message |> ignore
    | (_, _) -> failwithf "Error during RoboCopy From: %s To: %s" source destination

/// Executes a RoboCopy command
/// ## Parameters
///  - `source` - The source directory
///  - `destination` - The target directory
let RoboCopy (source:string) (destination:string) =
   roboCopyWithOptions source destination { Mirror = false }

/// Executes a RoboCopy mirror command (potentially destructive)
/// ## Parameters
///  - `source` - The source directory
///  - `destination` - The target directory
let RoboCopyMirror (source:string) (destination:string) =
    roboCopyWithOptions source destination { Mirror = true }
