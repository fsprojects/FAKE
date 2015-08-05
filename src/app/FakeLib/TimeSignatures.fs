[<AutoOpen>]
module TimeSignatures

/// Timestamps management with optional MD5 hashing

open System.Diagnostics
open System
open System.IO
open System.Xml
open System.Security.Cryptography
open Fake
open Fake.FileHelper
open Fake.ConfigurationHelper


/// [omit]
type GlobalXML = {
    mutable xml_conf: XmlDocument;
    mutable xml_name: string
}

/// [omit]
let private global_state = { xml_conf=null; xml_name="" }

/// [omit]
let mbox = MailboxProcessor<unit>.Start(fun inbox ->
                                              async { 
                                              while true do
                                                  let! _ = inbox.Receive()
                                                  global_state.xml_conf.Save(global_state.xml_name)})

/// Initialize an XML config file
/// ## Parameters
///  - `filename` - the name of the config file
///
/// ## Sample
///
///     let xml = initTS "my_conf.xml"
let initTSwithName (filename: string) : XmlDocument =
    
    if (not <| File.Exists(filename)) then
        let xml' = new XmlDocument()
        xml'.LoadXml("<files></files>")
        writeConfig filename xml'
        global_state.xml_conf <- xml'
    else
        global_state.xml_conf <- readConfig filename
    global_state.xml_name <- filename
    mbox.Post()
    global_state.xml_conf


/// Initialize an XML config file
/// ## Sample
///
///     let xml = initTS
let initTS : XmlDocument =
    global_state.xml_name <- ".state.xml"
    initTSwithName global_state.xml_name


/// [omit]
type TimeStamp = int64

/// [omit]
let inline private toTimeStamp(time: string) : TimeStamp = 
    int64(time)
    
/// [omit]
let private zeroTimeStamp = int64(0)


/// [omit]
type MD5Hash = byte[]

/// [omit]
let inline private toMD5Hash(signature: string) : MD5Hash = 
    Convert.FromBase64String(signature)

/// Auxiliary function to convert a signature to a string
/// ## Parameters
///  - `signature` - the signature to be converted
let inline md5ToString(signature: MD5Hash) : string = 
    Convert.ToBase64String(signature)

/// [omit]
let private zeroSignature = Convert.FromBase64String("")

/// A structure to keep timestamps and MD5 signatures
type TimeSignature = {
     timestamp: TimeStamp
     signature: MD5Hash;
}


/// [omit]
type GetTSFun = string -> TimeSignature * XmlElement


/// Get the timestamp recorded in the configuration file
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    let {timestamp=timestamp; signature=_} = getConfigTimestamp "filename"
///    printfn "The recorded timestamp for '%s' is %s" "filename" (timestamp.ToString())
let getConfigTimestamp (filename: string) : ((TimeSignature * XmlElement)) =
    traceFAKE "Getting the timestamp for file '%s'" filename
    let node = global_state.xml_conf.SelectSingleNode (sprintf "files/file[@name='%s']" filename) :?> XmlElement
    if node = null then
        let now = DateTime.Now.Ticks
        traceFAKE "New file: setting timestamp for file '%s' to %s" filename (now.ToString())
        ({timestamp=now; signature=zeroSignature}, null)
    else
        let ts = node.GetAttribute("timestamp")
        ({timestamp=toTimeStamp(ts); signature=zeroSignature}, node)    


/// Get the MD5 signature recorded in the configuration file
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    let {timestamp=_; signature=md5} = getConfigTimestamp "filename"
///    printfn "The recorded MD5 signature for '%s' is %s" "filename" (md5ToString(md5))
let getConfigMD5 (filename: string) : ((TimeSignature * XmlElement)) =
    traceFAKE "Getting the MD5 for file '%s'" filename
    let node = global_state.xml_conf.SelectSingleNode (sprintf "files/file[@name='%s']" filename) :?> XmlElement
    if node = null then
        use md5Hash = MD5.Create()  
        let md5 = md5Hash.ComputeHash(File.ReadAllBytes(filename))
        traceFAKE "New file: setting MD5 for file '%s' to %s" filename (md5ToString(md5))
        ({timestamp=zeroTimeStamp; signature=md5}, null)
    else
        let md5 = node.GetAttribute("md5")
        ({timestamp=zeroTimeStamp; signature=toMD5Hash(md5)}, node)


