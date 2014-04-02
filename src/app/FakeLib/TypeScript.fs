[<AutoOpen>]
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
} 

let defaultParams = {
    ECMAScript = ES3;
    EmitComments = false;
    OutputSingleFile = None;
    EmitDeclarations = false;
    ModuleGeneration = CommonJs;
    EmitSourceMaps = false;
    NoLib = false;
}

let private TypeScriptCompilerPath =  @"[ProgramFilesX86]\Microsoft SDKs\TypeScript\0.9\;[ProgramFiles]\Microsoft SDKs\TypeScript\0.9\"

let typeScriptCompilerPath = 
    if isUnix then
        "tsc"
    else
        findPath "TypeScriptPath" TypeScriptCompilerPath "tsc.exe"   


let private typeScriptCompilerProcess arguments  =
    let p = (fun (info:Diagnostics.ProcessStartInfo)-> 
        info.FileName <- typeScriptCompilerPath
        info.Arguments <- arguments )
    ExecProcessAndReturnMessages p TimeSpan.MaxValue

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
        |> appendIfTrue                 parameters.EmitSourceMaps " -sourcemap"
        |> appendIfTrue                 parameters.NoLib " --nolib"
        |> appendWithoutQuotes          " "
        |> append                       file
        
    args.ToString()

let TypeScriptCompiler parameters files = 
    let callResults =
        files
        |> Seq.map (buildArguments parameters)
        |> Seq.map typeScriptCompilerProcess

    let errors = Seq.collect (fun x -> x.Errors) callResults
    if errors |> Seq.isEmpty |> not then Seq.iter traceError errors
    Seq.collect (fun x -> x.Messages) callResults

let TypeScriptCompilerDefault files = TypeScriptCompiler defaultParams files