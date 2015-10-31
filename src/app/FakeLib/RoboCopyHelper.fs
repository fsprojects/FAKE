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

    let result = ExecProcess (fun info ->
       info.FileName <- "CMD.exe"
       info.Arguments <- args) System.TimeSpan.MaxValue
               
    if result <> 0 then failwithf "Error during RoboCopy From: %s To: %s" source destination

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
