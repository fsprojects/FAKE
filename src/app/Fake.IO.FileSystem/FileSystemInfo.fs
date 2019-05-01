namespace Fake.IO

open System.IO

[<RequireQualifiedAccess>]
module FileSystemInfo =
    /// Creates a FileInfo or a DirectoryInfo for the given path
    let inline ofPath path : FileSystemInfo = 
        if Directory.Exists path then upcast DirectoryInfo.ofPath path
        else upcast FileInfo.ofPath path
    
    /// Sets all given files or directories readonly.
    let setReadOnly readOnly (items : string seq) = 
        items |> Seq.iter (fun item ->
            let fi = FileInfo.ofPath item
            if fi.Exists then fi.IsReadOnly <- readOnly
            else 
                item
                |> DirectoryInfo.ofPath
                |> DirectoryInfo.setReadOnly readOnly)

    /// Active pattern which discriminates between files and directories.
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
