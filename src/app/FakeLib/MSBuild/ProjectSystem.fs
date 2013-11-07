module Fake.MsBuild.ProjectSystem

open Fake
open System.Xml
open System.Xml.Linq

type ProjectSystem(projectFile : string) = 
    let document = 
        ReadFileAsString projectFile        
        |> XMLHelper.XMLDoc

    let nsmgr = 
        let nsmgr = new XmlNamespaceManager(document.NameTable)
        nsmgr.AddNamespace("default", document.DocumentElement.NamespaceURI)
        nsmgr

    let files = 
        let xpath = "/default:Project/default:ItemGroup/default:Compile/@Include"
        [for node in document.SelectNodes(xpath,nsmgr) -> node.InnerText]

    member x.Files = files

let findMissingFiles projectFile1 projectFile2 =
    let files1 = (ProjectSystem projectFile1).Files |> Set.ofSeq
    let files2 = (ProjectSystem projectFile2).Files |> Set.ofSeq

    files1 |> Set.difference files2,files2 |> Set.difference files1

let findMissingFilesFromTemplate templateProject projects =
    let projectFiles = projects |> Seq.map (fun f -> f,(ProjectSystem f).Files |> Set.ofSeq) |> Seq.toList
    let templateFiles = (ProjectSystem templateProject).Files |> Set.ofSeq

    projectFiles
    |> List.map (fun (f,pf) -> f, pf |> Set.difference templateFiles)
    |> List.filter (fun (f,m) -> Set.isEmpty m)

let compareProjects templateProject projects =
    let errors =
        findMissingFilesFromTemplate templateProject projects
        |> List.map (fun (f,m) -> sprintf "Missing files in %s:\r\n%s" f (toLines m))
        |> toLines

    if isNotNullOrEmpty errors then
        failwith errors