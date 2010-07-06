[<AutoOpen>]
module Fake.MSBuild.SpecsRemovement

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

let removeFilteredElement elementName filterF (doc:XDocument) =
    let references =
        doc
          .Descendants(xname "Project")
          .Descendants(xname "ItemGroup")
          .Descendants(xname elementName)
         |> Seq.filter(fun e -> 
                let a = e.Attribute(XName.Get "Include")
                a <> null && filterF elementName a.Value)
    references.Remove()
    doc

let removeAssemblyReference filterF (doc:XDocument)=
    removeFilteredElement "Reference" filterF doc

let removeFiles filterF (doc:XDocument) =
    removeFilteredElement "Compile" filterF doc
      |> removeFilteredElement "Content" filterF

let RemoveTestsFromProjectWithFileName assemblyFilterF fileFilterF (targetFileName:string) projectFileName =
    projectFileName
      |> loadProject
      |> removeAssemblyReference assemblyFilterF
      |> removeFiles fileFilterF
      |> fun doc -> doc.Save(targetFileName,SaveOptions.DisableFormatting)
    targetFileName

let RemoveTestsFromProject assemblyFilterF fileFilterF projectFileName =
    let fi = fileInfo projectFileName            
    let targetFileName = fi.Directory.FullName @@ (fi.Name.Replace(fi.Extension,"") + "_Spliced" + fi.Extension)
    RemoveTestsFromProjectWithFileName assemblyFilterF fileFilterF targetFileName projectFileName 

// Default filters

/// All references to nunit.*.dlls
let AllNUnitReferences elementName (s:string) = s.StartsWith("nunit")

/// All Spec.cs or Spec.fs files
let AllSpecFiles elementName (s:string) = s.EndsWith("Specs.cs") || s.EndsWith("Specs.fs")

/// All Spec.cs or Spec.fs files and all files containing TestData
let AllSpecAndTestDataFiles elementName (s:string) =
    AllSpecFiles elementName s || (elementName = "Content" && s.Contains("TestData"))

let Nothing _ _ = false

let RemoveAllNUnitReferences projectFileName =
    RemoveTestsFromProject AllNUnitReferences Nothing projectFileName

let RemoveAllSpecAndTestDataFiles projectFileName =
    RemoveTestsFromProject Nothing AllSpecAndTestDataFiles projectFileName