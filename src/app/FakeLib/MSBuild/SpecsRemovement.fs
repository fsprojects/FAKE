[<AutoOpen>]
/// Contains functions which allow to remove side-by-side specs during the build.
module Fake.MSBuild.SpecsRemovement

open Fake
open System.Xml
open System.Xml.Linq

/// Converts a MSBuildProject to XML
/// [omit]
let normalize (project:MSBuildProject) =
    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        project.ToString(SaveOptions.DisableFormatting) 

/// [omit]
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

/// [omit]
let removeAssemblyReference filterF (doc:XDocument)=
    removeFilteredElement "Reference" filterF doc

/// [omit]
let removeFiles filterF (doc:XDocument) =
    removeFilteredElement "Compile" filterF doc
      |> removeFilteredElement "None" filterF
      |> removeFilteredElement "Content" filterF

/// [omit]
let createFileName projectFileName =
    let fi = fileInfo projectFileName            
    fi.Directory.FullName @@ (fi.Name.Replace(fi.Extension,"") + "_Spliced" + fi.Extension)

/// Removes test data and test files from a given MSBuild project and recursivly from all MSBuild project dependencies.
/// ## Parameters
///
///  - `assemblyFilterF` - A filter function for assembly references.
///  - `fileFilterF` - A filter function for files in a project.
///  - `projectFileName` - The MSBuild project to start.
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

/// All references to nunit.*.dlls
let AllNUnitReferences elementName (s:string) = s.StartsWith "nunit"

/// All Spec.cs or Spec.fs files
let AllSpecFiles elementName (s:string) = s.EndsWith "Specs.cs" || s.EndsWith "Specs.fs"

/// All Spec.cs or Spec.fs files and all files containing TestData
let AllSpecAndTestDataFiles elementName (s:string) =
    AllSpecFiles elementName s || ((elementName = "Content" || elementName = "None") && s.Contains("TestData"))

/// A Convetion which matches nothing
let Nothing _ _ = false

/// Removes all NUnit references from a project.
let RemoveAllNUnitReferences projectFileName =
    RemoveTestsFromProject AllNUnitReferences Nothing projectFileName

/// Removes all spec and test data references from a project.
let RemoveAllSpecAndTestDataFiles projectFileName =
    RemoveTestsFromProject Nothing AllSpecAndTestDataFiles projectFileName