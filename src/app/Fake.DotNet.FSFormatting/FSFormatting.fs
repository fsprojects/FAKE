/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
module Fake.DotNet.FSFormatting

open System
open System.Diagnostics
open System.IO
open Fake.Core
open Fake.IO.Globbing
open Fake.IO
open Fake.IO.FileSystemOperators

/// Specifies the fsformatting executable
let mutable toolPath =
    Tools.findToolInSubPath "fsformatting.exe" (Directory.GetCurrentDirectory() @@ "tools" @@ "FSharp.Formatting.CommandTool" @@ "tools")

/// Runs fsformatting.exe with the given command in the given repository directory.
let private run toolPath command = 
    if 0 <> Process.execSimple ((fun info ->
            { info with
                FileName = toolPath
                Arguments = command }) >> Process.withFramework) System.TimeSpan.MaxValue
    then failwithf "FSharp.Formatting %s failed." command

type LiterateArguments =
    { ToolPath : string
      Source : string
      OutputDirectory : string 
      Template : string
      ProjectParameters : (string * string) list
      LayoutRoots : string list 
      FsiEval : bool }

let defaultLiterateArguments =
    { ToolPath = toolPath
      Source = ""
      OutputDirectory = ""
      Template = ""
      ProjectParameters = []
      LayoutRoots = [] 
      FsiEval = false }

let createDocs p =
    let arguments = (p:LiterateArguments->LiterateArguments) defaultLiterateArguments
    let layoutroots =
        if arguments.LayoutRoots.IsEmpty then []
        else [ "--layoutRoots" ] @ arguments.LayoutRoots
    let source = arguments.Source
    let template = arguments.Template
    let outputDir = arguments.OutputDirectory
    let fsiEval = if arguments.FsiEval then [ "--fsieval" ] else []

    let command = 
        arguments.ProjectParameters
        |> Seq.map (fun (k, v) -> [ k; v ])
        |> Seq.concat
        |> Seq.append 
               (["literate"; "--processdirectory" ] @ layoutroots @ [ "--inputdirectory"; source; "--templatefile"; template; 
                  "--outputDirectory"; outputDir] @ fsiEval @ [ "--replacements" ])
        |> Seq.map (fun s -> 
               if s.StartsWith "\"" then s
               else sprintf "\"%s\"" s)
        |> String.separated " "
    run arguments.ToolPath command
    printfn "Successfully generated docs for %s" source

type MetadataFormatArguments =
    { ToolPath : string
      Source : string
      SourceRepository : string
      OutputDirectory : string 
      Template : string
      ProjectParameters : (string * string) list
      LayoutRoots : string list
      LibDirs : string list }

let defaultMetadataFormatArguments =
    { ToolPath = toolPath
      Source = Directory.GetCurrentDirectory()
      SourceRepository = ""
      OutputDirectory = ""
      Template = ""
      ProjectParameters = []
      LayoutRoots = []
      LibDirs = [] }

let createDocsForDlls (p:MetadataFormatArguments->MetadataFormatArguments) dllFiles = 
    let arguments = p defaultMetadataFormatArguments
    let outputDir = arguments.OutputDirectory
    let projectParameters = arguments.ProjectParameters
    let sourceRepo = arguments.SourceRepository
    let libdirs = 
        if arguments.LibDirs.IsEmpty then []
        else [ "--libDirs" ] @ arguments.LibDirs

    let layoutroots =
        if arguments.LayoutRoots.IsEmpty then []
        else [ "--layoutRoots" ] @ arguments.LayoutRoots

    
    projectParameters
    |> Seq.map (fun (k, v) -> [ k; v ])
    |> Seq.concat
    |> Seq.append 
            ([ "metadataformat"; "--generate"; "--outdir"; outputDir] @ layoutroots @ libdirs @ [ "--sourceRepo"; sourceRepo;
               "--sourceFolder"; arguments.Source; "--parameters" ])
    |> Seq.map (fun s -> 
            if s.StartsWith "\"" then s
            else sprintf "\"%s\"" s)
    |> String.separated " "
    |> fun prefix -> sprintf "%s --dllfiles %s" prefix (String.separated " " (dllFiles |> Seq.map (sprintf "\"%s\"")))
    |> run arguments.ToolPath

    
    printfn "Successfully generated docs for DLLs: %s" (String.separated ", " dllFiles)
