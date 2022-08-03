namespace Fake.Core

open System.Text

/// Contains functions to read and write XML files.
module Xml =

    open Fake.Core
    open System
    open System.IO

    open System.Xml
    open System.Xml.XPath
    open System.Xml.Xsl

    /// Reads a value from a XML document using a XPath
    ///
    /// ## Parameters
    ///  - `failOnError` - Flag to fail the operation when an error happens
    ///  - `xmlFileName` - The XML file name
    ///  - `nameSpace` - The XML name specification
    ///  - `prefix` - The XML name specification prefix
    ///  - `xPath` - X Path expression
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

    /// Reads a value from a XML document using a XPath
    /// Returns if the value is an int and the value
    ///
    /// ## Parameters
    ///  - `failOnError` - Flag to fail the operation when an error happens
    ///  - `xmlFileName` - The XML file name
    ///  - `nameSpace` - The XML name specification
    ///  - `prefix` - The XML name specification prefix
    ///  - `xPath` - X Path expression
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

    /// Creates a XmlWriter which writes to the given file name
    ///
    /// ## Parameters
    ///  - `fileName` - The XML file name
    let getWriter (fileName: string) =
        let writer = XmlWriter.Create(File.OpenWrite(fileName), null)
        writer.WriteStartDocument()
        writer

    /// Writes an XML comment to the given XmlTextWriter
    ///
    /// ## Parameters
    ///  - `comment` - The comment to write
    ///  - `writer` - The XML writer to use
    let writeComment comment (writer: XmlWriter) =
        writer.WriteComment comment
        writer

    /// Writes an XML start element to the given XmlTextWriter
    ///
    /// ## Parameters
    ///  - `name` - The element name to start
    ///  - `writer` - The XML writer to use
    let startElement name (writer: XmlWriter) =
        writer.WriteStartElement name
        writer

    /// Writes an XML end element to the given XmlTextWriter
    ///
    /// ## Parameters
    ///  - `writer` - The XML writer to use
    let endElement (writer: XmlWriter) =
        writer.WriteEndElement()
        writer

    /// Writes an XML attribute to current element of the given XmlTextWriter
    ///
    /// ## Parameters
    ///  - `name` - The attribute name
    ///  - `value` - The attribute value
    ///  - `writer` - The XML writer to use
    let writeAttribute name value (writer: XmlWriter) =
        writer.WriteAttributeString(name, value.ToString())
        writer

    /// Writes an CData element to the given XmlTextWriter
    ///
    /// ## Parameters
    ///  - `elementName` - The element name
    ///  - `data` - The CData to write
    ///  - `writer` - The XML writer to use
    let writeCDataElement elementName data (writer: XmlWriter) =
        startElement elementName writer |> ignore
        writer.WriteCData data
        endElement writer

    /// Gets the attribute with the given name from the given XmlNode
    ///
    /// ## Parameters
    ///  - `name` - The attribute name to get
    ///  - `node` - The XML node to get attribute on
    let getAttribute (name: string) (node: #XmlNode) =
        let attribute = node.Attributes[name]
        if not (isNull attribute) then attribute.Value else null

    /// Gets a sequence of all sub nodes for the given XmlNode
    ///
    /// ## Parameters
    ///  - `node` - The XML node to get sub nodes for
    let getSubNodes (node: #XmlNode) = seq { for x in node.ChildNodes -> x }

    /// Gets the first sub node with the given name from the given XmlNode
    ///
    /// ## Parameters
    ///  - `name` - The sub node name
    ///  - `node` - The XML node to get sub node for
    let getSubNode name node =
        getSubNodes node |> Seq.filter (fun x -> x.Name = name) |> Seq.head

    /// Parses a XmlNode
    ///
    /// ## Parameters
    ///  - `name` - The node name
    ///  - `f` - The parser function to use
    ///  - `node` - The XML node to parse
    let parse name f (node: #XmlNode) =
        if node.Name = name then
            f node
        else
            failwithf "Could not parse %s - Node was %s" name node.Name

    /// Parses a XML sub node
    ///
    /// ## Parameters
    ///  - `name` - The sub node name
    ///  - `f` - The parser function to use
    let parseSubNode name f = getSubNode name >> parse name f

    /// Loads the given text into a XmlDocument
    ///
    /// ## Parameters
    ///  - `text` - The XML text
    let createDoc text =
        if String.isNullOrEmpty text then
            null
        else
            let xmlDocument = XmlDocument()
            xmlDocument.LoadXml text
            xmlDocument

    /// Gets the DocumentElement of the XmlDocument
    ///
    /// ## Parameters
    ///  - `doc` - The XmlDocument object
    let getDocElement (doc: XmlDocument) = doc.DocumentElement

    /// Replaces text in the XML document specified by a XPath expression.
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression to use to select node
    ///  - `value` - The new value
    ///  - `doc` - The XML document to search on
    let replaceXPath xpath value (doc: XmlDocument) =
        let node = doc.SelectSingleNode xpath

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.Value <- value
            doc

    /// Replaces the inner text of an xml node in the XML document specified by a XPath expression.
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression to use to select node
    ///  - `innerTextValue` - The new value
    ///  - `doc` - The XML document to search on
    let replaceXPathInnerText xpath innerTextValue (doc: XmlDocument) =
        let node = doc.SelectSingleNode xpath

        if isNull node then
            failwithf "XML node '%s' not found" xpath
        else
            node.InnerText <- innerTextValue
            doc

    /// Replaces the value of attribute in an xml node in the XML document specified by a XPath expression.
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression to use to select node
    ///  - `attribute` - The attribute name
    ///  - `innerTextValue` - The new attribute value
    ///  - `doc` - The XML document to search on
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

    /// Selects a xml node value via XPath from the given document
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression to use to select node
    ///  - `namespaces` - The namespaces list
    ///  - `doc` - The XML document to search on
    let selectXPathValue xpath (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.DocumentElement.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.InnerText

    /// Selects a xml node attribute value via XPath from the given document
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression to use to select node
    ///  - `attribute` - The attribute name
    ///  - `namespaces` - The namespaces list
    ///  - `doc` - The XML document to search on
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

    /// Selects a xml node via XPath from the given document
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression to use to select node
    ///  - `namespaces` - The namespaces list
    ///  - `doc` - The XML document to search on
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

    /// Save the given XmlDocument to file path
    ///
    /// ## Parameters
    ///  - `fileName` - The output file name
    ///  - `doc` - The XML document to save
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

    /// Loads the given file path into a XmlDocument
    ///
    /// ## Parameters
    ///  - `path` - The file path to load an XmlDocument from
    let loadDoc (path: string) =
        if String.isNullOrEmpty path then
            null
        else
            let xmlDocument = XmlDocument()
            load path xmlDocument
            xmlDocument

    /// Replaces text in a XML file at the location specified by a XPath expression.
    ///
    /// ## Parameters
    ///  - `fileName` - The file name to use
    ///  - `xpath` - The XPath expression used to select text
    ///  - `value` - The new value
    let poke (fileName: string) xpath value =
        let doc = XmlDocument()
        load fileName doc
        replaceXPath xpath value doc |> saveDoc fileName

    /// Replaces the inner text of an xml node in a XML file at the location specified by a XPath expression.
    ///
    /// ## Parameters
    ///  - `fileName` - The file name to use
    ///  - `xpath` - The XPath expression used to select text
    ///  - `innerTextValue` - The new value
    let pokeInnerText (fileName: string) xpath innerTextValue =
        let doc = XmlDocument()
        load fileName doc
        replaceXPathInnerText xpath innerTextValue doc |> saveDoc fileName

    /// Replaces text in a XML document specified by a XPath expression, with support for namespaces.
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression used to select text
    ///  - `value` - The new value
    ///  - `fileName` - The file name to use
    ///  - `namespaces` - The namespaces list
    ///  - `doc` - The XmlDocument object
    let replaceXPathNS xpath value (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.Value <- value
            doc

    /// Replaces inner text in a XML document specified by a XPath expression, with support for namespaces.
    ///
    /// ## Parameters
    ///  - `xpath` - The XPath expression used to select text
    ///  - `innerTextValue` - The new value
    ///  - `fileName` - The file name to use
    ///  - `namespaces` - The namespaces list
    ///  - `doc` - The XmlDocument object
    let replaceXPathInnerTextNS xpath innerTextValue (namespaces: #seq<string * string>) (doc: XmlDocument) =
        let nsmgr = XmlNamespaceManager(doc.NameTable)
        namespaces |> Seq.iter nsmgr.AddNamespace
        let node = doc.SelectSingleNode(xpath, nsmgr)

        if node = null then
            failwithf "XML node '%s' not found" xpath
        else
            node.InnerText <- innerTextValue
            doc

    /// Replaces text in a XML file at the location specified by a XPath expression, with support for namespaces.
    ///
    /// ## Parameters
    ///  - `fileName` - The file name to use
    ///  - `namespaces` - The namespaces list
    ///  - `xpath` - The XPath expression used to select text
    ///  - `value` - The new value
    let pokeNS (fileName: string) namespaces xpath value =
        let doc = XmlDocument()
        load fileName doc
        replaceXPathNS xpath value namespaces doc |> saveDoc fileName

    /// Replaces inner text of an xml node in a XML file at the location specified by a XPath expression, with support for namespaces.
    ///
    /// ## Parameters
    ///  - `fileName` - The file name to use
    ///  - `namespaces` - The namespaces list
    ///  - `xpath` - The XPath expression used to select text
    ///  - `innerTextValue` - The new value
    let pokeInnerTextNS (fileName: string) namespaces xpath innerTextValue =
        let doc = XmlDocument()
        load fileName doc
        replaceXPathInnerTextNS xpath innerTextValue namespaces doc |> saveDoc fileName

    /// Loads the given text into a XslCompiledTransform.
    ///
    /// ## Parameters
    ///  - `text` - The text to use
    let XslTransformer text =
        if String.isNullOrEmpty text then
            null
        else
            let xslCompiledTransform = XslCompiledTransform()
            createDoc text |> xslCompiledTransform.Load
            xslCompiledTransform

    /// Transforms a XmlDocument using a XslCompiledTransform.
    ///
    /// ## Parameters
    ///  - `xsl` - The XslCompiledTransform which should be applied.
    ///  - `doc` - The XmlDocument to transform.
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

    /// Transforms a XML file using a XSL stylesheet file.
    ///
    /// ## Parameters
    ///  - `stylesheetUri` - The Uri for the XSL stylesheet file.
    ///  - `fileName` - The XML file to transform.
    let XmlTransform (stylesheetUri: string) (fileName: string) =
        let doc = XmlDocument()
        doc.Load fileName
        let xsl = XslCompiledTransform()
        xsl.Load stylesheetUri
        XslTransform xsl doc |> saveDoc fileName
