/// Contains project file comparison tools for MSBuild project files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.MSBuild.ProjectSystem

open Fake
open System
open System.Collections.Generic
open System.Xml
open System.Xml.Linq
open XMLHelper

/// A small abstraction over MSBuild project files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ProjectFile(projectFileName:string,documentContent : string) =
    let document = XMLDoc documentContent

    let nsmgr = 
        let nsmgr = new XmlNamespaceManager(document.NameTable)
        nsmgr.AddNamespace("default", document.DocumentElement.NamespaceURI)
        nsmgr

    let compileNodesXPath = "/default:Project/default:ItemGroup/default:Compile"
    let getCompileNodes (document:XmlDocument) =         
        [for node in document.SelectNodes(compileNodesXPath,nsmgr) -> node]

    let contentNodesXPath = "/default:Project/default:ItemGroup/default:Content"
    let getContentNodes (document:XmlDocument) =         
        [for node in document.SelectNodes(contentNodesXPath,nsmgr) -> node]

    let getFileAttribute (node:XmlNode) = node.Attributes.["Include"].InnerText

    /// Read a Project from a FileName
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    static member FromFile(projectFileName) = new ProjectFile(projectFileName,ReadFileAsString projectFileName)

    /// Saves the project file
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Save(?fileName) = document.Save(defaultArg fileName projectFileName)

    /// Add a file to the Compile nodes
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.AddFile fileName =        
        let document = XMLDoc documentContent // we create a copy and work immutable
        let node = getCompileNodes document |> Seq.last
        let newNode = node.CloneNode(false) :?> XmlElement
        newNode.SetAttribute("Include",fileName)
        
        node.ParentNode.AppendChild(newNode) |> ignore
        new ProjectFile(projectFileName,document.OuterXml)
        
    /// Add a file to the Content nodes
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.AddContentFile fileName =        
        let document = XMLDoc documentContent // we create a copy and work immutable
        let node = getContentNodes document |> Seq.last
        let newNode = node.CloneNode(false) :?> XmlElement
        newNode.SetAttribute("Include",fileName)
        
        node.ParentNode.AppendChild(newNode) |> ignore
        new ProjectFile(projectFileName,document.OuterXml)

    /// Removes a file from the Compile nodes
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.RemoveFile fileName =        
        let document = XMLDoc documentContent // we create a copy and work immutable
        let node = 
            getCompileNodes document 
            |> List.filter (fun node -> getFileAttribute node = fileName) 
            |> Seq.last  // we remove the last one to make easier to remove duplicates

        node.ParentNode.RemoveChild node |> ignore

        new ProjectFile(projectFileName,document.OuterXml)

    /// Removes a file from the Content nodes
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.RemoveContentFile fileName =        
        let document = XMLDoc documentContent // we create a copy and work immutable
        let node = 
            getContentNodes document 
            |> List.filter (fun node -> getFileAttribute node = fileName) 
            |> Seq.last  // we remove the last one to make easier to remove duplicates

        node.ParentNode.RemoveChild node |> ignore

        new ProjectFile(projectFileName,document.OuterXml)

    /// All files which are in "Compile" sections
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.Files = getCompileNodes document |> List.map getFileAttribute
    
    /// All files which are in "Content" sections
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.ContentFiles = getContentNodes document |> List.map getFileAttribute

    /// Finds duplicate files which are in "Compile" sections
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.FindDuplicateFiles() = 
        [let dict = Dictionary()
         for file in this.Files do
            match dict.TryGetValue file with
            | false,_    -> dict.[file] <- false            // first observance
            | true,false -> dict.[file] <- true; yield file // second observance
            | true,true  -> ()                              // already seen at least twice
        ]
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
     member x.RemoveDuplicates() =
            x.FindDuplicateFiles()
            |> List.fold (fun (project:ProjectFile) duplicate -> project.RemoveFile duplicate) x


    /// Finds duplicate files which are in "Content" sections
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.FindDuplicateContentFiles() = 
        [let dict = Dictionary()
         for file in this.ContentFiles do
            match dict.TryGetValue file with
            | false,_    -> dict.[file] <- false            // first observance
            | true,false -> dict.[file] <- true; yield file // second observance
            | true,true  -> ()                              // already seen at least twice
        ]

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.RemoveDuplicatesContent() =
        x.FindDuplicateContentFiles()
        |> List.fold (fun (project:ProjectFile) duplicate -> project.RemoveContentFile duplicate) x

    /// The project file name
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member x.ProjectFileName = projectFileName

