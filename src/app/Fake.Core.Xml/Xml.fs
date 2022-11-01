namespace Fake.Core

open System.Text

/// <summary>
/// Contains functions to read and write XML files.
/// </summary>
module Xml =

    open Fake.Core
    open System
    open System.IO

    open System.Xml
    open System.Xml.XPath
    open System.Xml.Xsl

    /// <summary>
    /// Reads a value from a XML document using a XPath
    /// </summary>
    ///
    /// <param name="failOnError">Flag to fail the operation when an error happens</param>
    /// <param name="xmlFileName">The XML file name</param>
    /// <param name="nameSpace">The XML name specification</param>
    /// <param name="prefix">The XML name specification prefix</param>
    /// <param name="xPath">X Path expression</param>
    let read failOnError (xmlFileName: string) nameSpace prefix xPath =
        try
            let document = XPathDocument(xmlFileName)
            let navigator = document.CreateNavigator()
            let manager = XmlNamespaceManager(navigator.NameTable)

            if prefix <> "" && nameSpace <> "" then
                manager.AddNamespace(prefix, nameSpace)

            let expression = XPathExpression.Compile(xPath, manager)

            seq {
                match expression.ReturnType with
                | XPathResultType.Number
                | XPathResultType.Boolean
                | XPathResultType.String -> yield navigator.Evaluate(expression).ToString()
                | XPathResultType.NodeSet ->
                    let nodes = navigator.Select(expression)

                    while nodes.MoveNext() do
                        yield nodes.Current.Value
                | _ ->
                    failwith
                    <| sprintf "XPath-Expression return type %A not implemented" expression.ReturnType
            }
        with exn ->
            if failOnError then
                failwithf "XMLRead error:\n%s" exn.Message
            else
                Seq.empty

    /// <summary>
    /// Reads a value from a XML document using a XPath
    /// Returns if the value is an int and the value
    /// </summary>
    ///
    /// <param name="failOnError">Flag to fail the operation when an error happens</param>
    /// <param name="xmlFileName">The XML file name</param>
    /// <param name="nameSpace">The XML name specification</param>
    /// <param name="prefix">The XML name specification prefix</param>
    /// <param name="xPath">X Path expression</param>
    let read_Int failOnError xmlFileName nameSpace prefix xPath =
        let headOrDefault def seq =
            if Seq.isEmpty seq then def else Seq.head seq

        read failOnError xmlFileName nameSpace prefix xPath
        |> Seq.map Int32.TryParse
        |> (fun seq ->
            if failOnError then
                Seq.head seq
            else
                headOrDefault (false, 0) seq)

    /// <summary>
    /// Creates a XmlWriter which writes to the given file name
    /// </summary>
    ///
    /// <param name="fileName">The XML file name</param>
    let getWriter (fileName: string) =
        let writer = XmlWriter.Create(File.OpenWrite(fileName), null)
        writer.WriteStartDocument()
        writer

    /// <summary>
    /// Writes an XML comment to the given XmlTextWriter
    /// </summary>
    ///
    /// <param name="comment">The comment to write</param>
    /// <param name="writer">The XML writer to use</param>
    let writeComment comment (writer: XmlWriter) =
        writer.WriteComment comment
        writer

    /// <summary>
    /// Writes an XML start element to the given XmlTextWriter
    /// </summary>
    ///
    /// <param name="name">The element name to start</param>
    /// <param name="writer">The XML writer to use</param>
    let startElement name (writer: XmlWriter) =
        writer.WriteStartElement name
        writer

    /// <summary>
    /// Writes an XML end element to the given XmlTextWriter
    /// </summary>
    ///
    /// <param name="writer">The XML writer to use</param>
    let endElement (writer: XmlWriter) =
        writer.WriteEndElement()
        writer

    /// <summary>
    /// Writes an XML attribute to current element of the given XmlTextWriter
    /// </summary>
    ///
    /// <param name="name">The attribute name</param>
    /// <param name="value">The attribute value</param>
    /// <param name="writer">The XML writer to use</param>
    let writeAttribute name value (writer: XmlWriter) =
        writer.WriteAttributeString(name, value.ToString())
        writer

    /// <summary>
    /// Writes an CData element to the given XmlTextWriter
    /// </summary>
    ///
    /// <param name="elementName">The element name</param>
    /// <param name="data">The CData to write</param>
    /// <param name="writer">The XML writer to use</param>
    let writeCDataElement elementName data (writer: XmlWriter) =
        startElement elementName writer |> ignore
        writer.WriteCData data
        endElement writer

    /// <summary>
    /// Gets the attribute with the given name from the given XmlNode
    /// </summary>
    ///
    /// <param name="name">The attribute name to get</param>
    /// <param name="node">The XML node to get attribute on</param>
    let getAttribute (name: string) (node: #XmlNode) =
        let attribute = node.Attributes[name]
        if not (isNull attribute) then attribute.Value else null

    /// <summary>
    /// Gets a sequence of all sub nodes for the given XmlNode
    /// </summary>
    ///
    /// <param name="node">The XML node to get sub nodes for</param>
    let getSubNodes (node: #XmlNode) = seq { for x in node.ChildNodes -> x }

    /// <summary>
    /// Gets the first sub node with the given name from the given XmlNode
    /// </summary>
    ///
    /// <param name="name">The sub node name</param>
    /// <param name="node">The XML node to get sub node for</param>
    let getSubNode name node =
        getSubNodes node |> Seq.filter (fun x -> x.Name = name) |> Seq.head

    /// <summary>
    /// Parses a XmlNode
    /// </summary>
    ///
    /// <param name="name">The node name</param>
    /// <param name="f">The parser function to use</param>
    /// <param name="node">The XML node to parse</param>
    let parse name f (node: #XmlNode) =
        if node.Name = name then
            f node
        else
            failwithf "Could not parse %s - Node was %s" name node.Name

    /// <summary>
    /// Parses a XML sub node
    /// </summary>
    ///
    /// <param name="name">The sub node name</param>
    /// <param name="f">The parser function to use</param>
    let parseSubNode name f = getSubNode name >> parse name f

    /// <summary>
    /// Loads the given text into a XmlDocument
    /// </summary>
    ///
    /// <param name="text">The XML text</param>
    let createDoc text =
        if String.isNullOrEmpty text then
            null
        else
            let xmlDocument = XmlDocument()
            xmlDocument.LoadXml text
            xmlDocument

    /// <summary>
    /// Gets the DocumentElement of the XmlDocument
    /// </summary>
    ///
    /// <param name="doc">The XmlDocument object</param>
    let getDocElement (doc: XmlDocument) = doc.DocumentElement

    /// <summary>
    /// Replaces text in the XML document specified by a XPath expression.
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression to use to select node</param>
    /// <param name="value">The new value</param>
    /// <param name="doc">The XML document to search on</param>
    let replaceXPath xpath value (doc: XmlDocument) =
        let node = doc.SelectSingleNode xpath

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.Value <- value
            doc

    /// <summary>
    /// Replaces the inner text of an xml node in the XML document specified by a XPath expression.
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression to use to select node</param>
    /// <param name="innerTextValue">The new value</param>
    /// <param name="doc">The XML document to search on</param>
    let replaceXPathInnerText xpath innerTextValue (doc: XmlDocument) =
        let node = doc.SelectSingleNode xpath

        if isNull node then
            failwithf "XML node '%s' not found" xpath
        else
            node.InnerText <- innerTextValue
            doc

    /// <summary>
    /// Replaces the value of attribute in an xml node in the XML document specified by a XPath expression.
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression to use to select node</param>
    /// <param name="attribute">The attribute name</param>
    /// <param name="innerTextValue">The new attribute value</param>
    /// <param name="doc">The XML document to search on</param>
    let replaceXPathAttribute xpath (attribute: string) value (doc: XmlDocument) =
        let node = doc.SelectSingleNode xpath

        if isNull node then
            failwithf "XML node '%s' not found" xpath
        else
            let attributeValue = node.Attributes[attribute]

            if isNull attributeValue then
                failwithf "XML node '%s' does not have attribute '%s'" xpath attribute
            else
                attributeValue.Value <- value
                doc

    /// <summary>
    /// Selects a xml node value via XPath from the given document
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression to use to select node</param>
    /// <param name="namespaces">The namespaces list</param>
    /// <param name="doc">The XML document to search on</param>
    let selectXPathValue xpath (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.DocumentElement.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.InnerText

    /// <summary>
    /// Selects a xml node attribute value via XPath from the given document
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression to use to select node</param>
    /// <param name="attribute">The attribute name</param>
    /// <param name="namespaces">The namespaces list</param>
    /// <param name="doc">The XML document to search on</param>
    let selectXPathAttributeValue xpath (attribute: string) (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.DocumentElement.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            let attributeValue = node.Attributes[attribute]

            if isNull attributeValue then
                failwithf "XML node '%s' does not have attribute '%s'" xpath attribute
            else
                attributeValue.Value

    /// <summary>
    /// Selects a xml node via XPath from the given document
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression to use to select node</param>
    /// <param name="namespaces">The namespaces list</param>
    /// <param name="doc">The XML document to search on</param>
    let selectXPathNode xpath (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.DocumentElement.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node

    let private load (fileName: string) (doc: XmlDocument) =
        use fs = File.OpenRead(fileName)
        doc.Load fs

    /// <summary>
    /// Save the given XmlDocument to file path
    /// </summary>
    ///
    /// <param name="fileName">The output file name</param>
    /// <param name="doc">The XML document to save</param>
    let saveDoc (fileName: string) (doc: XmlDocument) =
        // https://stackoverflow.com/questions/284394/net-xmldocument-why-doctype-changes-after-save
        // https://stackoverflow.com/a/16451790
        // https://github.com/fsharp/FAKE/issues/1692
#if !NETSTANDARD1_6 // required APIs available since 2.0
        let docType = doc.DocumentType

        if not (isNull docType) then
            if String.IsNullOrWhiteSpace docType.InternalSubset then
                let documentTypeWithNullInternalSubset =
                    doc.CreateDocumentType(docType.Name, docType.PublicId, docType.SystemId, null)

                docType.ParentNode.ReplaceChild(documentTypeWithNullInternalSubset, docType)
                |> ignore
#endif
        use fs = File.Open(fileName, FileMode.Truncate, FileAccess.Write)
        doc.Save fs

    /// <summary>
    /// Loads the given file path into a XmlDocument
    /// </summary>
    ///
    /// <param name="path">The file path to load an XmlDocument from</param>
    let loadDoc (path: string) =
        if String.isNullOrEmpty path then
            null
        else
            let xmlDocument = XmlDocument()
            load path xmlDocument
            xmlDocument

    /// <summary>
    /// Replaces text in a XML file at the location specified by a XPath expression.
    /// </summary>
    ///
    /// <param name="fileName">The file name to use</param>
    /// <param name="xpath">The XPath expression used to select text</param>
    /// <param name="value">The new value</param>
    let poke (fileName: string) xpath value =
        let doc = XmlDocument()
        load fileName doc
        replaceXPath xpath value doc |> saveDoc fileName

    /// <summary>
    /// Replaces the inner text of an xml node in a XML file at the location specified by a XPath expression.
    /// </summary>
    ///
    /// <param name="fileName">The file name to use</param>
    /// <param name="xpath">The XPath expression used to select text</param>
    /// <param name="innerTextValue">The new value</param>
    let pokeInnerText (fileName: string) xpath innerTextValue =
        let doc = XmlDocument()
        load fileName doc
        replaceXPathInnerText xpath innerTextValue doc |> saveDoc fileName

    /// <summary>
    /// Replaces text in a XML document specified by a XPath expression, with support for namespaces.
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression used to select text</param>
    /// <param name="value">The new value</param>
    /// <param name="fileName">The file name to use</param>
    /// <param name="namespaces">The namespaces list</param>
    /// <param name="doc">The XmlDocument object</param>
    let replaceXPathNS xpath value (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.Value <- value
            doc

    /// <summary>
    /// Replaces inner text in a XML document specified by a XPath expression, with support for namespaces.
    /// </summary>
    ///
    /// <param name="xpath">The XPath expression used to select text</param>
    /// <param name="innerTextValue">The new value</param>
    /// <param name="fileName">The file name to use</param>
    /// <param name="namespaces">The namespaces list</param>
    /// <param name="doc">The XmlDocument object</param>
    let replaceXPathInnerTextNS xpath innerTextValue (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.InnerText <- innerTextValue
            doc

    /// <summary>
    /// Replaces text in a XML file at the location specified by a XPath expression, with support for namespaces.
    /// </summary>
    ///
    /// <param name="fileName">The file name to use</param>
    /// <param name="namespaces">The namespaces list</param>
    /// <param name="xpath">The XPath expression used to select text</param>
    /// <param name="value">The new value</param>
    let pokeNS (fileName: string) namespaces xpath value =
        let doc = XmlDocument()
        load fileName doc
        replaceXPathNS xpath value namespaces doc |> saveDoc fileName

    /// <summary>
    /// Replaces inner text of an xml node in a XML file at the location specified by a XPath expression,
    /// with support for namespaces.
    /// </summary>
    ///
    /// <param name="fileName">The file name to use</param>
    /// <param name="namespaces">The namespaces list</param>
    /// <param name="xpath">The XPath expression used to select text</param>
    /// <param name="innerTextValue">The new value</param>
    let pokeInnerTextNS (fileName: string) namespaces xpath innerTextValue =
        let doc = XmlDocument()
        load fileName doc
        replaceXPathInnerTextNS xpath innerTextValue namespaces doc |> saveDoc fileName

    /// <summary>
    /// Loads the given text into a XslCompiledTransform.
    /// </summary>
    ///
    /// <param name="text">The text to use</param>
    let XslTransformer text =
        if String.isNullOrEmpty text then
            null
        else
            let xslCompiledTransform = XslCompiledTransform()
            createDoc text |> xslCompiledTransform.Load
            xslCompiledTransform

    /// <summary>
    /// Transforms a XmlDocument using a XslCompiledTransform.
    /// </summary>
    ///
    /// <param name="xsl">The XslCompiledTransform which should be applied.</param>
    /// <param name="doc">The XmlDocument to transform.</param>
    let XslTransform (xsl: XslCompiledTransform) (doc: XmlDocument) =
        use memoryStream = new MemoryStream()
        use textWriter = XmlWriter.Create(memoryStream) // , new UTF8Encoding(false)
        use writer = XmlWriter.Create(textWriter, xsl.OutputSettings)
        writer.WriteStartDocument()
        xsl.Transform(doc, null, writer)
        let outputDoc = XmlDocument()
        let encoding = UTF8Encoding(false)
        memoryStream.ToArray() |> encoding.GetString |> outputDoc.LoadXml
        outputDoc

    /// <summary>
    /// Transforms a XML file using a XSL stylesheet file.
    /// </summary>
    ///
    /// <param name="stylesheetUri">The Uri for the XSL stylesheet file.</param>
    /// <param name="fileName">The XML file to transform.</param>
    let XmlTransform (stylesheetUri: string) (fileName: string) =
        let doc = XmlDocument()
        doc.Load fileName
        let xsl = XslCompiledTransform()
        xsl.Load stylesheetUri
        XslTransform xsl doc |> saveDoc fileName
