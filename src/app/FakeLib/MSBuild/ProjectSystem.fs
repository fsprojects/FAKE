/// Contains project file comparion tools for MSBuild project files.
module Fake.MSBuild.ProjectSystem

open Fake
open System.Xml
open System.Xml.Linq

/// A small abstraction over MSBuild project files.
type ProjectSystem(projectFileName : string) =
    let document = 
        ReadFileAsString projectFileName        
        |> XMLHelper.XMLDoc

    let nsmgr = 
        let nsmgr = new XmlNamespaceManager(document.NameTable)
        nsmgr.AddNamespace("default", document.DocumentElement.NamespaceURI)
        nsmgr

    let files = 
        let xpath = "/default:Project/default:ItemGroup/default:Compile/@Include"
        [for node in document.SelectNodes(xpath,nsmgr) -> node.InnerText]

    /// All files which are in "Compile" sections
    member x.Files = files

    /// The project file name
    member x.ProjectFileName = projectFileName

type ProjectComparison = {
    TemplateProjectFileName: string
    ProjectFileName: string
    MissingFiles: string seq
}

/// Compares the given project files againts the template project and returns which files are missing.
let findMissingFiles templateProject projects =
    let templateFiles = Set.ofSeq (ProjectSystem templateProject).Files

    projects
    |> Seq.map (fun fileName -> ProjectSystem fileName)
    |> Seq.toList
    |> List.map (fun ps -> 
                      { TemplateProjectFileName = templateProject
                        ProjectFileName = ps.ProjectFileName
                        MissingFiles = Set.difference templateFiles (Set.ofSeq ps.Files)})
    |> List.filter (fun pc -> Seq.isEmpty pc.MissingFiles |> not)

/// Compares the given project files againts the template project and fails if any files are missing.
let CompareProjectsTo templateProject projects =
    let errors =
        findMissingFiles templateProject projects
        |> List.map (fun pc -> sprintf "Missing files in %s:\r\n%s" pc.ProjectFileName (toLines pc.MissingFiles))
        |> toLines

    if isNotNullOrEmpty errors then
        failwith errors