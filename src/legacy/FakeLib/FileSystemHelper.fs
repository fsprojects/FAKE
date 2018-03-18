[<AutoOpen>]
/// Contains helpers which allow to interact with the file system.
[<System.Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem)")>]
module Fake.FileSystemHelper

open System
open System.Text
open System.IO
open System.Runtime.InteropServices

/// Creates a DirectoryInfo for the given path.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.ofPath)")>]
let inline directoryInfo path = new DirectoryInfo(path)

/// Creates a FileInfo for the given path.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.FileInfo.ofPath)")>]
let inline fileInfo path = new FileInfo(path)

/// Creates a FileInfo or a DirectoryInfo for the given path
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.FileSystemInfo.ofPath)")>]
let inline fileSystemInfo path : FileSystemInfo = 
    if Directory.Exists path then upcast directoryInfo path
    else upcast fileInfo path

/// Converts a filename to it's full file system name.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.Path.getFullName)")>]
let inline FullName fileName = Path.GetFullPath fileName

/// Gets the directory part of a filename.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.Path.getDirectory)")>]
let inline DirectoryName fileName = Path.GetDirectoryName fileName

/// Gets all subdirectories of a given directory.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.getSubDirectories)")>]
let inline subDirectories (dir : DirectoryInfo) = dir.GetDirectories()

/// Gets all files in the directory.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.getFiles)")>]
let inline filesInDir (dir : DirectoryInfo) = dir.GetFiles()

/// Finds all the files in the directory matching the search pattern.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.getMatchingFiles)")>]
let filesInDirMatching pattern (dir : DirectoryInfo) = 
    if dir.Exists then dir.GetFiles pattern
    else [||]
    
/// Finds all the files in the directory and in all subdirectories matching the search pattern.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.getMatchingFilesRecursive)")>]
let filesInDirMatchingRecursive pattern (dir : DirectoryInfo) = 
    if dir.Exists then dir.GetFiles(pattern, SearchOption.AllDirectories)
    else [||]    

/// Gets the first file in the directory matching the search pattern as an option value.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.tryFindFirstMatchingFile)")>]
let TryFindFirstMatchingFile pattern dir = 
    dir
    |> directoryInfo
    |> filesInDirMatching pattern
    |> fun files -> 
        if Seq.isEmpty files then None
        else (Seq.head files).FullName |> Some

/// Gets the first file in the directory matching the search pattern or throws an error if nothing was found.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.findFirstMatchingFile)")>]
let FindFirstMatchingFile pattern dir = 
    match TryFindFirstMatchingFile pattern dir with
    | Some x -> x
    | None -> new FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir) |> raise

/// Gets the current directory.
let currentDirectory = Path.GetFullPath "."

/// Get the full location of the current assembly.
let fullAssemblyPath = System.Reflection.Assembly.GetAssembly(typeof<RegistryBaseKey>).Location

/// Checks if the file exists on disk.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.File.exists)")>]
let fileExists fileName = File.Exists fileName

/// Raises an exception if the file doesn't exist on disk.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.File.checkExists)")>]
let checkFileExists fileName = 
    if not <| fileExists fileName then new FileNotFoundException(sprintf "File %s does not exist." fileName) |> raise

/// Checks if all given files exist.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.File.allExist)")>]
let allFilesExist files = Seq.forall fileExists files

/// Normalizes a filename.
let rec normalizeFileName (fileName : string) = 
    fileName.Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())
            .TrimEnd(Path.DirectorySeparatorChar).ToLower()

/// Checks if dir1 is a subfolder of dir2. If dir1 equals dir2 the function returns also true.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.isSubfolderOf)")>]
let rec isSubfolderOf (dir2 : DirectoryInfo) (dir1 : DirectoryInfo) = 
    if normalizeFileName dir1.FullName = normalizeFileName dir2.FullName then true
    else if dir1.Parent = null then false
    else dir1.Parent |> isSubfolderOf dir2

/// Checks if the file is in a subfolder of the dir.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.containsFile. NB: reverse parameters)")>]
let isInFolder (dir : DirectoryInfo) (fileInfo : FileInfo) = isSubfolderOf dir fileInfo.Directory

/// Checks if the directory exists on disk.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.exists)")>]
let directoryExists dir = Directory.Exists dir

/// Ensure that directory chain exists. Create necessary directories if necessary.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.DirectoryInfo.ensure)")>]
let inline ensureDirExists (dir : DirectoryInfo) = 
    if not dir.Exists then dir.Create()

/// Checks if the given directory exists. If not then this functions creates the directory.
[<Obsolete("Use Fake.IO instead (FAKE0001 - package: Fake.IO.FileSystem - member: Fake.IO.Directory.ensure)")>]
let inline ensureDirectory dir = directoryInfo dir |> ensureDirExists

/// Detects whether the given path is a directory.
let isDirectory path = 
    let attr = File.GetAttributes path
    attr &&& FileAttributes.Directory = FileAttributes.Directory

/// Detects whether the given path is a file.
let isFile path = isDirectory path |> not

/// Detects whether the given path does not contains invalid characters.
let isValidPath (path:string) =
    Path.GetInvalidPathChars()
    |> Array.filter (fun char -> path.Contains(char.ToString()))
    |> Array.isEmpty
