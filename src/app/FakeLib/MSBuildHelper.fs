[<AutoOpen>]
module Fake.MSBuildHelper

open System
open System.Collections.Generic
open System.Text
open System.IO
open System.Configuration

/// MSBuild exe fileName
let msBuildExe =   
    let ev = environVar "MSBuild"
    if not (isNullOrEmpty ev) then ev else
        if "true".Equals(ConfigurationManager.AppSettings.["IgnoreMSBuild"],StringComparison.OrdinalIgnoreCase) then 
            String.Empty 
        else 
            findPath "MSBuildPath" "MSBuild.exe"

/// Runs a msbuild project
let build outputPath targets properties overwrite project =
    traceStartTask "MSBuild" project
    let targetsA = sprintf "/target:%s" targets |> toParam
    let output = 
        if isNullOrEmpty outputPath then "" else
        let outputDir = new DirectoryInfo(outputPath)
    
        sprintf "/p:OutputPath=\"%s\"\\" <| outputDir.FullName.Trim('\\')    

    let logger = sprintf " /logger:MSBUILDLogger,\"%s\\FakeLib.dll\"" fakePath
    let props = 
      properties
        |> Seq.fold (fun acc (key,value) -> sprintf "%s /p:%s=%s " acc key value) ""
 
    let args = toParam project + targetsA + props + output
    logfn "Building project: %s\n  %s %s" project msBuildExe args
    if not (execProcess3 (fun info ->  
        info.FileName <- msBuildExe
        info.Arguments <- args))
    then failwithf "Building %s project failed." project      
    traceEndTask "MSBuild" project

/// Builds the given project files and collects the output files
let MSBuild outputPath targets properties projects =      
    projects |> Seq.iter (build outputPath targets properties true)
    !+ (outputPath + "/**/*.*") 
      |> Scan   

/// Builds the given project files and collects the output files
let MSBuildDebug outputPath targets = MSBuild outputPath targets ["Configuration","Debug"]

/// Builds the given project files and collects the output files
let MSBuildRelease outputPath targets = MSBuild outputPath targets ["Configuration","Release"]