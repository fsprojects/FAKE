[<AutoOpen>]
//// Contains a task which can be used to run [StyleCop](http:///todo) on .NET source files.
module Fake.StyleCopHelper

open System
open System.Text
open Fake
open VSFile
open VSFile.Project
open VSFile.Source
open StyleCop

/// Parameter type for the StyleCop tool
[<CLIMutable>]
type StyleCopParams =
    { ConfigurationFlags : List<string>
      OutputFile : string
      RecursiveSearch : bool
      SettingsFile : string
      SourceFiles : List<string>
      ProjectFiles : List<string>
      SolutionFiles : List<string> }

/// StyleCop default parameters
let StyleCopDefaults = 
    { ConfigurationFlags = List.Empty
      OutputFile = "StyleCopViolations.xml"
      RecursiveSearch = true
      SettingsFile = "Settings.StyleCop"
      SourceFiles = List.Empty
      ProjectFiles = List.Empty
      SolutionFiles = List.Empty }

/// Run StyleCop with the given arguments
let StyleCop (setParams : StyleCopParams -> StyleCopParams) =
    let param = setParams StyleCopDefaults
    let analyser = StyleCopConsole(param.SettingsFile, true, param.OutputFile, null, true)
    let config = Configuration(Array.ofList param.ConfigurationFlags)

    analyser.OutputGenerated.AddHandler(fun _ e -> trace e.Output)

    /// Create Code Project with a given id and source path
    let createCodeProject id path = CodeProject(id, path, config)

    /// Create Visual Studio files from the given source paths
    let visualStudioFiles sourcePaths = VisualStudioFiles(Seq.ofList sourcePaths, param.RecursiveSearch)

    /// Add the source file to the analyser for futher handling
    let addSourceFile (codeProject : CodeProject) (sourceFile : CSharpSourceFile) =
        sourceFile.Load()
        analyser.Core.Environment.AddSourceCode(codeProject, sourceFile.FilePath, null) |> ignore

    let rec combineCodeProjectFromFiles id files func =
        match files with
        | [] -> []
        | head :: tail ->
            let nextId = id + 1
            let project = func nextId head
            project :: combineCodeProjectFromFiles nextId tail func

    /// Make 'CodeProject' files from source files
    let rec addSourceFiles id (files : List<CSharpSourceFile>) =
        combineCodeProjectFromFiles id files 
            (fun id head -> 
                let project = createCodeProject id head.DirectoryPath
                addSourceFile project head
                project)
    
    /// Make 'CodeProject' files from project files
    let rec addProjectFiles id (files : List<CSharpProjectFile>) =
        combineCodeProjectFromFiles id files 
            (fun id head ->
                let project = createCodeProject id head.DirectoryPath
                head.Load()
                head.SourceFiles |> Seq.iter (addSourceFile project)
                project)

    /// Make 'CodeProject' files from solution files
    let rec addSolutionFiles id (files : List<SolutionFile>) =
        files |> List.collect (fun f -> f.Load(); addProjectFiles id (List.ofSeq f.CSharpProjectFiles)) 

    let sourceFiles = visualStudioFiles param.SourceFiles
    let codeProjects = 
        /// 1. Add all the C# source files as 'CodeProjects'
        addSourceFiles 0 (List.ofSeq sourceFiles.CSharpSourceFiles)
            
            /// 2. Add all the C# project files as 'CodeProjects'
            |> (fun projects -> 
                let projectFiles = visualStudioFiles param.ProjectFiles
                let codeProjectsFromProjects = addProjectFiles (List.length projects) (List.ofSeq projectFiles.CSharpProjectFiles)
                List.append projects codeProjectsFromProjects)
            
            /// 3. Add all the C# solution files as 'CodeProjects'
            |> (fun projects ->
                let solutionFiles = visualStudioFiles param.SolutionFiles
                let codeProjectsFromSolutions = addSolutionFiles (List.length projects) (List.ofSeq solutionFiles.SolutionFiles)
                List.append projects codeProjectsFromSolutions)

    let ok = analyser.Start(System.Collections.Generic.List<CodeProject>(codeProjects), true)
    if not ok then failwith "StyleCop test failed"