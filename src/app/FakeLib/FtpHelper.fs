/// Contains helpers which allow to upload a whole folder/specific file into a FTP Server. 
/// Uses `Passive Mode` FTP and handles all files as binary (and not ASCII).
/// Assumes direct network connectivity to destination FTP server (not via a proxy).
/// Does not support FTPS and SFTP.
module Fake.FtpHelper

open System
open System.IO
open System.Net
open System.Text.RegularExpressions

type FtpServerInfo = 
    { Server : string
      Request : FtpWebRequest }

/// Gets a connection to the FTP server
let getServerInfo (serverNameIp : string) (user : string) (password : string) ftpMethod = 
    let ftpRequest = (WebRequest.Create serverNameIp) :?> FtpWebRequest
    ftpRequest.Credentials <- new NetworkCredential(user, password)
    ftpRequest.Method <- ftpMethod
    { Server = serverNameIp
      Request = ftpRequest }

/// Writes given byte array into the given stream
let rec private writeChunkToReqStream (chunk : byte []) (reqStrm : Stream) (br : BinaryReader) = 
    if chunk.Length <> 0 then 
        reqStrm.Write(chunk, 0, chunk.Length)
        writeChunkToReqStream (br.ReadBytes 1024) reqStrm br

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

/// Checks to see if the `ftp content` string containts the string `Given_Folder_Name`
let inline regexCheck fname ftpContents = Regex.IsMatch(ftpContents, (sprintf @"\s+%s\s+" fname))

/// Gets the contents/listing of files and folders in a given FTP server folder
/// ## Parameters
///  - `dirPath` - The full name of folder whose content need to be listed
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let getFtpDirContents (server : string) (user : string) (pwd : string) (dirPath : string) = 
    logfn "Getting FTP dir contents for %s" dirPath
    dirPath
    |> fun d -> getServerInfo (sprintf "%s/%s" server d) user pwd WebRequestMethods.Ftp.ListDirectoryDetails
    |> fun si -> 
        use response = (si.Request.GetResponse() :?> FtpWebResponse)
        use responseStream = response.GetResponseStream()
        use reader = new StreamReader(responseStream)
        reader.ReadToEnd()

/// Uploads a single file from local directory into remote FTP folder.
/// ## Parameters
///  - `destPath` - The full local file path that needs to be uploaded
///  - `srcPath` - The full path to file which needs to be created, including all its parent folders
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let uploadAFile (server : string) (user : string) (pwd : string) (destPath : string) (srcPath : string) = 
    logfn "Uploading %s to %s" srcPath destPath
    let fl = new FileInfo(srcPath)
    if (fl.Length <> 0L) then 
        destPath
        |> fun d -> getServerInfo (sprintf "%s/%s" server d) user pwd WebRequestMethods.Ftp.UploadFile
        |> fun si -> 
            use fs = new FileStream(srcPath, FileMode.Open, FileAccess.Read)
            use br = new BinaryReader(fs, new System.Text.UTF8Encoding())
            use reqStrm = si.Request.GetRequestStream()
            writeChunkToReqStream (br.ReadBytes 1024) reqStrm br

/// Given a folder name, will check if that folder is present at a given root directory of a FTP server.
/// ## Parameters
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let private checkInExistingDirList server user pwd destPath fname = 
    destPath
    |> lastSlashPos
    |> getSubstring 0 destPath
    |> getFtpDirContents server user pwd
    |> regexCheck fname

/// Given a folder path, will check if that folder is present at a given root directory of a FTP server.
/// ## Parameters
///  - `destPath` - The full name of folder which needs to be checked for existance, including all its parent folders
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let isFolderPresent server user pwd (destPath : string) = 
    destPath
    |> lastSlashPos
    |> destPath.Substring
    |> checkInExistingDirList server user pwd destPath

/// Creates a matching folder in FTP folder, if not already present.
/// ## Parameters
///  - `destPath` - The full name of folder which needs to be created, including all its parent folders
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let createAFolder (server : string) (user : string) (pwd : string) (destPath : string) = 
    logfn "Creating folder %s" destPath
    if not ((String.IsNullOrEmpty destPath) || (isFolderPresent server user pwd destPath)) then 
        destPath
        |> fun d -> getServerInfo (sprintf "%s/%s" server d) user pwd WebRequestMethods.Ftp.MakeDirectory
        |> fun si -> 
            use response = (si.Request.GetResponse() :?> FtpWebResponse)
            logfn "Create folder status: %s" (response.StatusDescription)

/// Uploads a given local folder to a given root dir on a FTP server.
/// ## Parameters
///  - `srcPath` - The local server path from which files need to be uploaded
///  - `rootDir` - The remote root dir where files need to be uploaded, leave this as empty, if files need to be uploaded to root dir of FTP server
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let rec uploadAFolder server user pwd (srcPath : string) (rootDir : string) = 
    logfn "Uploading folder %s" srcPath
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
    | _ -> logfn "Unknown object found at %A" fsi

/// Deletes a single file from remote FTP folder.
/// ## Parameters
///  - `destPath` - The full path to the file which needs to be deleted, including all its parent folders
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let deleteAFile (server : string) (user : string) (pwd : string) (destPath : string) = 
    logfn "Deleting %s" destPath
    destPath
    |> fun p -> getServerInfo (sprintf "%s/%s" server p) user pwd WebRequestMethods.Ftp.DeleteFile
    |> fun si -> 
        use response = (si.Request.GetResponse() :?> FtpWebResponse)
        logfn "Delete file %s status: %s" destPath response.StatusDescription

let private getFolderContents (server : string) (user : string) (pwd : string) (destPath : string) =
    getServerInfo (sprintf "%s/%s" server destPath) user pwd WebRequestMethods.Ftp.ListDirectory
    |> fun si -> 
        use response = (si.Request.GetResponse() :?> FtpWebResponse)
        use responseStream = response.GetResponseStream()
        use reader = new StreamReader(responseStream)
        [ while not reader.EndOfStream do yield reader.ReadLine() ]

let private deleteEmptyFolder (server : string) (user : string) (pwd : string) (destPath : string) =
    destPath
    |> fun p -> getServerInfo (sprintf "%s/%s" server p) user pwd WebRequestMethods.Ftp.RemoveDirectory
    |> fun si -> 
        use response = (si.Request.GetResponse() :?> FtpWebResponse)
        logfn "Delete folder %s status: %s" destPath response.StatusDescription

/// Deletes a single folder from remote FTP folder.
/// ## Parameters
///  - `destPath` - The full path to the folder which needs to be deleted, including all its parent folders
///  - `server` - FTP Server name (ex: "ftp://10.100.200.300:21/")
///  - `user` - FTP Server login name (ex: "joebloggs")
///  - `pwd` - FTP Server login password (ex: "J0Eblogg5")
let rec deleteAFolder (server : string) (user : string) (pwd : string) (destPath : string) = 
    logfn "Deleting %s" destPath
    let folderContents = getFolderContents server user pwd destPath

    if folderContents |> List.isEmpty then
        deleteEmptyFolder server user pwd destPath
    else
        folderContents
        |> List.iter (fun entry ->
                        try
                            deleteAFile server user pwd (Path.Combine(destPath, entry))
                        with
                        | _ -> deleteAFolder server user pwd (Path.Combine(destPath, entry)))

        deleteEmptyFolder server user pwd destPath