/// Result type for project comparisons.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ProjectComparison = 
    { TemplateProjectFileName: string
      ProjectFileName: string
      MissingFiles: string seq
      DuplicateFiles: string seq
      UnorderedFiles: string seq }   

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.HasErrors = 
        not (Seq.isEmpty this.MissingFiles && 
             Seq.isEmpty this.UnorderedFiles && 
             Seq.isEmpty this.DuplicateFiles)

/// Compares the given project files against the template project and returns which files are missing.
/// For F# projects it is also reporting unordered files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

/// Compares the given project files against the template project and returns which files are missing.
/// For F# projects it is also reporting unordered files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let findMissingContentFiles templateProject projects =
    let isFSharpProject file = file |> endsWith ".fsproj"

    let templateFiles = (ProjectFile.FromFile templateProject).ContentFiles
    let templateFilesSet = Set.ofSeq templateFiles
    
    projects
    |> Seq.map (fun fileName -> ProjectFile.FromFile fileName)
    |> Seq.map (fun ps ->             
            let missingFiles = Set.difference templateFilesSet (Set.ofSeq ps.ContentFiles)
                
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
              DuplicateFiles = ps.FindDuplicateContentFiles()
              UnorderedFiles = unorderedFiles })
    |> Seq.filter (fun pc -> pc.HasErrors)

/// Compares the given projects to the template project and adds all missing files to the projects if needed.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

/// Compares the given projects to the template project and adds all missing files to the projects if needed.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let FixMissingContentFiles templateProject projects =
    let addMissing (project:ProjectFile) missingFile = 
        tracefn "Adding %s to %s" missingFile project.ProjectFileName
        project.AddContentFile missingFile

    findMissingContentFiles templateProject projects
    |> Seq.iter (fun pc -> 
            let project = ProjectFile.FromFile pc.ProjectFileName
            if not (Seq.isEmpty pc.MissingFiles) then
                let newProject = Seq.fold addMissing project pc.MissingFiles
                newProject.Save())

/// It removes duplicate files from the project files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RemoveDuplicateFiles projects =    
    projects
    |> Seq.iter (fun fileName ->
            let project = ProjectFile.FromFile fileName
            if not (project.FindDuplicateFiles().IsEmpty) then
                let newProject = project.RemoveDuplicates()
                newProject.Save())

/// It removes duplicate content files from the project files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RemoveDuplicateContentFiles projects =    
    projects
    |> Seq.iter (fun fileName ->
            let project = ProjectFile.FromFile fileName
            if not (project.FindDuplicateContentFiles().IsEmpty) then
                let newProject = project.RemoveDuplicatesContent()
                newProject.Save())

/// Compares the given projects to the template project and adds all missing files to the projects if needed.
/// It also removes duplicate files from the project files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let FixProjectFiles templateProject projects =
    FixMissingFiles templateProject projects
    RemoveDuplicateFiles projects


/// Compares the given projects to the template project and adds all missing content files to the projects if needed.
/// It also removes duplicate files from the project files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let FixProjectContentFiles templateProject projects =
    FixMissingContentFiles templateProject projects
    RemoveDuplicateContentFiles projects

/// Compares the given project files against the template project and fails if any files are missing.
/// For F# projects it is also reporting unordered files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]        
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

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
let removeContentNodesWithMissingFiles includeExistsF (project:ProjectFile) =
    let projectDir = IO.Path.GetDirectoryName(project.ProjectFileName)
    let missingFiles =
        seq { for filePath in project.ContentFiles do
                // We have to normalize the path, because csproj can have win style directory separator char on Mono too
                // Xbuild handles them, so we do too http://www.mono-project.com/archived/porting_msbuild_projects_to_xbuild/#paths 
                let includePath = Globbing.normalizePath (IO.Path.Combine([|projectDir; filePath|]))
                if not (includeExistsF(includePath)) then yield filePath }
    missingFiles
    |> Seq.fold (fun (project:ProjectFile) file -> project.RemoveContentFile(file)) project

/// Removes projects Compile nodes that have Include attributes pointing to files missing from the file system.  Saves updated projects.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RemoveCompileNodesWithMissingFiles project =
    let newProject = removeCompileNodesWithMissingFiles System.IO.File.Exists (ProjectFile.FromFile project)
    newProject.Save()

/// Removes projects Content nodes that have Include attributes pointing to files missing from the file system.  Saves updated projects.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RemoveContentNodesWithMissingFiles project =
    let newProject = removeContentNodesWithMissingFiles System.IO.File.Exists (ProjectFile.FromFile project)
    newProject.Save()

         
