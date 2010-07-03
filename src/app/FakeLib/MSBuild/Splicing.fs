[<AutoOpen>]
module Fake.MSBuild.Splicing

open Fake
open System.Xml
open System.Xml.Linq

type MSBuildProject = XDocument

let normalize (project:MSBuildProject) =
    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        project.ToString(SaveOptions.DisableFormatting) 

let msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003"
let xname name = XName.Get(name,msbuildNamespace)

let loadProject (projectFileName:string) : MSBuildProject = 
    MSBuildProject.Load(projectFileName,LoadOptions.PreserveWhitespace)

let removeFilteredElement (doc:XDocument) elementName filterF =
    let references =
        doc
          .Descendants(xname "Project")
          .Descendants(xname "ItemGroup")
          .Descendants(xname elementName)
         |> Seq.filter(fun e -> 
                let a = e.Attribute(XName.Get "Include")
                a <> null && filterF (a.Value))
    references.Remove()
    doc

let removeAssemblyReference (doc:XDocument) filterF =
    removeFilteredElement doc "Reference" filterF


let removeFiles (doc:XDocument) filterF =
    removeFilteredElement doc "Compile" filterF