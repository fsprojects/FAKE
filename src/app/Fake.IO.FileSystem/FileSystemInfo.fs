namespace Fake.IO

open System.IO

/// <summary>
/// Contains tasks to interact with <c>FileSystemInfo</c>
/// </summary>
[<RequireQualifiedAccess>]
module FileSystemInfo =
    
    /// <summary>
    /// Creates a FileInfo or a DirectoryInfo for the given path
    /// </summary>
    ///
    /// <param name="path">The path to create <c>FileSystemInfo</c> from</param>
    let inline ofPath path : FileSystemInfo = 
        if Directory.Exists path then upcast DirectoryInfo.ofPath path
        else upcast FileInfo.ofPath path
    
    /// <summary>
    /// Sets all given files or directories readonly.
    /// </summary>
    ///
    /// <param name="readOnly">The readonly flag</param>
    /// <param name="items">The list of files to set readonly flag on</param>
    let setReadOnly readOnly (items : string seq) = 
        items |> Seq.iter (fun item ->
            let fi = FileInfo.ofPath item
            if fi.Exists then fi.IsReadOnly <- readOnly
            else 
                item
                |> DirectoryInfo.ofPath
                |> DirectoryInfo.setReadOnly readOnly)

    /// <summary>
    /// Active pattern which discriminates between files and directories.
    /// </summary>
    ///
    /// <param name="fileSysInfo">The <c>FileSystemInfo</c> to check</param>
    let (|File|Directory|) (fileSysInfo : FileSystemInfo) = 
        match fileSysInfo with
        | :? FileInfo as file -> File(file)
        | :? DirectoryInfo as dir -> Directory(dir, dir.EnumerateFileSystemInfos())
        | _ -> failwith "No file or directory given."

    let internal moveTo (fileSysInfo: FileSystemInfo) dest =
        match fileSysInfo with
        | :? FileInfo as file -> file.MoveTo(dest)
        | :? DirectoryInfo as dir -> dir.MoveTo(dest)
        | _ -> failwith "No file or directory given."
