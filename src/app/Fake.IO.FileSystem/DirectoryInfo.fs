namespace Fake.IO.FileSystem

open System.IO

module DirectoryInfo =
    /// Creates a DirectoryInfo for the given path.
    let inline ofPath path = DirectoryInfo(path)

    /// Gets all subdirectories of a given directory.
    let inline getDirectories (dir : DirectoryInfo) = dir.GetDirectories()

    /// Gets all files in the directory.
    let inline getFiles (dir : DirectoryInfo) = dir.GetFiles()

    /// Finds all the files in the directory matching the search pattern.
    let getMatchingFiles pattern (dir : DirectoryInfo) = 
        if dir.Exists then dir.GetFiles pattern
        else [||]
        
    /// Checks if dir1 is a subfolder of dir2. If dir1 equals dir2 the function returns also true.
    let rec isSubfolderOf (dir2 : DirectoryInfo) (dir1 : DirectoryInfo) = 
        if Path.normalizeFileName dir1.FullName = Path.normalizeFileName dir2.FullName then true
        else if isNull dir1.Parent then false
        else dir1.Parent |> isSubfolderOf dir2
        
    /// Checks if the file is in a subfolder of the dir.
    let containsFile (fileInfo : FileInfo) (dir : DirectoryInfo) = isSubfolderOf dir fileInfo.Directory
    
    /// Checks if the directory exists on disk.
    let exists (dir : DirectoryInfo) = dir.Exists
    

    /// Ensure that directory chain exists. Create necessary directories if necessary.
    let inline ensure (dir : DirectoryInfo) =
        if not dir.Exists then dir.Create()

 
    /// Performs the given actions on all files and subdirectories
    let rec recursively dirF fileF (dir : DirectoryInfo) = 
        dir
        |> getDirectories
        |> Seq.iter (fun dir -> 
               recursively dirF fileF dir
               dirF dir)
        dir
        |> getFiles
        |> Seq.iter fileF

    /// Sets the directory readonly 
    let setDirectoryReadOnly readOnly (dir : DirectoryInfo) = 
        if dir.Exists then 
            let isReadOnly = dir.Attributes &&& FileAttributes.ReadOnly = FileAttributes.ReadOnly
            if readOnly && (not isReadOnly) then dir.Attributes <- dir.Attributes ||| FileAttributes.ReadOnly
            if (not readOnly) && not isReadOnly then dir.Attributes <- dir.Attributes &&& (~~~FileAttributes.ReadOnly)

    /// Sets all files in the directory readonly.
    let SetDirReadOnly readOnly dir = 
        recursively (setDirectoryReadOnly readOnly) (fun file -> file.IsReadOnly <- readOnly) dir
    
    /// Copies the file structure recursively.
    let rec copyRecursiveTo2 overwrite filter (outputDir : DirectoryInfo) (dir : DirectoryInfo) = 
        let files = 
            dir
            |> getDirectories
            |> Seq.fold (fun acc (d : DirectoryInfo) -> 
                   let newDir = outputDir.FullName @@ d.Name
                                |> ofPath
                   ensure newDir
                   d
                   |> copyRecursiveTo2 overwrite filter newDir
                   |> fun r -> r @ acc) []
        (dir
         |> getFiles
         |> Seq.filter (fun f -> filter outputDir f)
         |> Seq.map (fun f -> 
                let newFileName = outputDir.FullName @@ f.Name
                f.CopyTo(newFileName, overwrite) |> ignore
                newFileName)
         |> Seq.toList) @ files

    /// Copies the file structure recursively.
    let copyRecursiveTo overwrite (outputDir : DirectoryInfo) (dir : DirectoryInfo) = copyRecursiveTo2 overwrite (fun _ _ -> true) outputDir dir
    /// Copies the file structure recursively.
    let copyRecursive (dir : DirectoryInfo) (outputDir : DirectoryInfo) overwrite = dir |> copyRecursiveTo overwrite outputDir
    /// Copies the file structure recursively.
    let copyRecursive2 (dir : DirectoryInfo) (outputDir : DirectoryInfo) overwrite filter = dir |> copyRecursiveTo2 overwrite filter outputDir

