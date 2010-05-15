[<AutoOpen>]
module Fake.XMLHelper

open System
open System.Collections.Generic
open System.Text
open System.IO
open System.Xml
open System.Xml.XPath

/// Reads a value from a XML document using a XPath
let XMLRead failOnError (xmlFileName:string) nameSpace prefix xPath =
    try
        let document = new XPathDocument(xmlFileName)
        let navigator = document.CreateNavigator()
        let manager = new XmlNamespaceManager(navigator.NameTable)
        if prefix <> "" && nameSpace <> "" then manager.AddNamespace(prefix, nameSpace)
    
        let  expression = XPathExpression.Compile(xPath, manager)
        seq {
            match expression.ReturnType with
            | XPathResultType.Number 
            | XPathResultType.Boolean
            | XPathResultType.String  -> yield navigator.Evaluate(expression).ToString()
            | XPathResultType.NodeSet ->
                let nodes = navigator.Select(expression)            
                while nodes.MoveNext() do
                yield nodes.Current.Value
            | _ -> failwith <| sprintf "XPath-Expression return type %A not implemented" expression.ReturnType}
    with
    | exn -> if failOnError then failwith "XMLRead error:\n%s" exn.Message else Seq.empty

/// Reads a value from a XML document using a XPath
/// returns if the value is an int and the value
let XMLRead_Int failOnError xmlFileName nameSpace prefix xPath =
    XMLRead failOnError xmlFileName nameSpace prefix xPath 
      |> Seq.head
      |> Int32.TryParse
  
/// Generates an XmlWriter    
let XmlWriter (fileName:string) = 
    let writer = new XmlTextWriter(fileName, null)    
    writer.WriteStartDocument()
    writer

/// Writes an Xml comment      
let XmlComment comment (writer:XmlTextWriter) =
    writer.WriteComment comment
    writer
  
/// Writes an Xml element start
let XmlStartElement name (writer:XmlTextWriter) =
    writer.WriteStartElement name
    writer
  
/// Writes an Xml element end
let XmlEndElement (writer:XmlTextWriter) =
    writer.WriteEndElement()
    writer        
  
/// Writes an Xml attribute
let XmlAttribute name value (writer:XmlTextWriter) =
    writer.WriteAttributeString(name, value.ToString())
    writer    

/// Writes an CData element  
let XmlCDataElement elementName data (writer:XmlTextWriter) =
    XmlStartElement elementName writer |> ignore
    writer.WriteCData data
    XmlEndElement writer

/// Gets the attribute with the given name 
let getAttribute (name:string) (node : #System.Xml.XmlNode) = node.Attributes.[name].Value

/// Gets the child nodes for the given nodes
let getChilds (node : #System.Xml.XmlNode) = seq { for x in node.ChildNodes -> x }

/// gets the sub node with the name
let getSubNode name =
    getChilds 
      >> Seq.filter (fun x -> x.Name = name)
      >> Seq.head

/// parses a node
let parse name f (node : #System.Xml.XmlNode) =
    if node.Name = name then f node else 
    failwithf "Could not parse %s - Node was %s" name node.Name

/// parses a subnode
let parseSubNode name f =
    getSubNode name
      >> parse name f

/// Gets the result as xml
let XMLDoc text =
    if isNullOrEmpty text then null else
    let xmlDocument = new XmlDocument()
    xmlDocument.LoadXml text
    xmlDocument

/// Gets the DocumentElement of the XmlDocument
let DocElement (doc:XmlDocument) = doc.DocumentElement

/// Replaces text in XML document specified by an XPath expression.
let XPathReplace xpath value (doc:XmlDocument) =
    let node = doc.SelectSingleNode xpath
    node.Value <- value
    doc

/// Replaces text in an XML file at the location specified by an XPath expression.
let XmlPoke (fileName:string) xpath value =
    let doc = new XmlDocument()
    doc.Load fileName
    XPathReplace xpath value doc
      |> fun x -> x.Save fileName