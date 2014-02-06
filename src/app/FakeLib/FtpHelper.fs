/// Contains helpers which allow to upload a whole folder/specific file into a Ftp Server. 
/// Uses `Passive Mode` FTP and handles all files as binary (and not ASCII)
/// Assumes direct network connectivity to destination Ftp server (not via a proxy)
/// Does not support FTPS and SFTP
module Fake.FtpHelper

open System
open System.IO
open System.Net
open System.Text.RegularExpressions

type FtpServerInfo = 
    { Server : string
      Request : FtpWebRequest }

/// Gets a connection to the Ftp server
let private getServerInfo (serverNameIp : string) (user : string) (pwd : string) ftpMethod = 
    let ftpRequest = (WebRequest.Create serverNameIp) :?> FtpWebRequest
    ftpRequest.Credentials <- new NetworkCredential(user, pwd)
    ftpRequest.Method <- ftpMethod
    { Server = serverNameIp
      Request = ftpRequest }

/// Writes given byte array into the given stream
let rec private writeChunkToReqStream (chunk : byte []) (reqStrm : Stream) (br : BinaryReader) = 
    if chunk.Length <> 0 then 
        reqStrm.Write(chunk, 0, chunk.Length)
        writeChunkToReqStream (br.ReadBytes 1024) reqStrm |> ignore

