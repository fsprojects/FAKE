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

let findMissingFiles templateProject projects =
    let projectFiles = projects |> Seq.map (fun f -> f,(ProjectSystem f).Files |> Set.ofSeq) |> Seq.toList
    let templateFiles = (ProjectSystem templateProject).Files |> Set.ofSeq

    projectFiles
    |> List.map (fun (f,pf) -> f, pf |> Set.difference templateFiles)
    |> List.filter (fun (f,m) -> Set.isEmpty m |> not)

let compareProjects templateProject projects =
    let errors =
        findMissingFiles templateProject projects
        |> List.map (fun (f,m) -> sprintf "Missing files in %s:\r\n%s" f (toLines m))
        |> toLines

    if isNotNullOrEmpty errors then
        failwith errors