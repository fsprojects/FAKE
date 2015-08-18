[<AutoOpen>]
module TimeSignatures

/// Timestamps management with CRC32 and optional MD5 hashing

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
type CRC32 = string

/// [omit]
let private zeroCRC32 = ""


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
let private zeroMD5 = Convert.FromBase64String("")

/// A structure to keep timestamps and MD5 signatures
type TimeSignature = {
     timestamp: TimeStamp;
     crc32: CRC32;
     md5: MD5Hash;
}


/// [omit]
type GetTSFun = string -> TimeSignature * XmlElement


/// Get the timestamp recorded in the configuration file
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    let {timestamp=timestamp; crc32=_; md5=_} = getConfigTimestamp "filename"
///    printfn "The recorded timestamp for '%s' is %s" "filename" (timestamp.ToString())
let getConfigTimestamp (filename: string) : ((TimeSignature * XmlElement)) =
    logVerbosefn "Getting the timestamp for file '%s'" filename
    let node = global_state.xml_conf.SelectSingleNode (sprintf "files/file[@name='%s']" filename) :?> XmlElement
    if node = null then
        let now = DateTime.Now.Ticks
        logVerbosefn "New file: setting timestamp for file '%s' to %s" filename (now.ToString())
        ({timestamp=now; crc32=zeroCRC32; md5=zeroMD5}, null)
    else
        let ts = node.GetAttribute("timestamp")
        ({timestamp=toTimeStamp(ts); crc32=zeroCRC32; md5=zeroMD5}, node)    

/// [omit]
let inline private computeCRC32 (filename: string) : string =
    let hasher = HashLib.HashFactory.Checksum.CreateCRC32a()
    hasher.ComputeBytes(File.ReadAllBytes(filename)).ToString()

/// Get the CRC32 signature recorded in the configuration file
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    let {timestamp=_; cr32=crc32; md5=_} = getConfigTimestamp "filename"
///    printfn "The recorded CRC32 signature for '%s' is %s" "filename" crc32
let getConfigCRC32 (filename: string) : ((TimeSignature * XmlElement)) =
    logVerbosefn "Getting the CRC32 for file '%s'" filename
    let node = global_state.xml_conf.SelectSingleNode (sprintf "files/file[@name='%s']" filename) :?> XmlElement
    if node = null then
        let crc32 = computeCRC32 filename
        logVerbosefn "New file: setting CRC32 for file '%s' to %s" filename crc32
        ({timestamp=zeroTimeStamp; crc32=crc32; md5=zeroMD5}, null)
    else
        let crc32 = node.GetAttribute("crc32")
        ({timestamp=zeroTimeStamp; crc32=crc32; md5=zeroMD5}, node)


let inline private computeMD5 (filename: string) : byte[] =
    use md5Hash = MD5.Create()  
    md5Hash.ComputeHash(File.ReadAllBytes(filename))


/// Get the MD5 signature recorded in the configuration file
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    let {timestamp=_; crc32=_; md5=md5} = getConfigTimestamp "filename"
///    printfn "The recorded MD5 signature for '%s' is %s" "filename" (md5ToString(md5))
let getConfigMD5 (filename: string) : ((TimeSignature * XmlElement)) =
    logVerbosefn "Getting the MD5 for file '%s'" filename
    let node = global_state.xml_conf.SelectSingleNode (sprintf "files/file[@name='%s']" filename) :?> XmlElement
    if node = null then
        let md5 = computeMD5 filename
        logVerbosefn "New file: setting MD5 for file '%s' to %s" filename (md5ToString(md5))
        ({timestamp=zeroTimeStamp; crc32=zeroCRC32; md5=md5}, null)
    else
        let md5 = node.GetAttribute("md5")
        ({timestamp=zeroTimeStamp; crc32=zeroCRC32; md5=toMD5Hash(md5)}, node)


