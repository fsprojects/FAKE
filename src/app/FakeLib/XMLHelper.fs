[<AutoOpen>]
/// Contains functions to read and write XML files.
module Fake.XMLHelper

open System
open System.Collections.Generic
open System.Text
open System.IO
open System.Xml
open System.Xml.XPath
open System.Xml.Xsl

/// Reads a value from a XML document using a XPath
let XMLRead failOnError (xmlFileName : string) nameSpace prefix xPath = 
    try 
        let document = new XPathDocument(xmlFileName)
        let navigator = document.CreateNavigator()
        let manager = new XmlNamespaceManager(navigator.NameTable)
        if prefix <> "" && nameSpace <> "" then manager.AddNamespace(prefix, nameSpace)
        let expression = XPathExpression.Compile(xPath, manager)
        seq { 
            match expression.ReturnType with
            | XPathResultType.Number | XPathResultType.Boolean | XPathResultType.String -> 
                yield navigator.Evaluate(expression).ToString()
            | XPathResultType.NodeSet -> 
                let nodes = navigator.Select(expression)
                while nodes.MoveNext() do
                    yield nodes.Current.Value
            | _ -> failwith <| sprintf "XPath-Expression return type %A not implemented" expression.ReturnType
        }
    with exn -> 
        if failOnError then failwithf "XMLRead error:\n%s" exn.Message
        else Seq.empty

/// Reads a value from a XML document using a XPath
/// Returns if the value is an int and the value
let XMLRead_Int failOnError xmlFileName nameSpace prefix xPath = 
    let headOrDefault def seq = 
        if Seq.isEmpty seq then def
        else Seq.head seq
    XMLRead failOnError xmlFileName nameSpace prefix xPath
    |> Seq.map Int32.TryParse
    |> (fun seq -> 
    if failOnError then Seq.head seq
    else headOrDefault (false, 0) seq)

/// Creates a XmlWriter which writes to the given file name
let XmlWriter(fileName : string) = 
    let writer = new XmlTextWriter(fileName, null)
    writer.WriteStartDocument()
    writer

/// Writes an XML comment to the given XmlTextWriter
let XmlComment comment (writer : XmlTextWriter) = 
    writer.WriteComment comment
    writer

/// Writes an XML start element to the given XmlTextWriter
let XmlStartElement name (writer : XmlTextWriter) = 
    writer.WriteStartElement name
    writer

/// Writes an XML end element to the given XmlTextWriter
let XmlEndElement(writer : XmlTextWriter) = 
    writer.WriteEndElement()
    writer

/// Writes an XML attribute to current element of the given XmlTextWriter
let XmlAttribute name value (writer : XmlTextWriter) = 
    writer.WriteAttributeString(name, value.ToString())
    writer

/// Writes an CData element to the given XmlTextWriter
let XmlCDataElement elementName data (writer : XmlTextWriter) = 
    XmlStartElement elementName writer |> ignore
    writer.WriteCData data
    XmlEndElement writer

