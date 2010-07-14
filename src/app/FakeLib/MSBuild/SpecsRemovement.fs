[<AutoOpen>]
module Fake.MSBuild.SpecsRemovement

open Fake
open System.Xml
open System.Xml.Linq

/// Converts a MSBuildProject to XML
let normalize (project:MSBuildProject) =
    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        project.ToString(SaveOptions.DisableFormatting) 

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

let createFileName projectFileName =
    let fi = fileInfo projectFileName            
    fi.Directory.FullName @@ (fi.Name.Replace(fi.Extension,"") + "_Spliced" + fi.Extension)

/// <summary>Removes test data and test files from a given MSBuild project and recursivly from all MSBuild project dependencies</summary>
/// <param name="assemblyFilterF">A filter function for assembly references.</param>
/// <param name="fileFilterF">A filter function for files in a project.</param>
/// <param name="projectFileName">The MSBuild project to start.</param>
let RemoveTestsFromProject assemblyFilterF fileFilterF projectFileName =
    let processedProjects = new System.Collections.Generic.HashSet<_>()
    let rec removeTestsFromProject assemblyFilterF fileFilterF projectFileName =        
        let targetFileName = createFileName projectFileName

        if not <| processedProjects.Contains projectFileName then
            processedProjects.Add projectFileName |> ignore
            projectFileName
              |> loadProject
              |> removeAssemblyReference assemblyFilterF
              |> removeFiles fileFilterF     
              |> processReferences "ProjectReference" (removeTestsFromProject assemblyFilterF fileFilterF) projectFileName
              |> fun doc -> doc.Save(targetFileName,SaveOptions.DisableFormatting)

        targetFileName

    removeTestsFromProject assemblyFilterF fileFilterF projectFileName

// Default filters

/// All references to nunit.*.dlls
let AllNUnitReferences elementName (s:string) = s.StartsWith "nunit"

/// All Spec.cs or Spec.fs files
let AllSpecFiles elementName (s:string) = s.EndsWith "Specs.cs" || s.EndsWith "Specs.fs"

/// All Spec.cs or Spec.fs files and all files containing TestData
let AllSpecAndTestDataFiles elementName (s:string) =
    AllSpecFiles elementName s || ((elementName = "Content" || elementName = "None") && s.Contains("TestData"))

/// A Convetion which matches nothing
let Nothing _ _ = false

let RemoveAllNUnitReferences projectFileName =
    RemoveTestsFromProject AllNUnitReferences Nothing projectFileName

let RemoveAllSpecAndTestDataFiles projectFileName =
    RemoveTestsFromProject Nothing AllSpecAndTestDataFiles projectFileName