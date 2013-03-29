[<AutoOpen>]
module Fake.FSIHelper

open System
open System.IO
open System.Linq
open System.Diagnostics
open System.Threading

/// The Path to the F# interactive tool
let fsiPath =
    let ev = environVar "FSI"
    if not (isNullOrEmpty ev) then ev else
    if isUnix then
        let paths = appSettings "FSIPath"
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
        findPath "FSIPath" "fsi.exe"

type private Stage =
    | BootStage
    | RunStage

/// Computes extra command-line arguments to enable bootstrapping FAKE scripts.
let private BootArgs (stage: Stage) (script: string) : list<string> =
    let fakeDir = Path.GetDirectoryName(typeof<Stage>.Assembly.Location)
    let quote (s: string) : string =
        String.Format(@"""{0}""", s.Replace(@"""", @"\"""))
    [
        match stage with
        | BootStage -> yield "--define:BOOT"
        | RunStage -> ()
        yield "-I"
        yield quote fakeDir
        yield "-r"
        yield "FakeLib"
    ]

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

let fsiStartInfo script workingDirectory args =
    FsiStartInfo script workingDirectory [] args

/// Run the given buildscript with fsi.exe
let executeFSI workingDirectory script args =
    let (result, messages) =
        ExecProcessRedirected
            (fsiStartInfo script workingDirectory args)
            TimeSpan.MaxValue
    Thread.Sleep 1000
    (result, messages)

/// Run the given buildscript with fsi.exe
let runBuildScriptAt workingDirectory printDetails script args =
    let fullPath = Path.Combine(workingDirectory, script)
    let main (extraArgs: list<string>) : bool =
        if printDetails then traceFAKE "Running Buildscript: %s" script
        let result = ExecProcess (FsiStartInfo script workingDirectory extraArgs args) TimeSpan.MaxValue
        Thread.Sleep 1000
        result = 0
    if Fake.Boot.IsBootScript script then
        main (BootArgs BootStage script)
        && main (BootArgs RunStage script)
    else
        main []

let runBuildScript printDetails script args =
    runBuildScriptAt "" printDetails script args
