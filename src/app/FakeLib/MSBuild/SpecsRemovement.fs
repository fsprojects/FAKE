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
      |> removeFilteredElement "None" filterF
      |> removeFilteredElement "Content" filterF

let removeFromProjectReferences f projectFileName (doc:XDocument) =
    let fi = fileInfo projectFileName
    doc
        .Descendants(xname "Project")
        .Descendants(xname "ItemGroup")
        .Descendants(xname "ProjectReference")
        |> Seq.iter(fun e -> 
            let a = e.Attribute(XName.Get "Include")
            let value = a.Value
            let fileName =
                if value.StartsWith(@"..\") then
                    fi.Directory.FullName @@ value
                else
                    value                                      
            a.Value <- f fileName)
    doc

let createFileName projectFileName =
    let fi = fileInfo projectFileName            
    fi.Directory.FullName @@ (fi.Name.Replace(fi.Extension,"") + "_Spliced" + fi.Extension)

let rec RemoveTestsFromProject assemblyFilterF fileFilterF projectFileName =
    let targetFileName = createFileName projectFileName
    projectFileName
      |> loadProject
      |> removeAssemblyReference assemblyFilterF
      |> removeFiles fileFilterF     
      |> removeFromProjectReferences (RemoveTestsFromProject assemblyFilterF fileFilterF) projectFileName
      |> fun doc -> doc.Save(targetFileName,SaveOptions.DisableFormatting)
    targetFileName

// Default filters

/// All references to nunit.*.dlls
let AllNUnitReferences elementName (s:string) = s.StartsWith("nunit")

/// All Spec.cs or Spec.fs files
let AllSpecFiles elementName (s:string) = s.EndsWith("Specs.cs") || s.EndsWith("Specs.fs")

/// All Spec.cs or Spec.fs files and all files containing TestData
let AllSpecAndTestDataFiles elementName (s:string) =
    AllSpecFiles elementName s || ((elementName = "Content" || elementName = "None") && s.Contains("TestData"))

let Nothing _ _ = false

let RemoveAllNUnitReferences projectFileName =
    RemoveTestsFromProject AllNUnitReferences Nothing projectFileName

let RemoveAllSpecAndTestDataFiles projectFileName =
    RemoveTestsFromProject Nothing AllSpecAndTestDataFiles projectFileName