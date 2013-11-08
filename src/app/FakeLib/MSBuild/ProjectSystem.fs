/// Contains project file comparion tools for MSBuild project files.
module Fake.MSBuild.ProjectSystem

open Fake
open System.Xml
open System.Xml.Linq

/// A small abstraction over MSBuild project files.
type ProjectFile(projectFileName:string,documentContent : string) =
    let document = XMLHelper.XMLDoc documentContent

    let nsmgr = 
        let nsmgr = new XmlNamespaceManager(document.NameTable)
        nsmgr.AddNamespace("default", document.DocumentElement.NamespaceURI)
        nsmgr

    let compileNodesXPath = "/default:Project/default:ItemGroup/default:Compile"
    let getCompileNodes (document:XmlDocument) =         
        [for node in document.SelectNodes(compileNodesXPath,nsmgr) -> node]

    let getFileAttributes (document:XmlDocument) = 
        let xpath = compileNodesXPath + "/@Include"
        [for attr in document.SelectNodes(xpath,nsmgr) -> attr]

    let files = getFileAttributes document |> List.map (fun attr -> attr.InnerText)
    
    /// Read a Project from a FileName
    static member FromFile(projectFileName) = new ProjectFile(projectFileName,ReadFileAsString projectFileName)

    /// Add a file to the Compile nodes
    member x.AddFile fileName =        
        let document = XMLHelper.XMLDoc documentContent // we create a copy and work immutable
        let node = getCompileNodes document |> Seq.last
        let newNode = node.CloneNode(false) :?> XmlElement
        newNode.SetAttribute("Include",fileName)
        
        node.ParentNode.AppendChild(newNode) |> ignore
        new ProjectFile(projectFileName,document.OuterXml)

    /// All files which are in "Compile" sections
    member x.Files = files

    /// The project file name
    member x.ProjectFileName = projectFileName

/// Result type for project comparisons.
type ProjectComparison = 
    { TemplateProjectFileName: string
      ProjectFileName: string
      MissingFiles: string seq
      UnorderedFiles: string seq }   
    
      member this.HasErrors = not (Seq.isEmpty this.MissingFiles && Seq.isEmpty this.UnorderedFiles)

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

                templateFiles 
                |> List.zip remainingFiles
                |> List.filter (fun (a,b) -> a <> b) 
                |> List.map fst

            { TemplateProjectFileName = templateProject
              ProjectFileName = ps.ProjectFileName
              MissingFiles = missingFiles
              UnorderedFiles = unorderedFiles })
    |> Seq.filter (fun pc -> pc.HasErrors)

/// Compares the given project files againts the template project and fails if any files are missing.
/// For F# projects it is also reporting unordered files.
let CompareProjectsTo templateProject projects =
    let errors =
        findMissingFiles templateProject projects
        |> Seq.map (fun pc -> 
                if Seq.isEmpty pc.UnorderedFiles then
                    sprintf "Missing files in %s:\r\n%s" pc.ProjectFileName (toLines pc.MissingFiles)
                else
                    sprintf "Unordered files in %s:\r\n%s" pc.ProjectFileName (toLines pc.UnorderedFiles))
        |> toLines

    if isNotNullOrEmpty errors then
        failwith errors