[<AutoOpen>]
/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.FSIHelper

open System
open System.IO
open System.Linq
open System.Diagnostics
open System.Threading

let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

/// The path to the F# Interactive tool.
let fsiPath =
    let ev = environVar "FSI"
    if not (isNullOrEmpty ev) then ev else
    if isUnix then
        let paths = appSettings "FSIPath" FSIPath
        // The standard name on *nix is "fsharpi"
        match tryFindFile paths "fsharpi" with
        | Some file -> file
        | None -> 
        // The early F# 2.0 name on *nix was "fsi"
        match tryFindFile paths "fsi" with
        | Some file -> file
        | None -> "fsharpi"
    else
        let dir = Path.GetDirectoryName fullAssemblyPath
        let fi = fileInfo (Path.Combine(dir, "fsi.exe"))
        if fi.Exists then fi.FullName else
        findPath "FSIPath" FSIPath "fsi.exe"

let private FsiStartInfo script workingDirectory extraFsiArgs args =
    (fun (info: ProcessStartInfo) ->
        info.FileName <- fsiPath
        info.Arguments <- String.concat " " (extraFsiArgs @ [script])
        info.WorkingDirectory <- workingDirectory
        let setVar k v =
            info.EnvironmentVariables.[k] <- v
        for (k, v) in args do
            setVar k v
        setVar "MSBuild"  msBuildExe
        setVar "GIT" Git.CommandHelper.gitPath
        setVar "FSI" fsiPath)

/// Creates a ProcessStartInfo which is configured to the F# Interactive.
let fsiStartInfo script workingDirectory args info =
    FsiStartInfo script workingDirectory [] args info

/// Run the given buildscript with fsi.exe
let executeFSI workingDirectory script args =
    let (result, messages) =
        ExecProcessRedirected
            (fsiStartInfo script workingDirectory args)
            TimeSpan.MaxValue
    Thread.Sleep 1000
    (result, messages)

/// Run the given build script with fsi.exe and allows for extra arguments to FSI.
let executeFSIWithArgs workingDirectory script extraFsiArgs args =
    let result = ExecProcess (FsiStartInfo script workingDirectory extraFsiArgs args) TimeSpan.MaxValue
    Thread.Sleep 1000
    result = 0

/// Run the given buildscript with fsi.exe at the given working directory.
let runBuildScriptAt workingDirectory printDetails script extraFsiArgs args =
    if printDetails then traceFAKE "Running Buildscript: %s" script
    let result = ExecProcess (FsiStartInfo script workingDirectory extraFsiArgs args) System.TimeSpan.MaxValue
    Thread.Sleep 1000
    result = 0

/// Run the given buildscript with fsi.exe
let runBuildScript printDetails script extraFsiArgs args =
    runBuildScriptAt "" printDetails script extraFsiArgs args
