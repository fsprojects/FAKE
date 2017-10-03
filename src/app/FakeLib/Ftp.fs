/// Contains functions to upload files and folders to an FTP Server. Uses `Passive Mode` FTP. 
/// All transfers are binary. Supports non-trivial scenarios (secure FTP, proxy, etc.) by
/// delegating creation of the FtpWebRequest object to a function of type FtpRequestFunc.
/// A default factory function, createFtpRequestFunc(), is provided, and is sufficient when
/// only a name and password is needed for a request (i.e., the only scenarios that FAKE 4
/// FtpHelper supported). See comments for createFtpRequestFunc() for more details. All functions
/// that execute requests take an instance of FtpRequestFunc as the first parameter, and use it
/// to create one or more FtpWebRequest objects, as needed (some operations require multiple
/// requests, and the framework does not allow an FtpWebRequest to be used more than once).
module Ftp

(*

// example task

Target "Ftp" (fun _ ->
    let uri = Uri("ftp://mycompany.com/")
    let user = "john"
    let pwd = "password"
    let version = "1.0.0.0"

    // We need an FtpRequestFunc object. If our scenario is simple, just 
    // use the default factory method:
    //
    // let rf = Ftp.createFtpRequestFunc(user, pwd, uri)
    //
    // In this example, we have a certificate on the server, so we need to
    // wrap the default factory method so we can add additional logic.

    let createRequestFunc (user: string, pwd: string, uri: Uri) = 
        // return a function that wraps Ftp.createFtpRequestFunc, doing whatever
        // additional processing is necessary (in this case, turning on SSL)
        (fun path ->
            let ftpRequest = Ftp.createFtpRequestFunc(user, pwd, uri) path
            ftpRequest.EnableSsl <- true
            ftpRequest)

    // curry some functions for ease of use
    let requestFunc = createRequestFunc(user, pwd, uri)
    let deleteFolder = Ftp.deleteFolder requestFunc
    let upload = Ftp.upload requestFunc

    // If you have a certificate installed on the server, then presumably you have also
    // installed it locally, and now you need to add it. 
    let userCaStore = X509Store(StoreName.My, StoreLocation.CurrentUser)
    userCaStore.Open(OpenFlags.ReadOnly)
    // (etc.)

    // Starting with .NET 4.5 you can (if you understand the risk) accept any certificate, see
    // https://stackoverflow.com/questions/12506575/how-to-ignore-the-certificate-check-when-ssl

    // recursively delete remote folder and contents (if they exist)
    version
    |> sprintf "/website/%s/" 
    |> deleteFolder

    // recurvsively upload local folder and contents
    version
    |> combine deployDir 
    |> upload "/website/"
)

*)

open System
open System.IO
open System.Net
open System.Text

/// Type alias to clarify that a string represents a remote path.
type RemotePath = string

/// Type alias to clarify that a string represents a local path.
type LocalPath = string

/// Function signature for a "create FtpWebRequest" function. This will be the first parameter
/// for any function that performs a remote operation. See createFtpRequestFunc() for more details.
type FtpRequestFunc = (RemotePath -> FtpWebRequest)

module private Impl =
    /// Gets listing of files and folders for a remote path.
    /// ## Parameters
    ///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
    ///  - `detailed` - Flag to indicate whether to use ListDirectoryDetails or ListDirectory as the request method.
    ///  - `remotePath` - Remote path to list contents for.
    let getFolderContents (rf: FtpRequestFunc) detailed (remotePath: RemotePath) =
        let request = rf remotePath
        let ``method`` = 
            match detailed with
            | true -> WebRequestMethods.Ftp.ListDirectoryDetails 
            | false -> WebRequestMethods.Ftp.ListDirectory
        request.Method <- ``method``
        use response = request.GetResponse() :?> FtpWebResponse
        use responseStream = response.GetResponseStream()
        use reader = new StreamReader(responseStream)
        reader.ReadToEnd()

    /// Check to see if a remote folder already exists.
    /// ## Parameters
    ///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
    ///  - `remotePath` - Remote path to check.
    let folderExists (rf: FtpRequestFunc) (remotePath: RemotePath) = 
        // ah, the joys of FTP programming
        try
            remotePath
            |> getFolderContents rf false
            |> ignore
            true
        with 
        | :? WebException as ex ->
            let response = ex.Response :?> FtpWebResponse
            if (response.StatusCode = FtpStatusCode.ActionNotTakenFileUnavailable) then
                // 550
                false
            else
                reraise()

    /// Deletes a remote file.
    /// ## Parameters
    ///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
    ///  - `remotePath` - The full path to the file which needs to be deleted, including all its parent folders
    let deleteFile (rf: FtpRequestFunc) (remotePath: RemotePath) = 
        let request = rf remotePath
        request.Method <- WebRequestMethods.Ftp.DeleteFile
        use __ = request.GetResponse()
        ()

    /// Uploads a local file to a remote path.
    /// ## Parameters
    ///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
    ///  - `remotePath` - Remote path where file will be uploaded to.
    ///  - `localPath` - Full path to a local file to be uploaded.
    let uploadFile (rf: FtpRequestFunc) (remotePath: RemotePath) (localPath: LocalPath) = 
        let rec write (bytes: byte[]) (stream: Stream) (br: BinaryReader) = 
            if bytes.Length <> 0 then 
                stream.Write(bytes, 0, bytes.Length)
                write (br.ReadBytes 1024) stream br

        let request = rf remotePath
        request.Method <- WebRequestMethods.Ftp.UploadFile
        // see comments in upload() as to why I'm only printing name here
        printfn "Uploading '%s'" (Path.GetFileName localPath)
        use fs = new FileStream(localPath, FileMode.Open, FileAccess.Read)
        use br = new BinaryReader(fs, UTF8Encoding())
        use stream = request.GetRequestStream()
        write (br.ReadBytes 1024) stream br

