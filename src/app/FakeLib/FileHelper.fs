[<AutoOpen>]
/// Contains helper function which allow to deal with files and directories.
module Fake.FileHelper

open System.IO
open System.Text
open System.Diagnostics

/// Performs the given actions on all files and subdirectories
let rec recursively dirF fileF (dir : DirectoryInfo) = 
    dir
    |> subDirectories
    |> Seq.iter (fun dir -> 
           recursively dirF fileF dir
           dirF dir)
    dir
    |> filesInDir
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

/// Sets all given files readonly.
let SetReadOnly readOnly (files : string seq) = 
    files |> Seq.iter (fun file -> 
                 let fi = fileInfo file
                 if fi.Exists then fi.IsReadOnly <- readOnly
                 else 
                     file
                     |> directoryInfo
                     |> setDirectoryReadOnly readOnly)

/// Deletes a directory if it exists.
let DeleteDir path = 
    let dir = directoryInfo path
    if dir.Exists then 
        // set all files readonly = false
        !!"/**/*.*"
        |> SetBaseDir dir.FullName
        |> (SetReadOnly false)
        logfn "Deleting %s" dir.FullName
        dir.Delete true
    else logfn "%s does not exist." dir.FullName

/// Creates a directory if it does not exist.
let CreateDir path = 
    let dir = directoryInfo path
    if not dir.Exists then 
        logfn "Creating %s" dir.FullName
        dir.Create()
    else logfn "%s already exists." dir.FullName

/// Creates a file if it does not exist.
let CreateFile fileName = 
    let file = fileInfo fileName
    if not file.Exists then 
        logfn "Creating %s" file.FullName
        let newFile = file.Create()
        newFile.Close()
    else logfn "%s already exists." file.FullName

/// Deletes a file if it exists.
let DeleteFile fileName = 
    let file = fileInfo fileName
    if file.Exists then 
        logfn "Deleting %s" file.FullName
        file.Delete()
    else logfn "%s does not exist." file.FullName

/// Deletes the given files.
let DeleteFiles files = Seq.iter DeleteFile files

/// Active pattern which discriminates between files and directories.
let (|File|Directory|) (fileSysInfo : FileSystemInfo) = 
    match fileSysInfo with
    | :? FileInfo as file -> File(file)
    | :? DirectoryInfo as dir -> Directory(dir, dir.EnumerateFileSystemInfos())
    | _ -> failwith "No file or directory given."

/// Active Pattern for determining file extension.
let (|EndsWith|_|) extension (file : string) = 
    if file.EndsWith extension then Some()
    else None

/// Active Pattern for determining file name.
let (|FileInfoFullName|) (f : FileInfo) = f.FullName

/// Active Pattern for determining FileInfoNameSections.
let (|FileInfoNameSections|) (f : FileInfo) = (f.Name, f.Extension, f.FullName)

/// Copies a single file to the target and overwrites the existing file.
/// ## Parameters
/// 
///  - `target` - The target directory or file.
///  - `fileName` - The FileName.
let CopyFile target fileName = 
    let fi = fileSystemInfo fileName
    match fi with
    | File f -> 
        let targetName = 
            match fileSystemInfo target with
            | Directory _ -> target @@ fi.Name
            | File f' -> f'.FullName
        logVerbosefn "Copy %s to %s" fileName targetName
        f.CopyTo(targetName, true) |> ignore
    | Directory _ -> logVerbosefn "Ignoring %s, because it is a directory." fileName

let private DoCopyFile targetName fileName =
    let fi = fileInfo fileName
    let target = fileInfo targetName
    ensureDirExists target.Directory
    logVerbosefn "Copy %s to %s" fileName targetName
    fi.CopyTo(targetName, true) |> ignore

/// Copies a single file to a relative subfolder of the target.
/// ## Parameters
///
///  - `target` - The target directory
///  - `fileName` - The fileName
let CopyFileIntoSubFolder target fileName =
    let relative = (toRelativePath fileName).TrimStart '.'
    DoCopyFile (target + relative) fileName

/// Copies a single file to the target folder preserving the folder structure
/// starting from the specified base folder.
/// ## Parameters
///
///  - `baseDir` - The base directory.
///  - `target` - The target directory.
///  - `fileName` - The file name.
let CopyFileWithSubfolder baseDir target fileName =
    let fileName = FullName fileName
    let baseDir = FullName baseDir
    let relative = (ProduceRelativePath baseDir fileName).TrimStart '.'
    DoCopyFile (target + relative) fileName