let inline private getSubstring (fromPos : int) (str : string) (toPos : int) = str.Substring(fromPos, toPos)
let inline private lastSlashPos (str : string) = str.LastIndexOf(@"\") + 1

///Partial validation for folder name, based on http://msdn.microsoft.com/en-us/library/aa365247.aspx
let inline private dirNameIsValid (dirName : string) = 
    let invalidChars = [ "<"; ">"; ":"; "\""; "/"; "\\"; "|"; "?"; "*" ]
    let invalidNames = 
        [ "CON"; "PRN"; "AUX"; "NUL"; "COM1"; "COM2"; "COM3"; "COM4"; "COM5"; "COM6"; "COM7"; "COM8"; "COM9"; "LPT1"; 
          "LPT2"; "LPT3"; "LPT4"; "LPT5"; "LPT6"; "LPT7"; "LPT8"; "LPT9" ]
    let invalid1 = List.exists (fun s -> s = dirName.ToUpper()) invalidNames
    let invalid2 = List.exists (fun s -> dirName.ToUpper().StartsWith(sprintf "%s." s)) invalidNames
    let invalid3 = dirName.EndsWith(" ")
    let invalid4 = dirName.EndsWith(".")
    let invalid5 = List.exists (fun s -> dirName.Contains(s)) invalidChars
    not (invalid1 && invalid2 && invalid3 && invalid4 && invalid5)

/// Checks to see if the `ftp content` string containts the string `<DIR>    Given_Folder_Name`
let inline regexCheck fname ftpContents = Regex.IsMatch(ftpContents, (sprintf @"\<DIR\>\s*%s\s+" fname))

/// Gets the contents/listing of files and folders in a given ftp server folder
/// ## Parameters
///  - `dirPath` - The full name of folder whose content need to be listed
///  - `server` - Ftp Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - Ftp Server login name (ex: "joebloggs")
///  - `pwd`Ftp Server login password (ex: "J0Eblogg5")
let getFtpDirContents (server : string) (user : string) (pwd : string) (dirPath : string) = 
    logfn "getting ftp dir contents for %s" dirPath
    dirPath
    |> fun d -> getServerInfo (sprintf "%s/%s" server d) user pwd WebRequestMethods.Ftp.ListDirectoryDetails
    |> fun si -> 
        use response = (si.Request.GetResponse() :?> FtpWebResponse)
        use responseStream = response.GetResponseStream()
        use reader = new StreamReader(responseStream)
        reader.ReadToEnd()

/// Uploads a single file from local directory into remote Ftp folder
/// ## Parameters
///  - `destPath` - The full local file path that needs to be uploaded
///  - `srcPath` - The full path to file which needs to be created, including all its parent folders
///  - `server` - Ftp Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - Ftp Server login name (ex: "joebloggs")
///  - `pwd` - Ftp Server login password (ex: "J0Eblogg5")
let uploadAFile (server : string) (user : string) (pwd : string) (destPath : string) (srcPath : string) = 
    logfn "upload %s from to %s" srcPath destPath
    let fl = new FileInfo(srcPath)
    if (fl.Length <> 0L) then 
        destPath
        |> fun d -> getServerInfo (sprintf "%s/%s" server d) user pwd WebRequestMethods.Ftp.UploadFile
        |> fun si -> 
            use fs = new FileStream(srcPath, FileMode.Open, FileAccess.Read)
            use br = new BinaryReader(fs, new System.Text.UTF8Encoding())
            use reqStrm = si.Request.GetRequestStream()
            writeChunkToReqStream (br.ReadBytes 1024) reqStrm br

/// Given a folder name, will check if that folder is present at a given root directory of a ftp server
/// ## Parameters
///  - `server` - Ftp Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - Ftp Server login name (ex: "joebloggs")
///  - `pwd` - Ftp Server login password (ex: "J0Eblogg5")
let private checkInExistingDirList server user pwd destPath fname = 
    destPath
    |> lastSlashPos
    |> getSubstring 0 destPath
    |> getFtpDirContents server user pwd
    |> regexCheck fname

/// Given a folder path, will check if that folder is present at a given root directory of a ftp server
/// ## Parameters
///  - `destPath` - The full name of folder which needs to be checked for existance, including all its parent folders
///  - `server` - Ftp Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - Ftp Server login name (ex: "joebloggs")
///  - `pwd` - Ftp Server login password (ex: "J0Eblogg5")
let isFolderPresent server user pwd (destPath : string) = 
    destPath
    |> lastSlashPos
    |> destPath.Substring
    |> checkInExistingDirList server user pwd destPath

/// Creates a matching folder in ftp folder, if not already present
/// ## Parameters
///  - `destPath` - The full name of folder which needs to be created, including all its parent folders
///  - `server` - Ftp Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - Ftp Server login name (ex: "joebloggs")
///  - `pwd` - Ftp Server login password (ex: "J0Eblogg5")
let createAFolder (server : string) (user : string) (pwd : string) (destPath : string) = 
    logfn "folder to create=%s" destPath
    if not ((String.IsNullOrEmpty destPath) || (isFolderPresent server user pwd destPath)) then 
        destPath
        |> fun d -> getServerInfo (sprintf "%s/%s" server d) user pwd WebRequestMethods.Ftp.MakeDirectory
        |> fun si -> 
            use response = (si.Request.GetResponse() :?> FtpWebResponse)
            logfn "create folder status = %s" (response.StatusDescription)

/// Uploads a given local folder to a given root dir on a Ftp server
/// ## Parameters
///  - `srcPath` - The local server path from which files need to be uploaded
///  - `rootDir` - The remote root dir where files need to be uploaded, leave this as empty, if files need to be uploaded to root dir of ftp server
///  - `server` - Ftp Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - Ftp Server login name (ex: "joebloggs")
///  - `pwd` - Ftp Server login password (ex: "J0Eblogg5")
let rec uploadAFolder server user pwd (srcPath : string) (rootDir : string) = 
    logfn "folder to upload=%s" srcPath
    let dirInfo = new DirectoryInfo(srcPath)
    if dirInfo.Exists && dirNameIsValid rootDir then 
        dirInfo.GetFileSystemInfos() |> Seq.iter (fun fsi -> upload server user pwd fsi rootDir)

and private upload server user pwd (fsi : FileSystemInfo) (rootDir : string) = 
    match fsi.GetType().ToString() with
    | "System.IO.DirectoryInfo" -> 
        createAFolder server user pwd rootDir
        createAFolder server user pwd (sprintf "%s\\%s" rootDir fsi.Name)
        uploadAFolder server user pwd fsi.FullName (sprintf "%s\\%s" rootDir fsi.Name)
    | "System.IO.FileInfo" -> uploadAFile server user pwd (sprintf "%s\\%s" rootDir fsi.Name) fsi.FullName
    | _ -> logfn "unknown object found at %A" fsi
