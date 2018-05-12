//// Contains a task which can be used to run [StyleCop](https://github.com/StyleCop/StyleCop) on .NET source files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.StyleCopHelper

open System
open System.Text
open Fake
open VSFile
open VSFile.Project
open VSFile.Source
open StyleCop

/// Type to define the behavior of how StyleCop must react on violations
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type StyleCopErrorLevel = 
    | Fail 
    | Warning

/// Parameter type for the StyleCop tool
[<CLIMutable>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type StyleCopParams =
    { ConfigurationFlags : List<string>
      OutputFile : string
      ErrorLevel : StyleCopErrorLevel
      RecursiveSearch : bool
      SettingsFile : string
      SourceFiles : List<string>
      ProjectFiles : List<string>
      SolutionFiles : List<string> }

/// StyleCop default parameters
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let StyleCopDefaults = 
    { ConfigurationFlags = List.Empty
      OutputFile = "StyleCopViolations.xml"
      ErrorLevel = Warning
      RecursiveSearch = true
      SettingsFile = null
      SourceFiles = List.Empty
      ProjectFiles = List.Empty
      SolutionFiles = List.Empty }

/// Runs the StyleCop tool, using the listed source, project and solution files.
///
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the StyleCop default parameters.
///
/// ## Sample
///
///     StyleCop (fun p -> { p with 
///                     SolutionFiles = [ artifactsDir @@ "MySolution.sln" ] }) 
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let StyleCop (setParams : StyleCopParams -> StyleCopParams) =
    let param = setParams StyleCopDefaults

    use __ = traceStartTaskUsing "StyleCop" ""
    
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

    /// Start analysing
    let ok = analyser.Start(Collections.Generic.List<CodeProject>(codeProjects), true)
    if not ok then failwith "StyleCop test failed"

    /// Inform user with any violations
    let userMessage = "StyleCop has some violations!"
    match param.ErrorLevel with
    | Fail -> failwith userMessage
    | _ -> traceImportant userMessage
