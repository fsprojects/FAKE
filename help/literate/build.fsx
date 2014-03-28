// Given a typical setup (with 'FSharp.Formatting' referenced using NuGet),
// the following will include binaries and load the literate script
#I "../lib/net40/"
#r "FSharp.Literate.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.MetadataFormat.dll"
open System.IO
open FSharp.Literate

// ----------------------------------------------------------------------------
// SETUP
// ----------------------------------------------------------------------------

/// Return path relative to the current file location
let relative subdir = Path.Combine(__SOURCE_DIRECTORY__, subdir)

// Create output directories & copy content files there
// (We have two sets of samples in "output" and "output-all" directories,
//  for simplicitly, this just creates them & copies content there)
if not (Directory.Exists(relative "/output")) then
  Directory.CreateDirectory(relative "/output") |> ignore
  Directory.CreateDirectory (relative "/output/content") |> ignore
if not (Directory.Exists(relative "/output-all")) then
  Directory.CreateDirectory(relative "/output-all") |> ignore
  Directory.CreateDirectory (relative "/output-all/content") |> ignore

for fileInfo in DirectoryInfo(relative "/../content").EnumerateFiles() do
  fileInfo.CopyTo(Path.Combine(relative "/output/content", fileInfo.Name)) |> ignore
  fileInfo.CopyTo(Path.Combine(relative "/output-all/content", fileInfo.Name)) |> ignore

// ----------------------------------------------------------------------------
// EXAMPLES
// ----------------------------------------------------------------------------

/// Processes a single F# Script file and produce HTML output
let processScriptAsHtml () =
  let file = relative "/demo.fsx"
  let output = relative "/output/demo-script.html"
  let template = relative "/templates/template-file.html"
  Literate.ProcessScriptFile(file, template, output)

/// Processes a single F# Script file and produce LaTeX output
let processScriptAsHtml () =
  let file = relative "/demo.fsx"
  let output = relative "/output/demo-script.html"
  let template = relative "/templates/template-color.tex"
  Literate.ProcessScriptFile(file, template, output, format = OutputKind.Latex)


/// Processes a single Markdown document and produce HTML output
let processDocument templateFile outputKind =
  let file = relative "/demo.md"
  let output = relative "/output/demo-markdown.html"
  let template = relative "/templates/template-file.html"
  Literate.ProcessMarkdown(file, template, output)

/// Processes a single Markdown document and produce LaTeX output
let processDocument templateFile outputKind =
  let file = relative "/demo.md"
  let output = relative "/output/demo-markdown.tex"
  let template = relative "/templates/template-color.tex"
  Literate.ProcessMarkdown(file, template, output, format = outputKind)


/// Processes an entire directory containing multiple script files 
/// (*.fsx) and Markdown documents (*.md) and it specifies additional 
/// replacements for the template file
let processDirectory() =
  let template = relative "/templates/template-project.html"
  let projInfo =
    [ "page-description", "F# Literate Programming"
      "page-author", "Tomas Petricek"
      "github-link", "https://github.com/tpetricek/FSharp.Formatting"
      "project-name", "F# Formatting" ]
  Literate.ProcessDirectory
    ( __SOURCE_DIRECTORY__, template, dir + "/output-all", 
      OutputKind.Html, replacements = projInfo)