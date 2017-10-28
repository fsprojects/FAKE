/// Contains functions to read and write XML files.
module Fake.Core.Xml

open Fake.Core.String
open Fake.Core.BuildServer
open Fake.Core.Process
open Fake.Core
open System
open System.IO
open System.Text

open System.Xml
open System.Xml.XPath
#if !NETSTANDARD
open System.Xml.Xsl
#endif

/// Reads a value from a XML document using a XPath
let Read failOnError (xmlFileName : string) nameSpace prefix xPath =
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
let Read_Int failOnError xmlFileName nameSpace prefix xPath =
    let headOrDefault def seq =
        if Seq.isEmpty seq then def
        else Seq.head seq
    Read failOnError xmlFileName nameSpace prefix xPath
    |> Seq.map Int32.TryParse
    |> (fun seq ->
    if failOnError then Seq.head seq
    else headOrDefault (false, 0) seq)

/// Creates a XmlWriter which writes to the given file name
let Writer(fileName : string) =
    let writer = XmlWriter.Create(File.OpenWrite(fileName), null)
    writer.WriteStartDocument()
    writer

/// Writes an XML comment to the given XmlTextWriter
let Comment comment (writer : XmlWriter) =
    writer.WriteComment comment
    writer

/// Writes an XML start element to the given XmlTextWriter
let StartElement name (writer : XmlWriter) =
    writer.WriteStartElement name
    writer

/// Writes an XML end element to the given XmlTextWriter
let EndElement(writer : XmlWriter) =
    writer.WriteEndElement()
    writer

/// Writes an XML attribute to current element of the given XmlTextWriter
let Attribute name value (writer : XmlWriter) =
    writer.WriteAttributeString(name, value.ToString())
    writer

/// Writes an CData element to the given XmlTextWriter
let CDataElement elementName data (writer : XmlWriter) =
    StartElement elementName writer |> ignore
    writer.WriteCData data
    EndElement writer

/// Gets the attribute with the given name from the given XmlNode
let getAttribute (name : string) (node : #XmlNode) =
    let attribute = node.Attributes.[name]
    if not (isNull attribute) then attribute.Value else null

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
let Doc text =
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
    if isNull node then failwithf "XML node '%s' not found" xpath
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

let private load (fileName:string) (doc:XmlDocument) =
    use fs = File.OpenRead(fileName)
    doc.Load fs
let private save (fileName:string) (doc:XmlDocument) =
    use fs = File.Open(fileName, FileMode.Truncate, FileAccess.Write)
    doc.Save fs

/// Replaces text in a XML file at the location specified by a XPath expression.
let Poke (fileName : string) xpath value =
    let doc = new XmlDocument()
    XPathReplace xpath value doc |> save fileName

/// Replaces the inner text of an xml node in a XML file at the location specified by a XPath expression.
let PokeInnerText (fileName : string) xpath innerTextValue =
    let doc = new XmlDocument()
    load fileName doc
    XPathReplaceInnerText xpath innerTextValue doc |> save fileName

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
let PokeNS (fileName : string) namespaces xpath value =
    let doc = new XmlDocument()
    load fileName doc
    XPathReplaceNS xpath value namespaces doc |> save fileName

/// Replaces inner text of an xml node in a XML file at the location specified by a XPath expression, with support for namespaces.
let PokeInnerTextNS (fileName : string) namespaces xpath innerTextValue =
    let doc = new XmlDocument()
    load fileName doc
    XPathReplaceInnerTextNS xpath innerTextValue namespaces doc |> save fileName

#if !NETSTANDARD
/// Loads the given text into a XslCompiledTransform.
[<System.Obsolete("This API is not jet available on netcore. If you have an alternative please switch. If the API becomes available this warning is removed.")>]
let XslTransformer text =
    if isNullOrEmpty text then null
    else
        let xslCompiledTransform = new XslCompiledTransform()
        Doc(text) |> xslCompiledTransform.Load
        xslCompiledTransform
/// Transforms a XmlDocument using a XslCompiledTransform.
/// ## Parameters
///
///  - `xsl` - The XslCompiledTransform which should be applied.
///  - `doc` - The XmlDocument to transform.
[<System.Obsolete("This API is not jet available on netcore. If you have an alternative please switch. If the API becomes available this warning is removed.")>]
let XslTransform (xsl : XslCompiledTransform) (doc : XmlDocument) =
    use memoryStream = new MemoryStream()
    use textWriter = XmlWriter.Create(memoryStream) // , new UTF8Encoding(false)
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
[<System.Obsolete("This API is not jet available on netcore. If you have an alternative please switch. If the API becomes available this warning is removed.")>]
let XmlTransform (stylesheetUri : string) (fileName : string) =
    let doc = new XmlDocument()
    doc.Load fileName
    let xsl = new XslCompiledTransform()
    xsl.Load stylesheetUri
    XslTransform xsl doc |> save fileName

#endif
