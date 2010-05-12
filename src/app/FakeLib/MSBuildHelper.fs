[<AutoOpen>]
module Fake.MSBuildHelper

open System
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
        outputPath
          |> FullName
          |> trimSeparator
          |> sprintf "/p:OutputPath=\"%s\"\\"
        
    let props = 
        properties
          |> Seq.map (fun (key,value) -> sprintf " /p:%s=%s " key value)
          |> separated ""
 
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