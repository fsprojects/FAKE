/// Contains helper functions which allow to interact with the F# Interactive.
[<RequireQualifiedAccess>]
module Fake.DotNet.Fsi.Exe

open System
open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.DotNet.Fsi.Params

let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\10.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.0\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

/// The path to the F# Interactive tool.
let private pathToFsiExe =
    let ev = Environment.environVar "FSI"
    if not (String.isNullOrEmpty ev) then ev else
    if Environment.isUnix then
        let paths = Process.appSettings "FSIPath" FSIPath
        // The standard name on *nix is "fsharpi"
        match Process.tryFindFile paths "fsharpi" with
        | Some file -> file
        | None ->
        // The early F# 2.0 name on *nix was "fsi"
        match Process.tryFindFile paths "fsi" with
        | Some file -> file
        | None -> "fsharpi"
    else
        // let dir = Path.GetDirectoryName fullAssemblyPath
        // let fi = FileInfo.ofPath (Path.Combine(dir, "fsi.exe"))
        // if fi.Exists then fi.FullName else
        Process.findPath "FSIPath" FSIPath "fsi.exe"

/// Gets the default environment variables and additionally appends user defined vars to it
let private defaultEnvironmentVars environmentVars = 
    [
        ("MSBuild", MSBuild.msBuildExe)
        ("GIT", Git.CommandHelper.gitPath)
        // ("FSI", findFsiPath )
    ]
    |> Seq.append environmentVars

/// Serializes arguments, putting script arguments after an empty "--" arg, which denotes the beginning of script arguments
let private serializeArgs (fsiParams: FsiParam list) script (scriptArgs: string list) = 
    let stringParams = List.map string fsiParams
    let args = 
        List.concat ([ stringParams; [script;"--"]; scriptArgs]) 
        |> List.toArray
        |> Arguments.OfArgs 
    args.ToWindowsCommandLine

/// Executes a user supplied Fsi.exe with the option to set args and environment variables
let execFsiRaw workingDirectory fsiExe fsiParams script scriptArgs environmentVars = 
    let args = serializeArgs fsiParams script scriptArgs
    let environmentVars' = defaultEnvironmentVars environmentVars

    Trace.trace <| sprintf "Executing FSI at %s with args %s" fsiExe args

    let r = Process.execWithResult (fun info -> 
            { info with
                FileName = fsiExe
                Arguments = args
                WorkingDirectory = workingDirectory
            }.WithEnvironmentVariables environmentVars' ) TimeSpan.MaxValue

    if r.ExitCode <> 0 then
        List.iter Trace.traceError r.Errors

    (r.ExitCode, r.Messages)

/// Locates Fsi.exe and executes 
let execFsiSimple fsiParams script scriptArgs = 
    execFsiRaw "" pathToFsiExe fsiParams script scriptArgs []
