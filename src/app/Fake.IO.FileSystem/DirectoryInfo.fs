namespace Fake.IO

open System.IO
open Fake.IO.FileSystemOperators

/// <summary>
/// Contains tasks to interact with <c>DirectoryInfo</c>
/// </summary>
[<RequireQualifiedAccess>]
module DirectoryInfo =
    
    /// <summary>
    /// Creates a DirectoryInfo for the given path.
    /// </summary>
    /// 
    /// <param name="path">The path to use</param>
    let inline ofPath path = DirectoryInfo(path)

    /// <summary>
    /// Gets all subdirectories of a given directory.
    /// </summary>
    /// 
    /// <param name="dir">The directory to use</param>
    let inline getSubDirectories (dir : DirectoryInfo) = dir.GetDirectories()

    /// <summary>
    /// Gets all files in the directory.
    /// </summary>
    /// 
    /// <param name="dir">The directory to use</param>
    let inline getFiles (dir : DirectoryInfo) = dir.GetFiles()

    /// <summary>
    /// Finds all the files in the directory matching the search pattern.
    /// </summary>
    /// 
    /// <param name="pattern">The glob pattern to use for search</param>
    /// <param name="dir">The directory to use</param>
    let getMatchingFiles pattern (dir : DirectoryInfo) = 
        if dir.Exists then dir.GetFiles pattern
        else [||]

        
    /// <summary>
    /// Finds all the files in the directory and in all subdirectories matching the search pattern.
    /// </summary>
    /// 
    /// <param name="pattern">The glob pattern to use for search</param>
    /// <param name="dir">The directory to use</param>
    let getMatchingFilesRecursive pattern (dir : DirectoryInfo) = 
        if dir.Exists then dir.GetFiles(pattern, SearchOption.AllDirectories)
        else [||]
        
    /// <summary>
    /// Checks if dir1 is a subfolder of dir2. If dir1 equals dir2 the function returns also true.
    /// </summary>
    /// 
    /// <param name="dir2">The second directory to check for</param>
    /// <param name="dir1">The first directory to check for</param>
    let rec isSubfolderOf (dir2 : DirectoryInfo) (dir1 : DirectoryInfo) = 
        if Path.normalizeFileName dir1.FullName = Path.normalizeFileName dir2.FullName then true
        else if isNull dir1.Parent then false
        else dir1.Parent |> isSubfolderOf dir2
        
    /// <summary>
    /// Checks if the file is in a subfolder of the dir.
    /// </summary>
    /// 
    /// <param name="fileInfo">The file to check for</param>
    /// <param name="dir">The directory to search in</param>
    let containsFile (fileInfo : FileInfo) (dir : DirectoryInfo) = isSubfolderOf dir fileInfo.Directory
    
    /// <summary>
    /// Checks if the directory exists on disk.
    /// </summary>
    /// 
    /// <param name="dir">The directory to check for</param>
    let exists (dir : DirectoryInfo) = dir.Exists
    
    /// <summary>
    /// Ensure that directory chain exists. Create necessary directories if necessary.
    /// </summary>
    /// 
    /// <param name="dir">The directory to check</param>
    let inline ensure (dir : DirectoryInfo) =
        if not dir.Exists then dir.Create()
 
    /// <summary>
    /// Performs the given actions on all files and subdirectories
    /// </summary>
    let rec private recursively dirF fileF (dir : DirectoryInfo) = 
        dir
        |> getSubDirectories
        |> Seq.iter (fun dir -> 
               recursively dirF fileF dir
               dirF dir)
        dir
        |> getFiles
        |> Seq.iter fileF

    /// <summary>
    /// Sets the directory readonly
    /// </summary>
    /// 
    /// <param name="readOnly">Flag to set directory to readonly or not. </param>
    /// <param name="dir">The directory to set</param>
    let setReadOnly readOnly (dir : DirectoryInfo) = 
        if dir.Exists then 
            let isReadOnly = dir.Attributes &&& FileAttributes.ReadOnly = FileAttributes.ReadOnly
            if readOnly && (not isReadOnly) then dir.Attributes <- dir.Attributes ||| FileAttributes.ReadOnly
            if (not readOnly) && not isReadOnly then dir.Attributes <- dir.Attributes &&& ~~~FileAttributes.ReadOnly

    /// <summary>
    /// Sets all files in the directory readonly recursively.
    /// </summary>
    /// 
    /// <param name="readOnly">Flag to set directory to readonly or not. </param>
    /// <param name="dir">The directory to set</param>
    let setReadOnlyRecursive readOnly dir = 
        recursively (setReadOnly readOnly) (fun file -> file.IsReadOnly <- readOnly) dir
    
    /// <summary>
    /// Copies the file structure recursively, filtering files.
    /// </summary>
    /// 
    /// <param name="overwrite">Flag to overwrite same files in target dir</param>
    /// <param name="filter">The filter to use to filter the list of files</param>
    /// <param name="outputDir">The target directory to copy to</param>
    /// <param name="dir">The source directory to copy from</param>
    let rec copyRecursiveToWithFilter overwrite filter (outputDir : DirectoryInfo) (dir : DirectoryInfo) = 
        let files = 
            dir
            |> getSubDirectories
            |> Seq.fold (fun acc (d : DirectoryInfo) -> 
                   let newDir = outputDir.FullName @@ d.Name
                                |> ofPath
                   ensure newDir
                   d
                   |> copyRecursiveToWithFilter overwrite filter newDir
                   |> fun r -> r @ acc) []
        (dir
         |> getFiles
         |> Seq.filter (fun f -> filter outputDir f)
         |> Seq.map (fun f -> 
                let newFileName = outputDir.FullName @@ f.Name
                f.CopyTo(newFileName, overwrite) |> ignore
                newFileName)
         |> Seq.toList) @ files

    /// <summary>
    /// Copies the file structure recursively.
    /// </summary>
    /// 
    /// <param name="overwrite">Flag to overwrite same files in target dir</param>
    /// <param name="outputDir">The target directory to copy to</param>
    /// <param name="dir">The source directory to copy from</param>
    let copyRecursiveTo overwrite (outputDir : DirectoryInfo) (dir : DirectoryInfo) = copyRecursiveToWithFilter overwrite (fun _ _ -> true) outputDir dir
