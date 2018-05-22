/// Contains helper functions which allow to interact with the F# Interactive.
[<RequireQualifiedAccess>]
module Fake.DotNet.Fsi.Exe

open System
open System.Threading
open Fake.Core
open Fake.DotNet
open Fake.Tools

let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\10.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.0\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

/// The path to the F# Interactive tool.
let fsiPath =
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

type FsiArgs =
    FsiArgs of string list * string * string list with
    static member Parse (args:string array) =
        //Find first arg that does not start with - (as these are fsi options that precede the fsx).
        match args |> Array.tryFindIndex (fun arg -> not <| arg.StartsWith("-") ) with
        | Some(i) ->
            let fsxPath = args.[i]
            if fsxPath.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase) then
                let fsiOpts = if i > 0 then args.[0..i-1] else [||]
                let scriptArgs = if args.Length > (i+1) then args.[i+1..] else [||]
                Choice1Of2(FsiArgs(fsiOpts |> List.ofArray, fsxPath, scriptArgs |> List.ofArray))
            else Choice2Of2(sprintf "Expected argument %s to be the build script path, but it does not have the .fsx extension." fsxPath)
        | None -> Choice2Of2("Unable to locate the build script path.")

let private fsiStartInfo workingDirectory (FsiArgs(fsiOptions, scriptPath, scriptArgs)) environmentVars =
    let environmentVars' = 
        [
            ("MSBuild", MSBuild.msBuildExe)
            ("GIT", Git.CommandHelper.gitPath)
            ("FSI", fsiPath )
        ]
        |> Seq.append environmentVars

    (fun (info: ProcStartInfo) ->
        { info with 
            FileName = fsiPath
            Arguments = String.concat " " (fsiOptions @ [scriptPath] @ scriptArgs)
            WorkingDirectory = workingDirectory
        }.WithEnvironmentVariables environmentVars'
    )

/// Creates a ProcessStartInfo which is configured to the F# Interactive.
let private getFsiStartInfo workingDirectory extraFsiArgs script scriptArgs env info = 
    fsiStartInfo 
        workingDirectory 
        (FsiArgs(extraFsiArgs, script, scriptArgs |> List.ofArray)) 
        env info

/// Run the given build script with fsi.exe and allows for extra arguments to FSI and to the script. Returns output
let executeFSIRaw workingDirectory extraFsiArgs script scriptArgs env = 
    let r = 
        Process.execWithResult
            (getFsiStartInfo workingDirectory extraFsiArgs script scriptArgs env)
            TimeSpan.MaxValue
    Thread.Sleep 1000
    (r.ExitCode, r.Messages)  

/// Run the given buildscript with fsi.exe
let executeFSI workingDirectory script env = 
    executeFSIRaw workingDirectory [] script [||] env

/// Run the given build script with fsi.exe and allows for extra arguments to FSI.
let executeFSIWithArgs workingDirectory script extraFsiArgs env = 
    let result, _ = executeFSIRaw workingDirectory extraFsiArgs script [||] env
    result = 0

/// Run the given build script with fsi.exe and allows for extra arguments to FSI. Returns output.
let executeFSIWithArgsAndReturnMessages workingDirectory script extraFsiArgs env =
    executeFSIRaw workingDirectory extraFsiArgs script [||] env      

/// Run the given build script with fsi.exe and allows for extra arguments to the script. Returns output.
let executeFSIWithScriptArgsAndReturnMessages script (scriptArgs: string[]) =
    executeFSIRaw "" [] script scriptArgs []
