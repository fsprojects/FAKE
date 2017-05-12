//// Contains a task which can be used to run [StyleCop](https://github.com/StyleCop/StyleCop) on .NET source files.
module Fake.StyleCopHelper

open System
open System.Text
open Fake
open VSFile
open VSFile.Project
open VSFile.Source
open StyleCop

/// Type to define the behavior of how StyleCop must react on violations
type StyleCopErrorLevel = 
    | Fail 
    | Warning

/// Parameter type for the StyleCop tool
[<CLIMutable>]
type StyleCopParams =
    { ConfigurationFlags : List<string>
      OutputFile : string
      ErrorLevel : StyleCopErrorLevel
      RecursiveSearch : bool
      SettingsFile : string }

/// StyleCop default parameters
let StyleCopDefaults = 
    { ConfigurationFlags = List.Empty
      OutputFile = "StyleCopViolations.xml"
      ErrorLevel = Warning
      RecursiveSearch = true
      SettingsFile = null }

/// Runs the StyleCop tool, using the listed source, project and solution files.
///
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the StyleCop default parameters.
///
/// ## Sample
///
///     !! "**/*.cs"
///         |> StyleCop (fun p -> { p with OutputFile = "violations.xml" }) 
let StyleCop (setParams : StyleCopParams -> StyleCopParams) (sourceFiles : string seq) =
    let param = setParams StyleCopDefaults
    let analyser = StyleCopConsole(param.SettingsFile, true, param.OutputFile, null, true)
    let config = Configuration(Array.ofList param.ConfigurationFlags)

    analyser.OutputGenerated.AddHandler(fun _ e -> trace e.Output)

    let counter() = 
        let x = ref 0
        let increment() = x := !x + 1; !x
        increment

    let nextId = counter()

    /// Create Code Project with a given id and source path
    let createCodeProject path = CodeProject(nextId(), path, config)

    /// Add the source file to the analyser for futher handling
    let addSourceFile (codeProject : CodeProject) sourceFile =
        analyser.Core.Environment.AddSourceCode(codeProject, sourceFile, null) |> ignore
    
    let rec addSourceFiles files =
        match files with
        | [] -> []
        | head :: tail ->
            let project = createCodeProject head
            addSourceFile project head
            project :: addSourceFiles tail

    let codeProjects = 
        /// Add all the C# source files as 'CodeProjects'
        addSourceFiles (List.ofSeq sourceFiles)

    /// Start analysing
    let ok = analyser.Start(Collections.Generic.List<CodeProject>(codeProjects), true)
    if not ok then failwith "StyleCop test failed"

    /// Inform user with any violations
    let userMessage = "StyleCop has some violations!"
    match param.ErrorLevel with
    | Fail -> failwith userMessage
    | _ -> traceImportant userMessage