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
    let proj1 = ProjectSystem projectFile1
    let proj2 = ProjectSystem projectFile2

    let missing1 = 
        [for file in proj1.Files do
            if proj2.Files |> List.exists ((=) file) |> not then
                yield file]

    let missing2 = 
        [for file in proj2.Files do
            if proj1.Files |> List.exists ((=) file) |> not then
                yield file]
    missing1,missing2