/// Copies several file groups, each represented by a FileIncludes object,
/// to the target folder preserving the folder structure
/// starting from the BaseDirectory of each FileIncludes.
/// ## Parameters
///
///  - `target` - The target directory.
///  - `files` - A sequence of file groups.
let CopyWithSubfoldersTo target files =
    let copyFiles dir inc = Seq.iter (CopyFileWithSubfolder dir target) inc
    Seq.iter (fun inc -> copyFiles inc.BaseDirectory inc) files

/// Copies the files to the target.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `files` - The original file names as a sequence.
let Copy target files = 
    ensureDirectory target
    files |> Seq.iter (CopyFile target)

/// Copies the given files to the target.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `files` - The original file names as a sequence.
let CopyTo target files = Copy target files

/// Copies the files from a cache folder.
/// If the files are not cached or the original files have a different write time the cache will be refreshed.
/// ## Parameters
/// 
///  - `target` - The target FileName.
///  - `cacheDir` - The cache directory.
///  - `files` - The orginal files.
let CopyCached target cacheDir files = 
    let cache = directoryInfo cacheDir
    ensureDirExists cache
    files
    |> Seq.map (fun fileName -> 
           let fi = fileInfo fileName
           let cached = cacheDir @@ fi.Name
           let cachedFi = fileInfo cached
           
           let originalExists = 
               try 
                   fi.Exists
               with exn -> false
           if not originalExists then 
               if not cachedFi.Exists then failwithf "Original file %s and cached file %s do not exist." fileName cached
               else tracefn "Original file %s does not exist, using cached file %s." fileName cached
           else if not cachedFi.Exists || cachedFi.LastWriteTime <> fi.LastWriteTime then 
               tracefn "Cached file %s doesn't exist or is not up to date. Copying file to cache." cached
               CopyFile cacheDir fi.FullName
           else tracefn "Cached file %s is up to date." cached
           CopyFile target cached
           target @@ fi.Name)
    |> Seq.toList

/// Renames the file or directory to the target name.
/// ## Parameters
/// 
///  - `target` - The target file or directory name.
///  - `fileName` - The orginal file or directory name.
let Rename target fileName = (fileInfo fileName).MoveTo target

/// Copies a list of files to the specified directory without any output.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `files` - List of files to copy.
let SilentCopy target files = 
    files |> Seq.iter (fun file -> 
                 let fi = fileInfo file
                 let targetName = target @@ fi.Name
                 let targetFI = fileInfo targetName
                 if targetFI.Exists then 
                     if fi.LastWriteTime > targetFI.LastWriteTime then 
                         targetFI.Attributes <- FileAttributes.Normal
                         fi.CopyTo(targetName, true) |> ignore
                 else fi.CopyTo(targetName) |> ignore)

/// Copies the files to the target - Alias for Copy
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `files` - The orginal file names.
let CopyFiles target files = Copy target files

/// Exclude SVN files (path with .svn)
let excludeSVNFiles (path : string) = not <| path.Contains ".svn"

/// Includes all files
let allFiles (path : string) = true

/// Copies a directory recursivly. If the target directory does not exist, it will be created.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `source` - The source directory.
///  - `filterFile` - A file filter predicate.
let CopyDir target source filterFile = 
    CreateDir target
    Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)
    |> Seq.filter filterFile
    |> Seq.iter (fun file -> 
           let fi = 
               file
               |> replaceFirst source ""
               |> trimSeparator
           
           let newFile = target @@ fi
           logVerbosefn "%s => %s" file newFile
           DirectoryName newFile |> ensureDirectory
           File.Copy(file, newFile, true))
    |> ignore

/// Cleans a directory by removing all files and sub-directories.
let CleanDir path = 
    let di = directoryInfo path
    if di.Exists then 
        logfn "Deleting contents of %s" path
        // delete all files
        Directory.GetFiles(path, "*.*", SearchOption.AllDirectories) |> Seq.iter (fun file -> 
                                                                            let fi = fileInfo file
                                                                            fi.IsReadOnly <- false
                                                                            fi.Delete())
        // deletes all subdirectories
        let rec deleteDirs actDir = 
            Directory.GetDirectories(actDir) |> Seq.iter deleteDirs
            Directory.Delete(actDir, true)
        Directory.GetDirectories path |> Seq.iter deleteDirs
    else CreateDir path
    // set writeable
    File.SetAttributes(path, FileAttributes.Normal)