/// [omit]
let private updateOrAddConfigNode (filename: string) (value: TimeSignature) (node: XmlElement) =
    match node with
    | null ->
        traceFAKE "Creating a XML node for file '%s'" filename
        let node' = global_state.xml_conf.CreateElement("file")

        let att = global_state.xml_conf.CreateAttribute("name")
        att.Value <- filename
        node'.SetAttributeNode(att) |> ignore

        let att1 = global_state.xml_conf.CreateAttribute("timestamp")
        att1.Value <- value.timestamp.ToString()
        node'.SetAttributeNode(att1) |> ignore

        let att2 = global_state.xml_conf.CreateAttribute("md5")
        att2.Value <- md5ToString(value.signature)
        node'.SetAttributeNode(att2) |> ignore

        global_state.xml_conf.FirstChild.AppendChild(node') |> ignore  
    | node ->
        traceFAKE "Updating the XML node for file '%s' to timestamp=%s" filename (value.timestamp.ToString())
        node.SetAttribute("timestamp", value.timestamp.ToString())
        if value.signature <> zeroSignature then
            traceFAKE "\tand md5=%s" (md5ToString(value.signature))
            node.SetAttribute("md5", md5ToString(value.signature))  


/// [omit]
let rec private updateOrAddConfigNodeList (filenames: string list) (values: TimeSignature list) (nodes: XmlElement list) =
    assert (filenames.Length = values.Length)
    assert (values.Length = nodes.Length)
    match filenames with
    | [] ->
        mbox.Post()
    | filename :: tail_filenames ->
        let value :: tail_values = values // same length, therefore it will never be []
        let node :: tail_nodes = nodes // same length, therefore it will never be []
        updateOrAddConfigNode filename value node
        updateOrAddConfigNodeList tail_filenames tail_values tail_nodes

/// [omit]
let getTimesignature (filename: string) (updateMD5: bool) : TimeSignature =
    if File.Exists(filename) then
        let file_md5 = 
            if updateMD5 then
                use md5Hash = MD5.Create()  
                md5Hash.ComputeHash(File.ReadAllBytes(filename))
            else
                zeroSignature
        {timestamp=File.GetLastWriteTime(filename).Ticks; signature=file_md5}
    else
        {timestamp=zeroTimeStamp; signature=zeroSignature}
          

/// Check whether a file is up-to-dated
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    if isUpToDateTS "filename" then
///        printfn "%s is not up-to-date" "filename"
///    else
///        printfn "%s is up-to-date" "filename"
let isUpToDateTimestamp (filename: string) : bool =
    let ({timestamp=timestamp; signature=_}, _) = getConfigTimestamp filename
    let {timestamp=file_timestamp;signature=_} = getTimesignature filename false
    timestamp = file_timestamp


/// Check whether a file is up-to-dated (based on timestamps AND MD5 signature)
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    if isUpToDateMD5 "filename" then
///        printfn "%s is not up-to-date" "filename"
///    else
///        printfn "%s is up-to-date" "filename"
let isUpToDateMD5 (filename: string) : bool =
    if not(isUpToDateTimestamp filename) then
        let ({timestamp=_; signature=md5}, _) = getConfigMD5 filename
        let {timestamp=_;signature=file_md5} = getTimesignature filename true
        md5 = file_md5
    else
       true // timestamps are identical => files are identical (cheaper)


/// [omit]
let rec private unzip5' (l: ('a * 'b * 'c * 'd * 'e) list) (accum: 'a list * 'b list * 'c list * 'd list * 'e list) : ('a list * 'b list * 'c list * 'd list * 'e list) =
    match l with
    | [] ->
        accum
    | head_l :: tail_l ->
        let (head_st, head_nd, head_rd, head_th, head_th') = head_l
        let (tail_st, tail_nd, tail_rd, tail_th, tail_th') = accum
        let accum = (head_st :: tail_st, head_nd :: tail_nd, head_rd :: tail_rd, head_th :: tail_th, head_th' :: tail_th')
        unzip5' tail_l accum

/// [omit]
let private unzip5 (l: ('a * 'b * 'c * 'd * 'e) list) : ('a list * 'b list * 'c list * 'd list * 'e list) =
    let (a, b, c, d, e) = unzip5' l ([], [], [], [], [])
    (List.rev a, List.rev b, List.rev c, List.rev d, List.rev e)

/// [omit]
let inline private fst5  ((a, _, _, _, _): ('a * 'b * 'c * 'd * 'e)) : 'a = a
    
let inline private check_if_uptodate (value: TimeSignature) (file_value: TimeSignature) : bool = 
    (((file_value.signature <> zeroSignature) && (value.signature = file_value.signature)) 
    || ((file_value.signature = zeroSignature) && (value.timestamp = file_value.timestamp)))

/// [omit]
let private updateTS (filenames: string list) (getConfigTS: GetTSFun) (last_timestamp: TimeStamp list option) : (string list * TimeSignature list) option =
    let composite_values = 
        filenames 
        |> List.map (fun filename ->
                         let (value, node) = getConfigTS filename
                         let file_value = getTimesignature filename (last_timestamp <> None)
                         let uptodate = check_if_uptodate value file_value
                         (uptodate, filename, node, value, file_value))

    let changed_items = composite_values |> List.filter (fun item -> item |> fst5 |> not)
    let (_, filenames, nodes, _, file_values) = unzip5 changed_items

    updateOrAddConfigNodeList filenames file_values nodes
    Some(filenames, file_values)

/// Update the MD5 timestamps of a list of files. Return a list of the changed files
/// ## Parameters
///  - `filenames` - a list with the names of the files
///
/// ## Sample
///
///    let changed_files = updateTimestamps filenames
///    if changed_files.Length > 0 then
///        // process changed_files
let updateTimestamps (filenames: string list) : string list =
    match updateTS filenames getConfigTimestamp None with
    | Some((changed_files, _)) ->
        changed_files
    | None ->
        []


/// Update the MD5 signature of a list of files; first check their timestamps and, if they are the same, MD5s are not computed (more efficient). Return a list of the changed files
/// ## Parameters
///  - `filename` - a list with the names of the files
///
/// ## Sample
///
///    let changed_files = updateMD5 filenames
///    if changed_files.Length > 0 then
///        // process changed_files
let updateMD5 (filenames: string list) : string list =    
    match updateTS filenames getConfigTimestamp None with
    | Some((changed_files, ts)) ->
        /// MD5 is heavy, therefore just check small files (< 1024KB ~)
        if (List.exists (fun filename -> File.Exists(filename) && FileInfo(filename).Length >= int64(10e6)) filenames) then
            traceFAKE "A file is greater than 10MB and so the MD5 signatures have been deactivated"
            changed_files
        else
            let timestamps_option = Some(List.map (fun {timestamp=timestamp; signature=_} -> timestamp) ts)
            match updateTS changed_files getConfigMD5 timestamps_option with
            | Some((changed_files, ts)) ->
                changed_files
            | None ->
                []
    | None ->
        []
