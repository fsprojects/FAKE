namespace Fake.Net

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open Fake.Core

/// <namespacedoc>
/// <summary>
/// Net namespace contains tasks to interact with network, like FTP and SSH
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains helpers which allow to upload a whole folder/specific file into a FTP Server.
/// Uses <c>Passive Mode</c> FTP and handles all files as binary (and not ASCII).
/// Assumes direct network connectivity to destination FTP server (not via a proxy).
/// Does not support FTPS and SFTP.
/// </summary>
[<RequireQualifiedAccess>]
module FTP =

    /// <summary>
    /// Server information
    /// </summary>
    type FtpServerInfo =
        {
            /// Server name
            Server: string

            /// FTP request
            Request: FtpWebRequest
        }

    /// <summary>
    /// Gets a connection to the FTP server
    /// </summary>
    ///
    /// <param name="serverNameIp">The server IP address</param>
    /// <param name="user">The user name to use in login credentials</param>
    /// <param name="password">FTP Server login password</param>
    /// <param name="ftpMethod">Command to send to FTP server</param>
    let getServerInfo (serverNameIp: string) (user: string) (password: string) ftpMethod =
        let ftpRequest = (WebRequest.Create serverNameIp) :?> FtpWebRequest
        ftpRequest.Credentials <- NetworkCredential(user, password)
        ftpRequest.Method <- ftpMethod

        { Server = serverNameIp
          Request = ftpRequest }

    /// Writes given byte array into the given stream
    let rec private writeChunkToReqStream (chunk: byte[]) (requestStream: Stream) (br: BinaryReader) =
        if chunk.Length <> 0 then
            requestStream.Write(chunk, 0, chunk.Length)
            writeChunkToReqStream (br.ReadBytes 1024) requestStream br

    let inline private getSubstring (fromPos: int) (str: string) (toPos: int) = str.Substring(fromPos, toPos)
    let inline private getLastSlashPosition (str: string) = str.LastIndexOf(@"/") + 1

    let private charactersValidator (directoryName: string) =
        let invalidChars = [ "<"; ">"; ":"; "\""; "/"; "\\"; "|"; "?"; "*" ]
        not (invalidChars |> List.exists directoryName.Contains)

    let private namesValidator (directoryName: string) =
        let invalidNames =
            [ "CON"
              "PRN"
              "AUX"
              "NUL"
              "COM1"
              "COM2"
              "COM3"
              "COM4"
              "COM5"
              "COM6"
              "COM7"
              "COM8"
              "COM9"
              "LPT1"
              "LPT2"
              "LPT3"
              "LPT4"
              "LPT5"
              "LPT6"
              "LPT7"
              "LPT8"
              "LPT9" ]

        not (List.exists (fun s -> s = directoryName.ToUpper()) invalidNames)
        && not (List.exists (fun s -> directoryName.ToUpper().StartsWith $"%s{s}.") invalidNames)

    let private customValidator (directoryName: string) =
        not (directoryName.EndsWith(" ")) && not (directoryName.EndsWith("."))

    /// Partial validation for folder name, based on http://msdn.microsoft.com/en-us/library/aa365247.aspx
    let internal isValidDirectoryName (directoryName: string) =
        let validators = [ charactersValidator; namesValidator; customValidator ]
        List.forall (fun validator -> directoryName |> validator) validators

    /// Checks to see if the `ftp content` string contains the string `Given_Folder_Name`
    let inline regexCheck folderName ftpContents =
        Regex.IsMatch(ftpContents, $@"\s+%s{folderName}\s+")

    /// <summary>
    /// Gets the contents/listing of files and folders in a given FTP server folder
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    /// <param name="dirPath">The full name of folder whose content need to be listed</param>
    let getFtpDirContents (server: string) (user: string) (pwd: string) (dirPath: string) =
        Trace.logfn $"Getting FTP dir contents for %s{dirPath}"

        dirPath
        |> fun d -> getServerInfo $"%s{server}/%s{d}" user pwd WebRequestMethods.Ftp.ListDirectoryDetails
        |> fun si ->
            use response = (si.Request.GetResponse() :?> FtpWebResponse)
            use responseStream = response.GetResponseStream()
            use reader = new StreamReader(responseStream)
            reader.ReadToEnd()

    /// <summary>
    /// Uploads a single file from local directory into remote FTP folder.
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    /// <param name="destPath">The full local file path that needs to be uploaded</param>
    /// <param name="srcPath">The full path to file which needs to be created, including all its parent folders</param>
    let uploadAFile (server: string) (user: string) (pwd: string) (destPath: string) (srcPath: string) =
        Trace.logfn $"Uploading %s{srcPath} to %s{destPath}"
        let fl = FileInfo(srcPath)

        if (fl.Length <> 0L) then
            destPath
            |> fun d -> getServerInfo $"%s{server}/%s{d}" user pwd WebRequestMethods.Ftp.UploadFile
            |> fun si ->
                use fs = new FileStream(srcPath, FileMode.Open, FileAccess.Read)
                use br = new BinaryReader(fs, System.Text.UTF8Encoding())
                use requestStream = si.Request.GetRequestStream()
                writeChunkToReqStream (br.ReadBytes 1024) requestStream br

    /// <summary>
    /// Given a folder name, will check if that folder is present at a given root directory of a FTP server.
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    let private isFolderInDirectoryList server user pwd destPath folderName =
        destPath
        |> getLastSlashPosition
        |> getSubstring 0 destPath
        |> getFtpDirContents server user pwd
        |> regexCheck folderName

    /// <summary>
    /// Given a folder path, will check if that folder is present at a given root directory of a FTP server.
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    /// <param name="destPath">The full name of folder which needs to be checked for existence, including all its
    /// parent folders</param>
    let isFolderPresent server user pwd (destPath: string) =
        destPath
        |> getLastSlashPosition
        |> destPath.Substring
        |> isFolderInDirectoryList server user pwd destPath

    /// <summary>
    /// Creates a matching folder in FTP folder, if not already present.
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    /// <param name="destPath">The full name of folder which needs to be checked for existence, including all its
    /// parent folders</param>
    let createAFolder (server: string) (user: string) (pwd: string) (destPath: string) =
        Trace.logfn $"Creating folder %s{destPath}"

        if not ((String.IsNullOrEmpty destPath) || (isFolderPresent server user pwd destPath)) then
            destPath
            |> fun d -> getServerInfo $"%s{server}/%s{d}" user pwd WebRequestMethods.Ftp.MakeDirectory
            |> fun si ->
                use response = (si.Request.GetResponse() :?> FtpWebResponse)
                Trace.logfn $"Create folder status: %s{response.StatusDescription}"

    /// <summary>
    /// Uploads a given local folder to a given root dir on a FTP server.
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    /// <param name="srcPath">The local server path from which files need to be uploaded</param>
    /// <param name="rootDir">The remote root dir where files need to be uploaded, leave this as empty, if files need
    /// to be uploaded to root dir of FTP server</param>
    let rec uploadAFolder server user pwd (srcPath: string) (rootDir: string) =
        Trace.logfn $"Uploading folder %s{srcPath}"
        let dirInfo = DirectoryInfo(srcPath)

        if dirInfo.Exists && isValidDirectoryName rootDir then
            dirInfo.GetFileSystemInfos()
            |> Seq.iter (fun fsi -> upload server user pwd fsi rootDir)

    and private upload server user pwd (fsi: FileSystemInfo) (rootDir: string) =
        match fsi.GetType().ToString() with
        | "System.IO.DirectoryInfo" ->
            createAFolder server user pwd rootDir
            createAFolder server user pwd $"%s{rootDir}/%s{fsi.Name}"
            uploadAFolder server user pwd fsi.FullName $"%s{rootDir}/%s{fsi.Name}"
        | "System.IO.FileInfo" -> uploadAFile server user pwd $"%s{rootDir}/%s{fsi.Name}" fsi.FullName
        | _ -> Trace.logfn $"Unknown object found at %A{fsi}"

    /// <summary>
    /// Deletes a single file from remote FTP folder.
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    /// <param name="destPath">The full path to the file which needs to be deleted, including all its parent
    /// folders</param>
    let deleteAFile (server: string) (user: string) (pwd: string) (destPath: string) =
        Trace.logfn $"Deleting %s{destPath}"

        destPath
        |> fun p -> getServerInfo $"%s{server}/%s{p}" user pwd WebRequestMethods.Ftp.DeleteFile
        |> fun si ->
            use response = (si.Request.GetResponse() :?> FtpWebResponse)
            Trace.logfn $"Delete file %s{destPath} status: %s{response.StatusDescription}"

    let private getFolderContents (server: string) (user: string) (pwd: string) (destPath: string) =
        getServerInfo $"%s{server}/%s{destPath}" user pwd WebRequestMethods.Ftp.ListDirectory
        |> fun si ->
            use response = (si.Request.GetResponse() :?> FtpWebResponse)
            use responseStream = response.GetResponseStream()
            use reader = new StreamReader(responseStream)

            [ while not reader.EndOfStream do
                  yield reader.ReadLine() ]

    let private deleteEmptyFolder (server: string) (user: string) (pwd: string) (destPath: string) =
        destPath
        |> fun p -> getServerInfo $"%s{server}/%s{p}" user pwd WebRequestMethods.Ftp.RemoveDirectory
        |> fun si ->
            use response = (si.Request.GetResponse() :?> FtpWebResponse)
            Trace.logfn $"Delete folder %s{destPath} status: %s{response.StatusDescription}"

    /// <summary>
    /// Deletes a single folder from remote FTP folder.
    /// </summary>
    ///
    /// <param name="server">FTP Server name (ex: "ftp://10.100.200.300:21/")</param>
    /// <param name="user">FTP Server login name (ex: "joebloggs")</param>
    /// <param name="pwd">FTP Server login password (ex: "J0Eblogg5")</param>
    /// <param name="destPath">The full path to the folder which needs to be deleted, including all its parent
    /// folders</param>
    let rec deleteAFolder (server: string) (user: string) (pwd: string) (destPath: string) =
        Trace.logfn $"Deleting %s{destPath}"
        let folderContents = getFolderContents server user pwd destPath

        if folderContents |> List.isEmpty then
            deleteEmptyFolder server user pwd destPath
        else
            folderContents
            |> List.iter (fun entry ->
                try
                    deleteAFile server user pwd (Path.Combine(destPath, entry))
                with _ ->
                    deleteAFolder server user pwd (Path.Combine(destPath, entry)))

            deleteEmptyFolder server user pwd destPath
