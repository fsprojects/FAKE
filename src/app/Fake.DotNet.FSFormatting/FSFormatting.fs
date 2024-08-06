/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
[<System.Obsolete("This module is deprecated. Please use fsdocs module instead")>]
module Fake.DotNet.FSFormatting

open System
open System.Diagnostics
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO.Globbing
open Fake.IO
open Fake.IO.FileSystemOperators

/// Specifies the fsformatting executable
let private defaultToolPath =
    lazy
        let toolDll =
            ProcessUtils.tryFindLocalTool "FSFORMATTING" "fsformatting.dll" [ "." ]

        match toolDll with
        | Some s when s.EndsWith ".dll" -> s, ToolType.CreateFrameworkDependentDeployment()
        | _ ->
            match ProcessUtils.tryFindLocalTool "FSFORMATTING" "fsformatting" [ "." ] with
            | Some s -> s, ToolType.Create()
            | None -> "fsformatting.exe", ToolType.Create()

/// Runs fsformatting.exe with the given command in the given repository directory.
let private run (toolType: ToolType) toolPath (command: string) =
    CreateProcess.fromRawCommandLine toolPath command
    // RawCommand (, command)
    //|> CreateProcess.fromCommand
    |> CreateProcess.withToolType (toolType.WithDefaultToolCommandName "dotnet-fsformatting")
    |> CreateProcess.withTimeout System.TimeSpan.MaxValue
    |> CreateProcess.ensureExitCodeWithMessage "FSharp.Formatting failed"
    |> Proc.run
    |> ignore

/// [omit]
type LiterateArguments =
    { ToolPath: string
      ToolType: ToolType
      Source: string
      OutputDirectory: string
      Template: string
      ProjectParameters: (string * string) list
      LayoutRoots: string list
      FsiEval: bool }

let private createDefaultLiterateArguments () =
    let toolPath, toolType = defaultToolPath.Value

    { ToolPath = toolPath
      ToolType = toolType
      Source = ""
      OutputDirectory = ""
      Template = ""
      ProjectParameters = []
      LayoutRoots = []
      FsiEval = false }

/// [omit]
[<Obsolete("This API is deprecated. Please use an alternative API from fsdocs module instead")>]
let createDocs p =
    let arguments =
        (p: LiterateArguments -> LiterateArguments) (createDefaultLiterateArguments ())

    let layoutroots =
        if arguments.LayoutRoots.IsEmpty then
            []
        else
            [ "--layoutRoots" ] @ arguments.LayoutRoots

    let source = arguments.Source
    let template = arguments.Template
    let outputDir = arguments.OutputDirectory
    let fsiEval = if arguments.FsiEval then [ "--fsieval" ] else []

    let command =
        arguments.ProjectParameters
        |> Seq.collect (fun (k, v) -> [ k; v ])
        |> Seq.append (
            [ "literate"; "--processdirectory" ]
            @ layoutroots
              @ [ "--inputdirectory"
                  source
                  "--templatefile"
                  template
                  "--outputDirectory"
                  outputDir ]
                @ fsiEval @ [ "--replacements" ]
        )
        |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
        |> String.separated " "

    run arguments.ToolType arguments.ToolPath command
    printfn "Successfully generated docs for %s" source

/// [omit]
type MetadataFormatArguments =
    { ToolPath: string
      ToolType: ToolType
      Source: string
      SourceRepository: string
      OutputDirectory: string
      Template: string
      ProjectParameters: (string * string) list
      LayoutRoots: string list
      LibDirs: string list }

let private createDefaultMetadataFormatArguments () =
    let toolPath, toolType = defaultToolPath.Value

    { ToolPath = toolPath
      ToolType = toolType
      Source = Directory.GetCurrentDirectory()
      SourceRepository = ""
      OutputDirectory = ""
      Template = ""
      ProjectParameters = []
      LayoutRoots = []
      LibDirs = [] }

/// [omit]
[<Obsolete("This API is deprecated. Please use an alternative API from fsdocs module instead")>]
let createDocsForDlls (p: MetadataFormatArguments -> MetadataFormatArguments) dllFiles =
    let arguments = p (createDefaultMetadataFormatArguments ())
    let outputDir = arguments.OutputDirectory
    let projectParameters = arguments.ProjectParameters
    let sourceRepo = arguments.SourceRepository

    let libdirs =
        if arguments.LibDirs.IsEmpty then
            []
        else
            [ "--libDirs" ] @ arguments.LibDirs

    let layoutroots =
        if arguments.LayoutRoots.IsEmpty then
            []
        else
            [ "--layoutRoots" ] @ arguments.LayoutRoots


    projectParameters
    |> Seq.collect (fun (k, v) -> [ k; v ])
    |> Seq.append (
        [ "metadataformat"; "--generate"; "--outdir"; outputDir ]
        @ layoutroots
          @ libdirs
            @ [ "--sourceRepo"
                sourceRepo
                "--sourceFolder"
                arguments.Source
                "--parameters" ]
    )
    |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
    |> String.separated " "
    |> fun prefix -> sprintf "%s --dllfiles %s" prefix (String.separated " " (dllFiles |> Seq.map (sprintf "\"%s\"")))
    |> run arguments.ToolType arguments.ToolPath


    printfn "Successfully generated docs for DLLs: %s" (String.separated ", " dllFiles)
