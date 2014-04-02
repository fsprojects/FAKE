/// Contains code to call the typescript compiler
module Fake.TypeScript

open System
open System.Text

type ECMAScript =
    | ES3
    | ES5

type ModuleGeneration =
    | CommonJs
    | AMD

type TypeScriptParams = {
    ECMAScript : ECMAScript;
    EmitComments : bool;
    OutputSingleFile: string option;
    EmitDeclarations : bool;
    ModuleGeneration : ModuleGeneration;
    EmitSourceMaps : bool;
    NoLib : bool;
    ToolPath : string
    TimeOut : TimeSpan
}

let private TypeScriptCompilerPath =  @"[ProgramFilesX86]\Microsoft SDKs\TypeScript\0.9\;[ProgramFiles]\Microsoft SDKs\TypeScript\0.9\" 

let typeScriptCompilerPath = 
    if isUnix then
        "tsc"
    else
        findPath "TypeScriptPath" TypeScriptCompilerPath "tsc.exe"

let TypeScriptDefaultParams = {
    ECMAScript = ES3;
    EmitComments = false;
    OutputSingleFile = None;
    EmitDeclarations = false;
    ModuleGeneration = CommonJs;
    EmitSourceMaps = false;
    NoLib = false;
    ToolPath = typeScriptCompilerPath
    TimeOut = TimeSpan.FromMinutes 5.
}

let private typeScriptCompilerProcess fileName timeout arguments  =
    let p = (fun (info:Diagnostics.ProcessStartInfo)-> 
        info.FileName <- fileName
        info.Arguments <- arguments )
    ExecProcessAndReturnMessages p timeout

let private buildArguments parameters file =
    let version = match parameters.ECMAScript with
                    | ES3 -> "ES3"
                    | ES5 -> "ES5"    
                    
    let moduleGeneration = match parameters.ModuleGeneration with
                            | CommonJs -> "commonjs"
                            | AMD -> "amd"

    let args =  
        new StringBuilder()
        |> appendWithoutQuotes          (" --target " + version)
        |> appendIfTrueWithoutQuotes    parameters.EmitComments " --c"
        |> appendIfSome                 parameters.OutputSingleFile (fun s -> sprintf " --out %s" s)
        |> appendIfTrueWithoutQuotes    parameters.EmitDeclarations " --declarations"
        |> appendWithoutQuotes          (" --module " + moduleGeneration)
        |> appendIfTrueWithoutQuotes    parameters.EmitSourceMaps " -sourcemap"
        |> appendIfTrueWithoutQuotes    parameters.NoLib " --nolib"
        |> appendWithoutQuotes          " "
        |> append                       file
        
    args.ToString()

/// This task to can be used to call the [TypeScript](http://www.typescriptlang.org/) compiler.
/// ## Parameters
///
///  - `parameters` - The type script compiler flags.
///  - `files` - The type script files to compile.
///
/// ## Sample
///
///         !! "src/**/*.ts"
///             |> TypeScriptCompiler { p with Timout = TimeSpan.MaxValue }
let TypeScriptCompiler setParams files = 
    traceStartTask "TypeScript" ""
    let parameters = setParams TypeScriptDefaultParams

    let callResults =
        files
        |> Seq.map (buildArguments parameters)
        |> Seq.map (typeScriptCompilerProcess parameters.ToolPath parameters.TimeOut)

    let errors = Seq.collect (fun x -> x.Errors) callResults
    if errors |> Seq.isEmpty |> not then Seq.iter traceError errors
    Seq.collect (fun x -> x.Messages) callResults |> Seq.iter trace

    traceEndTask "TypeScript" "" 