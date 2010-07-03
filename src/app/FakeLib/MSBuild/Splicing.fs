[<AutoOpen>]
module Fake.MSBuild.Splicing

open Fake
open System.Xml
open System.Xml.XPath
open System.Xml.Linq

let removeAssemblyReference project filterF =
    let doc = XDocument.Parse(project,LoadOptions.PreserveWhitespace)
    let ns = "http://schemas.microsoft.com/developer/msbuild/2003"

    let xname name = XName.Get(name,ns)

    let references =
        doc
          .Descendants(xname "Project")
          .Descendants(xname "ItemGroup")
          .Descendants(xname "Reference")
         |> Seq.filter(fun e -> 
                let a = e.Attribute(XName.Get "Include")
                a <> null && filterF (a.Value))
    references.Remove()

    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
      doc.ToString(SaveOptions.DisableFormatting)


let removeFiles project filterF =
    let doc = XDocument.Parse(project,LoadOptions.PreserveWhitespace)
    let ns = "http://schemas.microsoft.com/developer/msbuild/2003"

    let xname name = XName.Get(name,ns)

    let references =
        doc
          .Descendants(xname "Project")
          .Descendants(xname "ItemGroup")
          .Descendants(xname "Compile")
         |> Seq.filter(fun e -> 
                let a = e.Attribute(XName.Get "Include")
                a <> null && filterF (a.Value))
    references.Remove()

    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
      doc.ToString(SaveOptions.DisableFormatting)
