/// Contains helper functions which allow to interact with the F# Interactive.
[<RequireQualifiedAccess>]
module Fake.DotNet.Fsi.Exe

open System
open System.IO
open System.Threading
open System.Text.RegularExpressions
open System.Xml.Linq
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.Tools

let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\10.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.0\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

let createDirectiveRegex id =
    Regex("^\s*#" + id + "\s*(@\"|\"\"\"|\")(?<path>.+?)(\"\"\"|\")", RegexOptions.Compiled ||| RegexOptions.Multiline)

let loadRegex = createDirectiveRegex "load"
let rAssemblyRegex = createDirectiveRegex "r"
let searchPathRegex = createDirectiveRegex "I"

let private extractDirectives (regex : Regex) scriptContents =
    regex.Matches scriptContents
    |> Seq.cast<Match>
    |> Seq.map(fun m -> m.Groups.Item("path").Value)


type Script = {
    Content : string
    Location : string
    SearchPaths : string seq
    IncludedAssemblies : Lazy<string seq>
}

let getAllScriptContents (pathsAndContents : seq<Script>) =
    pathsAndContents |> Seq.map(fun s -> s.Content)

let getIncludedAssembly scriptContents = extractDirectives rAssemblyRegex scriptContents

let getSearchPaths scriptContents = extractDirectives searchPathRegex scriptContents

let rec getAllScripts scriptPath : seq<Script> =
    let scriptContents = File.ReadAllText scriptPath
    let searchPaths = getSearchPaths scriptContents |> Seq.toList

    let loadedContents =
        extractDirectives loadRegex scriptContents
        |> Seq.collect (fun path ->
            if path.StartsWith ".fake" then
                Seq.empty
            else
                let path =
                    if Path.IsPathRooted path then
                        path
                    else
                        let pathMaybe =
                            ["./"] @ searchPaths
                            |> List.map(fun searchPath ->
                                if Path.IsPathRooted searchPath then
                                    Path.Combine(searchPath, path)
                                else
                                    Path.Combine(Path.GetDirectoryName scriptPath, searchPath, path))
                            |> List.tryFind File.Exists

                        match pathMaybe with
                        | None -> failwithf "Could not find script '%s' in any paths searched. Searched paths:\n%A" path searchPaths
                        | Some x -> x
                getAllScripts path
        )
    let s =
      { Location = scriptPath
        Content = scriptContents
        SearchPaths = searchPaths
        IncludedAssemblies = lazy(getIncludedAssembly scriptContents) }
    Seq.concat [List.toSeq [s]; loadedContents]
    

module internal Cache =
    let xname name = XName.Get(name)
    let create (loadedAssemblies : Reflection.Assembly seq) =
        let xelement name = XElement(xname name)
        let xattribute name value = XAttribute(xname name, value)

        let doc = XDocument()
        let root = xelement "FAKECache"
        doc.Add(root)
        let assemblies = xelement "Assemblies"
        root.Add(assemblies)

        let assemNodes =
            loadedAssemblies
            |> Seq.map(fun assem ->
                let ele = xelement "Assembly"
                ele.Add(xattribute "Location" assem.Location)
                ele.Add(xattribute "FullName" assem.FullName)
                ele.Add(xattribute "Version" (assem.GetName().Version.ToString()))
                ele)
            |> Seq.iter(assemblies.Add)
        doc

    type AssemblyInfo = {
        Location : string
        FullName : string
        Version : string
    }

    type CacheConfig = {
        Assemblies : AssemblyInfo seq
    }
    let read (path : string) : CacheConfig =
        let doc = XDocument.Load(path)
        //let root = doc.Descendants() |> Seq.exactlyOne
        let assembliesEle = doc.Descendants(xname "Assemblies") |> Seq.exactlyOne
        let assemblies =
            assembliesEle.Descendants()
            |> Seq.map(fun assemblyEle ->
                let get name = assemblyEle.Attribute(xname name).Value
                { Location = get "Location"
                  FullName = get "FullName"
                  Version = get "Version" })
        { Assemblies = assemblies }

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