/// Gets the attribute with the given name from the given XmlNode
let getAttribute (name : string) (node : #XmlNode) = node.Attributes.[name].Value

/// Gets a sequence of all child nodes for the given XmlNode
let getChilds (node : #XmlNode) = 
    seq { 
        for x in node.ChildNodes -> x
    }

/// Gets the first sub node with the given name from the given XmlNode
let getSubNode name node = 
    getChilds node
    |> Seq.filter (fun x -> x.Name = name)
    |> Seq.head

/// Parses a XmlNode
let parse name f (node : #XmlNode) = 
    if node.Name = name then f node
    else failwithf "Could not parse %s - Node was %s" name node.Name

/// Parses a XML subnode
let parseSubNode name f = getSubNode name >> parse name f

/// Loads the given text into a XmlDocument
let XMLDoc text = 
    if isNullOrEmpty text then null
    else 
        let xmlDocument = new XmlDocument()
        xmlDocument.LoadXml text
        xmlDocument

/// Gets the DocumentElement of the XmlDocument
let DocElement(doc : XmlDocument) = doc.DocumentElement

/// Replaces text in the XML document specified by a XPath expression.
let XPathReplace xpath value (doc : XmlDocument) = 
    let node = doc.SelectSingleNode xpath
    if node = null then failwithf "XML node '%s' not found" xpath
    else 
        node.Value <- value
        doc

/// Replaces the inner text of an xml node in the XML document specified by a XPath expression.
let XPathReplaceInnerText xpath innerTextValue (doc : XmlDocument) = 
    let node = doc.SelectSingleNode xpath
    if node = null then failwithf "XML node '%s' not found" xpath
    else 
        node.InnerText <- innerTextValue
        doc

/// Selects a xml node value via XPath from the given document
let XPathValue xpath (namespaces : #seq<string * string>) (doc : XmlDocument) = 
    let nsmgr = XmlNamespaceManager(doc.NameTable)
    namespaces |> Seq.iter nsmgr.AddNamespace
    let node = doc.DocumentElement.SelectSingleNode(xpath, nsmgr)
    if node = null then failwithf "XML node '%s' not found" xpath
    else node.InnerText

/// Replaces text in a XML file at the location specified by a XPath expression.
let XmlPoke (fileName : string) xpath value = 
    let doc = new XmlDocument()
    doc.Load fileName
    XPathReplace xpath value doc |> fun x -> x.Save fileName

/// Replaces the inner text of an xml node in a XML file at the location specified by a XPath expression.
let XmlPokeInnerText (fileName : string) xpath innerTextValue = 
    let doc = new XmlDocument()
    doc.Load fileName
    XPathReplaceInnerText xpath innerTextValue doc |> fun x -> x.Save fileName

/// Replaces text in a XML document specified by a XPath expression, with support for namespaces.
let XPathReplaceNS xpath value (namespaces : #seq<string * string>) (doc : XmlDocument) = 
    let nsmgr = XmlNamespaceManager(doc.NameTable)
    namespaces |> Seq.iter nsmgr.AddNamespace
    let node = doc.SelectSingleNode(xpath, nsmgr)
    if node = null then failwithf "XML node '%s' not found" xpath
    else 
        node.Value <- value
        doc

/// Replaces inner text in a XML document specified by a XPath expression, with support for namespaces.
let XPathReplaceInnerTextNS xpath innerTextValue (namespaces : #seq<string * string>) (doc : XmlDocument) = 
    let nsmgr = XmlNamespaceManager(doc.NameTable)
    namespaces |> Seq.iter nsmgr.AddNamespace
    let node = doc.SelectSingleNode(xpath, nsmgr)
    if node = null then failwithf "XML node '%s' not found" xpath
    else 
        node.InnerText <- innerTextValue
        doc

/// Replaces text in a XML file at the location specified by a XPath expression, with support for namespaces.
let XmlPokeNS (fileName : string) namespaces xpath value = 
    let doc = new XmlDocument()
    doc.Load fileName
    XPathReplaceNS xpath value namespaces doc |> fun x -> x.Save fileName

/// Replaces inner text of an xml node in a XML file at the location specified by a XPath expression, with support for namespaces.
let XmlPokeInnerTextNS (fileName : string) namespaces xpath innerTextValue = 
    let doc = new XmlDocument()
    doc.Load fileName
    XPathReplaceInnerTextNS xpath innerTextValue namespaces doc |> fun x -> x.Save fileName

/// Loads the given text into a XslCompiledTransform.
let XslTransformer text = 
    if isNullOrEmpty text then null
    else 
        let xslCompiledTransform = new XslCompiledTransform()
        XMLDoc(text) |> xslCompiledTransform.Load
        xslCompiledTransform

/// Transforms a XmlDocument using a XslCompiledTransform.
/// ## Parameters
/// 
///  - `xsl` - The XslCompiledTransform which should be applied.
///  - `doc` - The XmlDocument to transform.
let XslTransform (xsl : XslCompiledTransform) (doc : XmlDocument) = 
    use memoryStream = new MemoryStream()
    use textWriter = new XmlTextWriter(memoryStream, new UTF8Encoding(false))
    use writer = System.Xml.XmlWriter.Create(textWriter, xsl.OutputSettings)
    writer.WriteStartDocument()
    xsl.Transform(doc, null, writer)
    let outputDoc = new XmlDocument()
    let encoding = new UTF8Encoding(false)
    memoryStream.ToArray()
    |> encoding.GetString
    |> outputDoc.LoadXml
    outputDoc

/// Transforms a XML file using a XSL stylesheet file.
/// ## Parameters
/// 
///  - `stylesheetUri` - The Uri for the XSL stylesheet file.
///  - `fileName` - The XML file to transform.
let XmlTransform (stylesheetUri : string) (fileName : string) = 
    let doc = new XmlDocument()
    doc.Load fileName
    let xsl = new XslCompiledTransform()
    xsl.Load stylesheetUri
    XslTransform xsl doc |> fun x -> x.Save fileName
