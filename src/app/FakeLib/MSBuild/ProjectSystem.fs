/// Contains project file comparion tools for MSBuild project files.
module Fake.MSBuild.ProjectSystem

open Fake
open System
open System.Collections.Generic
open System.Xml
open System.Xml.Linq
open XMLHelper

/// A small abstraction over MSBuild project files.
type ProjectFile(projectFileName:string,documentContent : string) =
    let document = XMLDoc documentContent

    let nsmgr = 
        let nsmgr = new XmlNamespaceManager(document.NameTable)
        nsmgr.AddNamespace("default", document.DocumentElement.NamespaceURI)
        nsmgr

    let compileNodesXPath = "/default:Project/default:ItemGroup/default:Compile"
    let getCompileNodes (document:XmlDocument) =         
        [for node in document.SelectNodes(compileNodesXPath,nsmgr) -> node]

    let getFileAttribute (node:XmlNode) = node.Attributes.["Include"].InnerText

    /// Read a Project from a FileName
    static member FromFile(projectFileName) = new ProjectFile(projectFileName,ReadFileAsString projectFileName)

    /// Saves the project file
    member x.Save(?fileName) = document.Save(defaultArg fileName projectFileName)

    /// Add a file to the Compile nodes
    member x.AddFile fileName =        
        let document = XMLDoc documentContent // we create a copy and work immutable
        let node = getCompileNodes document |> Seq.last
        let newNode = node.CloneNode(false) :?> XmlElement
        newNode.SetAttribute("Include",fileName)
        
        node.ParentNode.AppendChild(newNode) |> ignore
        new ProjectFile(projectFileName,document.OuterXml)

    /// Removes a file from the Compile nodes
    member x.RemoveFile fileName =        
        let document = XMLDoc documentContent // we create a copy and work immutable
        let node = 
            getCompileNodes document 
            |> List.filter (fun node -> getFileAttribute node = fileName) 
            |> Seq.last  // we remove the last one to make easier to remove duplicates

        node.ParentNode.RemoveChild node |> ignore

        new ProjectFile(projectFileName,document.OuterXml)

    /// All files which are in "Compile" sections
    member x.Files = getCompileNodes document |> List.map getFileAttribute

    /// Finds duplicate files which are in "Compile" sections
    member this.FindDuplicateFiles() = 
        [let dict = Dictionary()
         for file in this.Files do
            match dict.TryGetValue file with
            | false,_    -> dict.[file] <- false            // first observance
            | true,false -> dict.[file] <- true; yield file // second observance
            | true,true  -> ()                              // already seen at least twice
        ]

    member x.RemoveDuplicates() =
        x.FindDuplicateFiles()
        |> List.fold (fun (project:ProjectFile) duplicate -> project.RemoveFile duplicate) x

    /// The project file name
    member x.ProjectFileName = projectFileName

/// Result type for project comparisons.
type ProjectComparison = 
    { TemplateProjectFileName: string
      ProjectFileName: string
      MissingFiles: string seq
      DuplicateFiles: string seq
      UnorderedFiles: string seq }   
    
      member this.HasErrors = 
        not (Seq.isEmpty this.MissingFiles && 
             Seq.isEmpty this.UnorderedFiles && 
             Seq.isEmpty this.DuplicateFiles)

/// Compares the given project files againts the template project and returns which files are missing.
/// For F# projects it is also reporting unordered files.
let findMissingFiles templateProject projects =
    let isFSharpProject file = file |> endsWith ".fsproj"

    let templateFiles = (ProjectFile.FromFile templateProject).Files
    let templateFilesSet = Set.ofSeq templateFiles
    
    projects
    |> Seq.map (fun fileName -> ProjectFile.FromFile fileName)
    |> Seq.map (fun ps ->             
            let missingFiles = Set.difference templateFilesSet (Set.ofSeq ps.Files)
                
            let unorderedFiles =
                if not <| isFSharpProject templateProject then [] else
                if not <| Seq.isEmpty missingFiles then [] else
                let remainingFiles = ps.Files |> List.filter (fun file -> Set.contains file templateFilesSet)
                if remainingFiles.Length <> templateFiles.Length then [] else

                templateFiles 
                |> List.zip remainingFiles
                |> List.filter (fun (a,b) -> a <> b) 
                |> List.map fst

            { TemplateProjectFileName = templateProject
              ProjectFileName = ps.ProjectFileName
              MissingFiles = missingFiles
              DuplicateFiles = ps.FindDuplicateFiles()
              UnorderedFiles = unorderedFiles })
    |> Seq.filter (fun pc -> pc.HasErrors)

/// Compares the given projects to the template project and adds all missing files to the projects if needed.
let FixMissingFiles templateProject projects =
    let addMissing (project:ProjectFile) missingFile = 
        tracefn "Adding %s to %s" missingFile project.ProjectFileName
        project.AddFile missingFile

    findMissingFiles templateProject projects
    |> Seq.iter (fun pc -> 
            let project = ProjectFile.FromFile pc.ProjectFileName
            if not (Seq.isEmpty pc.MissingFiles) then
                let newProject = Seq.fold addMissing project pc.MissingFiles
                newProject.Save())

/// It removes duplicate files from the project files.
let RemoveDuplicateFiles projects =    
    projects
    |> Seq.iter (fun fileName ->
            let project = ProjectFile.FromFile fileName
            if not (project.FindDuplicateFiles().IsEmpty) then
                let newProject = project.RemoveDuplicates()
                newProject.Save())

/// Compares the given projects to the template project and adds all missing files to the projects if needed.
/// It also removes duplicate files from the project files.
let FixProjectFiles templateProject projects =
    FixMissingFiles templateProject projects
    RemoveDuplicateFiles projects

/// Compares the given project files againts the template project and fails if any files are missing.
/// For F# projects it is also reporting unordered files.
let CompareProjectsTo templateProject projects =
    let errors =
        findMissingFiles templateProject projects
        |> Seq.map (fun pc -> 
                seq {
                    if Seq.isEmpty pc.MissingFiles |> not then
                        yield sprintf "Missing files in %s:\r\n%s" pc.ProjectFileName (toLines pc.MissingFiles)
                    if Seq.isEmpty pc.UnorderedFiles |> not then
                        yield sprintf "Unordered files in %s:\r\n%s" pc.ProjectFileName (toLines pc.UnorderedFiles)
                    if Seq.isEmpty pc.DuplicateFiles |> not then
                        yield sprintf "Duplicate files in %s:\r\n%s" pc.ProjectFileName (toLines pc.DuplicateFiles)}
                    |> toLines)
        |> toLines

    if isNotNullOrEmpty errors then
        failwith errors
        
let removeCompileNodesWithMissingFiles includeExistsF (project:ProjectFile) =
    let projectDir = IO.Path.GetDirectoryName(project.ProjectFileName)
    let missingFiles =
        seq { for filePath in project.Files do
                // We have to normalize the path, because csproj can have win style directory separator char on Mono too
                // Xbuild handles them, so we do too http://www.mono-project.com/archived/porting_msbuild_projects_to_xbuild/#paths 
                let includePath = Globbing.normalizePath (IO.Path.Combine([|projectDir; filePath|]))
                if not (includeExistsF(includePath)) then yield filePath }
    missingFiles
    |> Seq.fold (fun (project:ProjectFile) file -> project.RemoveFile(file)) project

/// Removes projects Compile nodes that have Include attributes pointing to files missing from the file system.  Saves updated projects.
let RemoveCompileNodesWithMissingFiles project =
    let newProject = removeCompileNodesWithMissingFiles System.IO.File.Exists (ProjectFile.FromFile project)
    newProject.Save()



         