open Impl

/// This returns a function to create an instance of FtpWebRequest for a single FTP request. It
/// can be used as is, or wrapped by a function that does additional work. For example, if you are
/// doing secure FTP, you could wrap this with a function that sets EnableSsl to true and adds
/// a certificate. (Note: you never need to set Method because that will be done by the consuming 
/// function; e.g., uploadFile() sets it to WebRequestMethods.Ftp.UploadFile).
/// ## Parameters
///  - `user` - FTP server login name
///  - `pwd` - FTP server login password
///  - `rootUri` - FTP server Uri with scheme, host, and optional port, but no path (ex: "ftp://10.100.200.300:21/")
let createFtpRequestFunc (user: string, password: string, rootUri: Uri) : FtpRequestFunc = 
    if rootUri.LocalPath <> "/" then 
        failwithf "rootUri contains a local path: '%s'" rootUri.LocalPath

    // create a function that takes a RemotePath and creates a FtpWebRequest for single-use for that path
    (fun path ->
        let uri = Uri(rootUri, path)
        let ftpRequest = 
            uri
            |> WebRequest.Create
            :?> FtpWebRequest
        ftpRequest.Credentials <- NetworkCredential(user, password)
        ftpRequest)

/// Attempts to create a remote folder. If the folder already exists, just prints a message.
/// ## Parameters
///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
///  - `remotePath` - Remote path of folder to be created.
let createFolder (rf: FtpRequestFunc) (remotePath: RemotePath) = 
    let request = rf remotePath
    printfn "Creating remote folder %s" remotePath
    if not (folderExists rf remotePath) then 
        request.Method <- WebRequestMethods.Ftp.MakeDirectory
        use __ = request.GetResponse()
        ()
    else
        printfn "Remote folder '%s' already exists" remotePath

/// Deletes a remote file.
/// ## Parameters
///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
///  - `remotePath` - The full path to the file which needs to be deleted, including all its parent folders
let deleteFile (rf: FtpRequestFunc) (remotePath: RemotePath) = 
    printfn "Deleting remote file '%s'" remotePath
    Impl.deleteFile rf remotePath

/// Attempts to delete a remote folder and its contents. If the folder doesn't exist, just prints a message.
/// ## Parameters
///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
///  - `remotePath` - The full path to the folder which needs to be deleted, including all its parent folders
let rec deleteFolder (rf: FtpRequestFunc) (remotePath: RemotePath) = 
    let splitLines (text: string) =
        // I'm assuming no FTP server uses \r as a newline
        text.Split([|"\r\n"; "\n"|], StringSplitOptions.RemoveEmptyEntries) 

    if (folderExists rf remotePath) then 
        remotePath 
        |> getFolderContents rf false 
        |> splitLines
        |> Seq.iter (fun line ->
            // Try to delete the file, if that fails it must be a folder. This is a hack to deal with the
            // fact that while most FTP servers do return directory listings in either Windows or UNIX 
            // format, it's not part of the standard, and thus difficult to handle all possible cases.
            let remoteFile = line |> sprintf "%s%s" remotePath
            try
                printfn "Deleting remote file '%s'" remoteFile
                // use the silent version, in case it fails
                Impl.deleteFile rf remoteFile
            with
            | _ -> 
                // must be a folder
                let remoteFolder = line |> sprintf "%s%s/" remotePath
                printfn "'%s' is not a remote file" remoteFile
                printfn "Deleting remote folder '%s'" remoteFolder
                remoteFolder
                |> deleteFolder rf)
    
        // folder should be empty now (but of course don't try to delete root)
        if (remotePath <> "/") then
            printfn "Deleting remote folder '%s'" remotePath
            let request = rf remotePath
            request.Method <- WebRequestMethods.Ftp.RemoveDirectory
            use __ = request.GetResponse()
            ()
    else
        printfn "Remote folder '%s' does not exist" remotePath

/// Uploads a local path to a remote path. If localPath represents a file, it uploads that file.
/// If it reprsents a folder, it creates that folder, then recursively iterates through all files
/// and subfolders.
/// ## Parameters
///  - `rf` - Function that will be used to create instance(s) of FtpWebRequest.
///  - `remotePath` - Remote path where folder and contents will be uploaded to.
///  - `localPath` - Full path to a local file or folder to be uploaded or created.
let rec upload (rf: FtpRequestFunc) (remotePath: RemotePath) (localPath: LocalPath) = 
    if File.Exists localPath then
        // upload this file
        let fi = FileInfo localPath
        let targetServerPath = sprintf "%s%s" remotePath fi.Name
        uploadFile rf targetServerPath localPath
    else if Directory.Exists localPath then
        // create this folder
        let di = DirectoryInfo localPath
        let targetServerPath = sprintf "%s%s/" remotePath di.Name
        createFolder rf targetServerPath

        // iterate through its children recursively
        // on first pass create all folders, then upload all files
        let entries = di.GetFileSystemInfos()

        entries
        |> Seq.filter (fun fsi -> fsi :? DirectoryInfo)
        |> Seq.map (fun fsi -> fsi.FullName)
        |> Seq.iter (fun localPath -> upload rf targetServerPath localPath)

        // printing the folder name once allows us to cut down on noise by only printing
        // name of file inside uploadFile()
        printfn "Uploading files from '%s' to '%s'" localPath targetServerPath

        entries
        |> Seq.filter (fun fsi -> fsi :? FileInfo)
        |> Seq.map (fun fsi -> fsi.FullName)
        |> Seq.iter (fun localPath -> upload rf targetServerPath localPath)
    else
        failwithf "Local path '%s' does not exist" localPath