/// Cleans multiple directories
let CleanDirs dirs = Seq.iter CleanDir dirs

/// Deletes multiple directories
let DeleteDirs dirs = Seq.iter DeleteDir dirs

/// Reads a csv file line by line
/// delimiter is a ,
let ReadCSVFile(file : string) = 
    let csvRegEx = new RegularExpressions.Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))")
    ReadFile file
    |> Seq.map csvRegEx.Split
    |> Seq.map (Array.map (fun s -> s.Trim [| '"' |]))

/// Appends all given files to one file.
/// ## Parameters
/// 
///  - `newFileName` - The target FileName.
///  - `files` - The original FileNames as a sequence.
let AppendTextFiles newFileName files = 
    let fi = fileInfo newFileName
    if fi.Exists then failwithf "File %s already exists." (fi.FullName)
    use writer = new StreamWriter(fi.FullName, false, encoding)
    files |> Seq.iter (fun file -> 
                 logVerbosefn "Appending %s to %s" file fi.FullName
                 ReadFile file |> Seq.iter writer.WriteLine)

/// Checks if the two files are byte-to-byte equal.
let FilesAreEqual (first : FileInfo) (second : FileInfo) = 
    if first.Length <> second.Length then false
    else 
        let BYTES_TO_READ = 32768
        use fs1 = first.OpenRead()
        use fs2 = second.OpenRead()
        let one = Array.create BYTES_TO_READ (byte 0)
        let two = Array.create BYTES_TO_READ (byte 0)
        let mutable eq = true
        while eq && fs1.Read(one, 0, BYTES_TO_READ) <> 0 && fs2.Read(two, 0, BYTES_TO_READ) <> 0 do
            if one <> two then eq <- false
        eq

/// Compares the given files for changes.
/// If delete is set to true then equal files will be removed.
let CompareFiles delete originalFileName compareFileName = 
    let ori = fileInfo originalFileName
    let comp = fileInfo compareFileName
    
    let identical = 
        if not (ori.Exists && comp.Exists && ori.Length = comp.Length) then false
        else ori.LastWriteTime = comp.LastWriteTime || FilesAreEqual ori comp
    if not identical then false
    else 
        if delete then 
            comp.Attributes <- FileAttributes.Normal
            comp.Delete()
            logVerbosefn "Deleting File: %s" comp.FullName
        else logVerbosefn "Files equal: %s" comp.FullName
        true

/// Checks if the directory exists
let TestDir path = 
    let di = directoryInfo path
    if di.Exists then true
    else 
        logfn "%s not found" di.FullName
        false
        
/// Checks if the file exists
let TestFile path = 
    let fi = fileInfo path
    if fi.Exists then true
    else 
        logfn "%s not found" fi.FullName
        false

/// Checks the srcFiles for changes to the last release.
/// ## Parameters
/// 
///  - `lastReleaseDir` - The directory of the last release
///  - `patchDir` - The target directory
///  - `srcFiles` - The source files
///  - `findOldFileF` - A function which finds the old file
let GeneratePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles findOldFileF = 
    let i = ref 0
    for file in srcFiles do
        let newFile = toRelativePath file
        let oldFile = findOldFileF newFile (lastReleaseDir + newFile.TrimStart('.'))
        let fi = fileInfo oldFile
        if not fi.Exists then logVerbosefn "LastRelease has no file like %s" fi.FullName
        if CompareFiles false oldFile newFile |> not then 
            i := !i + 1
            CopyFileIntoSubFolder patchDir newFile
    tracefn "Patch contains %d files." !i

/// Checks the srcFiles for changes to the last release.
/// ## Parameters
/// 
///  - `lastReleaseDir` - The directory of the last release.
///  - `patchDir` - The target directory.
///  - `srcFiles` - The source files.
let GeneratePatch lastReleaseDir patchDir srcFiles = 
    GeneratePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles (fun a b -> b)

/// Copies the file structure recursively.
let rec copyRecursive (dir : DirectoryInfo) (outputDir : DirectoryInfo) overwrite = 
    let files = 
        dir
        |> subDirectories
        |> Seq.fold (fun acc (d : DirectoryInfo) -> 
               let newDir = outputDir.FullName @@ d.Name
                            |> directoryInfo
               if not newDir.Exists then newDir.Create()
               copyRecursive d newDir overwrite @ acc) []
    (dir
     |> filesInDir
     |> Seq.map (fun f -> 
            let newFileName = outputDir.FullName @@ f.Name
            f.CopyTo(newFileName, overwrite) |> ignore
            newFileName)
     |> Seq.toList) @ files

