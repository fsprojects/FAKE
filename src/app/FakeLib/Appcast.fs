/// Contains code to configure FAKE for Appcast handling
module Fake.Appcast
open System.Xml
open System.Xml.Linq


//private XMLHelper inspired by https://nbevans.wordpress.com/2015/04/15/super-skinny-xml-document-generation-with-f/
let private XDeclaration version encoding standalone = XDeclaration(version, encoding, standalone)
let private XName expandedName = XName.Get(expandedName)
let private XDocument xdecl content = XDocument(xdecl, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray)
let private XElement expandedName content = XElement(XName expandedName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj
let private XAttribute expandedName value = XAttribute(XName expandedName, value) :> obj
let private XAttributeXName expandedName value = Linq.XAttribute(expandedName, value) :> obj

/// Sparkel namespace used for RSS extension
let private sparkle = XNamespace.Get("http://www.andymatuschak.org/xml-namespaces/sparkle")

/// Mime type of the download file
type MimeType =
    /// Octetstream use for exe or zip files
    | OctetStream
    /// Custom mimetype
    | Custom of string

/// Download details for the appcast
type AppcastItem = {
    /// The name of the update
    title : string;
    /// Date when update is published
    pubdate : System.DateTime;
    /// URI where the update files are found
    url : System.Uri;
    /// Machine readable version number used to determine if an update is available by the client (should follow semver)
    version : string;
    /// Optional human readable version number. This will be shown to the user if present otherwise
    /// the technical version number will be used
    shortVersion : string option;
    /// Mime type of the update file, usualy octetstream
    mimetype : MimeType
    /// Optional DSA signature for the archive. It is recommended to use this if the app itself is not signed
    dsaSignature : string option;
    /// Optional miminal system version for the update
    minimumSystemVersion : string option;
    /// Length of the file in bytes
    length : int64;
}

/// Configuration data for the appcast
type Appcast = {
    /// A titel, usually the app name
    title : string;
    /// Short description
    description : string;
    /// Language of your app
    language : string;
    /// Updates published to client, can habe multiple updates e.g. for different OS versions
    items : AppcastItem list;
}

/// writes an appcast to a file
/// ##Parameters
///
///  - `path` - The file where the appcast should be written
///  - `cast` - The appcast to write
/// ##Sample
///  // This target creates the app cast for our app. I contains two version 1.X and 2.X while 2.X requires at least OS X 10.10 Yosemite.
///  Target "CreateAppcast" (fun _ ->
///      let server = "https://example.com/files/"
///      let fileLength file =
///           let info = new System.IO.FileInfo(file)
///           info.Length
///           
///      let latestSize = fileLength "build/download-2.0.1.zip"
///      let legacySize = fileLength "build/download-1.1.4.zip"
///      
///      { 
///          title = "My Awesome App"
///          description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec a diam lectus."
///          language = "en"
///          items = [
///                      {
///                          title = "Hawk Nickel Greyhound"
///                          pubdate = System.DateTime.Now
///                          url = new System.Uri(server + "download-2.0.1.zip")
///                          version = "2014" // This our internal build number
///                          shortVersion = Some("2.0.1") //This is what we show to the user
///                          mimetype = OctetStream
///                          minimumSystemVersion = Some("10.10")
///                          length = latestSize
///                          dsaSignature = None
///                      };
///                      {
///                          title = "Sparrow Platinum Beagle"
///                          pubdate = System.DateTime.Now
///                          url = new System.Uri(server + "download-1.1.4.zip")
///                          version = "1142" // This our internal build number
///                          shortVersion = Some("1.1.4") //This is what we show to the user
///                          mimetype = OctetStream
///                          length = legacySize
///                          minimumSystemVersion = None
///                          dsaSignature = None
///                      }
///          ]
///      } |> writeAppcast "build/updates.xml"
///  )
let writeAppcast (path : string) (cast : Appcast) = 
    let toXml (cast : Appcast) =
        let mtToString mimetype = 
            match mimetype with 
                | OctetStream -> "application/octet-stream"
                | Custom(s) -> s
        let choose a b =
            match a with 
                | Some(c) -> c
                | None -> b
        let appendSome option consequence content =
            match option with
                | Some(data) -> (consequence data) :: content
                | None -> content

        
        let item (e : AppcastItem) = 
            let appendMinimumVersion = appendSome e.minimumSystemVersion (fun d ->  XAttributeXName (sparkle + "minimumSystemVersion") d)
            let appendSig = appendSome e.dsaSignature (fun d ->  XAttributeXName (sparkle + "dsaSignature") d)

            XElement "item" [
                XElement "title" e.title
                XElement "pubDate" (e.pubdate.ToString("r"))
                XElement "enclosure" (
                    [
                        XAttribute "url" e.url
                        XAttributeXName (sparkle + "version") e.version
                        XAttribute "type" (mtToString e.mimetype)
                        XAttribute "length" e.length
                        XAttributeXName (sparkle + "shortVersionString") (choose e.shortVersion e.version)
                    ] 
                |> appendMinimumVersion
                |> appendSig)
            ]

        let doc = XDocument (XDeclaration "1.0" "UTF-8" "no") [
                    XElement "rss" 
                        [
                            XAttribute "version" "2.0"
                            XAttributeXName (XNamespace.Xmlns + "sparkle") "http://www.andymatuschak.org/xml-namespaces/sparkle"
                            XAttributeXName (XNamespace.Xmlns + "dc") "http://purl.org/dc/elements/1.1/"
                            XElement "channel" ([
                                                    XElement "title" cast.title
                                                    XElement "description" cast.description
                                                    XElement "language" cast.language] 
                            @ List.map item cast.items)
                        ]
        ]
        doc
    let xml = toXml cast
    use writer = XmlWriter.Create(path)
    xml.Save(writer)