/// [omit]
let private updateOrAddConfigNode (filename: string) (value: TimeSignature) (node: XmlElement) =
    match node with
    | null ->
        logVerbosefn "Creating a XML node for file '%s'" filename
        let node' = global_state.xml_conf.CreateElement("file")

        let att = global_state.xml_conf.CreateAttribute("name")
        att.Value <- filename
        node'.SetAttributeNode(att) |> ignore

        let att1 = global_state.xml_conf.CreateAttribute("timestamp")
        att1.Value <- value.timestamp.ToString()
        node'.SetAttributeNode(att1) |> ignore

        let att2 = global_state.xml_conf.CreateAttribute("crc32")
        att2.Value <- value.crc32
        node'.SetAttributeNode(att2) |> ignore

        let att3 = global_state.xml_conf.CreateAttribute("md5")
        att3.Value <- md5ToString(value.md5)
        node'.SetAttributeNode(att3) |> ignore

        global_state.xml_conf.FirstChild.AppendChild(node') |> ignore  
    | node ->
        logVerbosefn "Updating the XML node for file '%s' to timestamp=%s" filename (value.timestamp.ToString())
        node.SetAttribute("timestamp", value.timestamp.ToString())
        if value.crc32 <> zeroCRC32 then
            logVerbosefn "\tand crc32=%s" (value.crc32)
            node.SetAttribute("crc32", value.crc32)
        if value.md5 <> zeroMD5 then
            logVerbosefn "\tand md5=%s" (md5ToString(value.md5))
            node.SetAttribute("md5", md5ToString(value.md5))  


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
let getTimesignature (filename: string) (updateCRC32: bool) (updateMD5: bool) : TimeSignature =
    if File.Exists(filename) then
        let file_crc32 = 
            if updateCRC32 then
                computeCRC32 filename
            else
                zeroCRC32
        let file_md5 = 
            if updateMD5 then
                computeMD5 filename
            else
                zeroMD5
        {timestamp=File.GetLastWriteTime(filename).Ticks; crc32=file_crc32; md5=file_md5}
    else
        {timestamp=zeroTimeStamp; crc32=zeroCRC32; md5=zeroMD5}
          

/// Check whether a file is up-to-date using its write time
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
    let ({timestamp=timestamp; crc32=_; md5=_}, _) = getConfigTimestamp filename
    let {timestamp=file_timestamp; crc32=_; md5=_} = getTimesignature filename false false
    timestamp = file_timestamp


/// Check whether a file is up-to-date using its CRC32 signature
/// ## Parameters
///  - `filename` - the name of the file
///
/// ## Sample
///
///    if isUpToDateCRC32 "filename" then
///        printfn "%s is not up-to-date" "filename"
///    else
///        printfn "%s is up-to-date" "filename"
let isUpToDateCRC32 (filename: string) : bool =
    let ({timestamp=_; crc32=crc32; md5=_}, _) = getConfigCRC32 filename
    let {timestamp=_; crc32=file_crc32; md5=_} = getTimesignature filename true false
    crc32 = file_crc32


/// Check whether a file is up-to-date using its CRC32 AND/OR MD5 signature(s)
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
    let ({timestamp=_; crc32=_; md5=md5}, _) = getConfigMD5 filename
    let {timestamp=_; crc32=_; md5=file_md5} = getTimesignature filename false true
    md5 = file_md5


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

/// [omit]    
let inline private check_if_uptodate (value: TimeSignature) (file_value: TimeSignature) : bool = 
    (((file_value.md5 <> zeroMD5) && (value.md5 = file_value.md5))
        || ((file_value.md5 = zeroMD5) && (value.crc32 = file_value.crc32))
        || ((file_value.crc32 = zeroCRC32) && (value.timestamp = file_value.timestamp)))

/// [omit]
let private updateTS (filenames: string list) (getConfigTS: GetTSFun) : (string list * TimeSignature list) option =
    let composite_values = 
        filenames 
        |> List.map (fun filename ->
                         let (value, node) = getConfigTS filename
                         let file_value = getTimesignature filename (value.crc32 <> zeroCRC32) (value.md5 <> zeroMD5)
                         let uptodate = check_if_uptodate value file_value
                         (uptodate, filename, node, value, file_value))

    let changed_items = composite_values |> List.filter (fun item -> item |> fst5 |> not)
    let (_, filenames, nodes, _, file_values) = unzip5 changed_items

    updateOrAddConfigNodeList filenames file_values nodes
    Some(filenames, file_values)

/// Update the timestamps of a list of files. Return a list of the changed files
/// ## Parameters
///  - `filenames` - a list with the names of the files
///
/// ## Sample
///
///    let changed_files = updateTimestamps filenames
///    if changed_files.Length > 0 then
///        // process changed_files
let updateTimestamps (filenames: string list) : string list =
    match updateTS filenames getConfigTimestamp with
    | Some((changed_files, _)) ->
        changed_files
    | None ->
        []

/// Update the CRC32 signatures of a list of files. Return a list of the changed files
/// ## Parameters
///  - `filenames` - a list with the names of the files
///
/// ## Sample
///
///    let changed_files = updateCRC32 filenames
///    if changed_files.Length > 0 then
///        // process changed_files
let updateCRC32 (filenames: string list) : string list =
    match updateTS filenames getConfigCRC32 with
    | Some((changed_files, _)) ->
        changed_files
    | None ->
        []

/// Update the MD5 signatures of a list of files. Return a list of the changed files
/// ## Parameters
///  - `filenames` - a list with the names of the files
///
/// ## Sample
///
///    let changed_files = updateMd5 filenames
///    if changed_files.Length > 0 then
///        // process changed_files
let updateMD5 (filenames: string list) : string list =
    match updateTS filenames getConfigMD5 with
    | Some((changed_files, _)) ->
        changed_files
    | None ->
        []