/// Copies the file structure recursively.
let CopyRecursive dir outputDir = copyRecursive (directoryInfo dir) (directoryInfo outputDir)

/// Moves a single file to the target and overwrites the existing file.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `fileName` - The FileName.
let MoveFile target fileName = 
    let fi = fileSystemInfo fileName
    match fi with
    | File f -> 
        let targetName = target @@ fi.Name
        let targetInfo = fileInfo targetName
        if targetInfo.Exists then targetInfo.Delete()
        logVerbosefn "Move %s to %s" fileName targetName
        f.MoveTo(targetName) |> ignore
    | Directory _ -> logVerbosefn "Ignoring %s, because it is a directory." fileName

/// Creates a config file with the parameters as "key;value" lines
let WriteConfigFile configFileName parameters = 
    if isNullOrEmpty configFileName then ()
    else 
        let fi = fileInfo configFileName
        if fi.Exists then fi.Delete()
        use streamWriter = fi.CreateText()
        for (key, value) in parameters do
            streamWriter.WriteLine("{0};{1}", key, value)

/// Replaces all occurences of the patterns in the given files with the given replacements.
/// ## Parameters
///
///  - `replacements` - A sequence of tuples with the patterns and the replacements.
///  - `files` - The files to process.
let ReplaceInFiles replacements files = processTemplates replacements files

/// Replace all occurences of the regex pattern with the given replacement in the specified file
/// ## Parameters
///
/// - `pattern` - The string to search for a match
/// - `replacement` - The replacement string
/// - `encoding` - The encoding to use when reading and writing the file
/// - `file` - The path of the file to process
let RegexReplaceInFileWithEncoding pattern (replacement:string) encoding file =
    let oldContent = File.ReadAllText(file, encoding)
    let newContent = System.Text.RegularExpressions.Regex.Replace(oldContent, pattern, replacement)
    File.WriteAllText(file, newContent, encoding)

/// Replace all occurences of the regex pattern with the given replacement in the specified files
/// ## Parameters
///
/// - `pattern` - The string to search for a match
/// - `replacement` - The replacement string
/// - `encoding` - The encoding to use when reading and writing the files
/// - `files` - The paths of the files to process
let RegexReplaceInFilesWithEncoding pattern (replacement:string) encoding files =
    files |> Seq.iter (RegexReplaceInFileWithEncoding pattern replacement encoding)

/// Get the version a file.
/// ## Parameters
///
///  - 'fileName' - Name of file from which the version is retrieved. The path can be relative.
let FileVersion(fileName : string) = 
    FullName fileName
    |> FileVersionInfo.GetVersionInfo
    |> fun x -> x.FileVersion.ToString()

/// Get the filename extension including the leading '.', or an empty string if the file has no extension.
/// ## Parameters
///
/// - 'fileName' - Name of the file from which the extension is retrieved.
let ext fileName = Path.GetExtension fileName

/// Change the extension of the file.
/// ## Parameters
///
/// - 'extension' - The new extension containing the leading '.'.
/// - 'fileName' - Name of the file from which the extension is retrieved.
let changeExt extension fileName = Path.ChangeExtension(fileName, extension)

/// Tests whether the file has specified extensions (containing the leading '.')
/// ## Parameters
///
/// - 'extension' - The extension to fine containing the leading '.'.
/// - 'fileName' - Name of the file from which the extension is retrieved.
let hasExt extension fileName = System.String.Equals(ext fileName, extension, System.StringComparison.InvariantCultureIgnoreCase)

/// Get the filename for the specified path
/// ## Parameters
///
/// - 'path' - The path from which the filename is retrieved.
let filename path = Path.GetFileName path

/// Get the filename for the specified path without it's extension
/// ## Parameters
///
/// - 'path' - The path from which the filename is retrieved.
let fileNameWithoutExt path = Path.GetFileNameWithoutExtension path

[<System.Obsolete("This was a typo - please use fileNameWithoutExt")>]
let filenameWithouExt path = Path.GetFileNameWithoutExtension path

/// Get the directory of the specified path
/// ## Parameters
///
/// - 'path' - The path from which the directory is retrieved.
let directory path = Path.GetDirectoryName path
