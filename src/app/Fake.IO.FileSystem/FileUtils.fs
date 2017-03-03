/// Shell-like functions. Similar to [Ruby's FileUtils](http://www.ruby-doc.org/stdlib-2.0.0/libdoc/rake/rdoc/FileUtils.html).
module Fake.IO.FileSystem.Shell

open System.IO
open Fake.Core
open Fake.IO.FileSystem.Operators
open Fake.IO.FileSystem.FileSystemInfo

/// Copies a single file to the target and overwrites the existing file.
/// ## Parameters
/// 
///  - `target` - The target directory or file.
///  - `fileName` - The FileName.
let CopyFile target fileName = 
    let fi = ofPath fileName
    match fi with
    | File f -> 
        let targetName = 
            match ofPath target with
            | Directory _ -> target @@ fi.Name
            | File f' -> f'.FullName
        //TODO: logVerbosefn "Copy %s to %s" fileName targetName
        f.CopyTo(targetName, true) |> ignore
    | Directory _ -> () //TODO: logVerbosefn "Ignoring %s, because it is a directory." fileName

let private DoCopyFile targetName fileName =
    let fi = FileInfo.ofPath fileName
    let target = FileInfo.ofPath targetName
    DirectoryInfo.ensure target.Directory
    //TODO: logVerbosefn "Copy %s to %s" fileName targetName
    fi.CopyTo(targetName, true) |> ignore

/// Copies a single file to a relative subfolder of the target.
/// ## Parameters
///
///  - `target` - The target directory
///  - `fileName` - The fileName
let CopyFileIntoSubFolder target fileName =
    let relative = (Path.toRelativeFromCurrent fileName).TrimStart '.'
    DoCopyFile (target + relative) fileName

/// Copies a single file to the target folder preserving the folder structure
/// starting from the specified base folder.
/// ## Parameters
///
///  - `baseDir` - The base directory.
///  - `target` - The target directory.
///  - `fileName` - The file name.
let CopyFileWithSubfolder baseDir target fileName =
    let fileName = Path.GetFullPath fileName
    let baseDir = Path.GetFullPath baseDir
    let relative = (Path.toRelativeFrom baseDir fileName).TrimStart '.'
    DoCopyFile (target + relative) fileName

/// Copies the files to the target.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `files` - The original file names as a sequence.
let Copy target files = 
    Directory.ensure target
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
    let cache = DirectoryInfo.ofPath cacheDir
    DirectoryInfo.ensure cache
    files
    |> Seq.map (fun fileName -> 
           let fi = FileInfo.ofPath fileName
           let cached = cacheDir @@ fi.Name
           let cachedFi = FileInfo.ofPath cached
           
           let originalExists = 
               try 
                   fi.Exists
               with exn -> false
           if not originalExists then 
               if not cachedFi.Exists then failwithf "Original file %s and cached file %s do not exist." fileName cached
               else () //TODO: tracefn "Original file %s does not exist, using cached file %s." fileName cached
           else if not cachedFi.Exists || cachedFi.LastWriteTime <> fi.LastWriteTime then 
               () //TODO: tracefn "Cached file %s doesn't exist or is not up to date. Copying file to cache." cached
               CopyFile cacheDir fi.FullName
           else () //TODO: tracefn "Cached file %s is up to date." cached
           CopyFile target cached
           target @@ fi.Name)
    |> Seq.toList

/// Renames the file or directory to the target name.
/// ## Parameters
/// 
///  - `target` - The target file or directory name.
///  - `fileName` - The orginal file or directory name.
let Rename target fileName = (FileInfo.ofPath fileName).MoveTo target

/// Copies a list of files to the specified directory without any output.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `files` - List of files to copy.
let SilentCopy target files = 
    files |> Seq.iter (fun file -> 
                 let fi = FileInfo.ofPath file
                 let targetName = target @@ fi.Name
                 let targetFI = FileInfo.ofPath targetName
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


/// Copies a directory recursivly. If the target directory does not exist, it will be created.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `source` - The source directory.
///  - `filterFile` - A file filter predicate.
let CopyDir target source filterFile = 
    Directory.ensure target
    Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)
    |> Seq.filter filterFile
    |> Seq.iter (fun file -> 
           let fi = 
               file
               |> String.replaceFirst source ""
               |> String.trimSeparator
           
           let newFile = target @@ fi
           () //TODO: logVerbosefn "%s => %s" file newFile
           Path.getDirectory newFile |> Directory.ensure
           File.Copy(file, newFile, true))
    |> ignore

/// Cleans a directory by removing all files and sub-directories.
let CleanDir path = 
    let di = DirectoryInfo.ofPath path
    if di.Exists then 
        () //TODO: logfn "Deleting contents of %s" path
        // delete all files
        Directory.GetFiles(path, "*.*", SearchOption.AllDirectories) |> Seq.iter (fun file -> 
                                                                            let fi = FileInfo.ofPath file
                                                                            fi.IsReadOnly <- false
                                                                            fi.Delete())
        // deletes all subdirectories
        let rec deleteDirs actDir = 
            Directory.GetDirectories(actDir) |> Seq.iter deleteDirs
            Directory.Delete(actDir, true)
        Directory.GetDirectories path |> Seq.iter deleteDirs
    else Directory.ensure path
    // set writeable
    File.SetAttributes(path, FileAttributes.Normal)

/// Cleans multiple directories
let CleanDirs dirs = Seq.iter CleanDir dirs

/// Compat
let DeleteDir dir = Directory.delete dir

/// Deletes multiple directories
let DeleteDirs dirs = Seq.iter Directory.delete dirs

/// Compat
let ensureDirectory dir = Directory.ensure dir

/// Appends all given files to one file.
/// ## Parameters
/// 
///  - `newFileName` - The target FileName.
///  - `files` - The original FileNames as a sequence.
let AppendTextFilesWithEncoding encoding newFileName files = 
    let fi = FileInfo.ofPath newFileName
    if fi.Exists then failwithf "File %s already exists." (fi.FullName)
    use file = fi.Open(FileMode.Create)
    use writer = new StreamWriter(file, encoding)
    files |> Seq.iter (File.Read >> Seq.iter writer.WriteLine)
                 //() //TODO: logVerbosefn "Appending %s to %s" file fi.FullName
                 //)

/// Appends all given files to one file.
/// ## Parameters
/// 
///  - `newFileName` - The target FileName.
///  - `files` - The original FileNames as a sequence.
let AppendTextFiles newFileName files = AppendTextFilesWithEncoding System.Text.Encoding.UTF8 newFileName files

/// Compares the given files for changes.
/// If delete is set to true then equal files will be removed.
let CompareFiles delete originalFileName compareFileName = 
    let ori = FileInfo.ofPath originalFileName
    let comp = FileInfo.ofPath compareFileName
    
    let identical = 
        if not (ori.Exists && comp.Exists && ori.Length = comp.Length) then false
        else ori.LastWriteTime = comp.LastWriteTime || FileInfo.contentIsEqualTo ori comp
    if not identical then false
    else 
        if delete then 
            comp.Attributes <- FileAttributes.Normal
            comp.Delete()
            () //TODO: logVerbosefn "Deleting File: %s" comp.FullName
        else () //TODO: logVerbosefn "Files equal: %s" comp.FullName
        true

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
        let newFile = Path.toRelativeFromCurrent file
        let oldFile = findOldFileF newFile (lastReleaseDir + newFile.TrimStart('.'))
        let fi = FileInfo.ofPath oldFile
        if not fi.Exists then () //TODO: logVerbosefn "LastRelease has no file like %s" fi.FullName
        if CompareFiles false oldFile newFile |> not then 
            i := !i + 1
            CopyFileIntoSubFolder patchDir newFile
    () //TODO: tracefn "Patch contains %d files." !i

/// Checks the srcFiles for changes to the last release.
/// ## Parameters
/// 
///  - `lastReleaseDir` - The directory of the last release.
///  - `patchDir` - The target directory.
///  - `srcFiles` - The source files.
let GeneratePatch lastReleaseDir patchDir srcFiles = 
    GeneratePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles (fun a b -> b)
    
/// Checks if the directory exists
let TestDir path = 
    let di = DirectoryInfo.ofPath path
    if di.Exists then true
    else 
        () //TODO: logfn "%s not found" di.FullName
        false
        
/// Checks if the file exists
let TestFile path = 
    let fi = FileInfo.ofPath path
    if fi.Exists then true
    else 
        () //TODO: logfn "%s not found" fi.FullName
        false


/// Copies the file structure recursively.
let CopyRecursive dir outputDir overWrite = DirectoryInfo.copyRecursiveTo overWrite (DirectoryInfo.ofPath outputDir) (DirectoryInfo.ofPath dir) 
let CopyRecursiveTo overWrite outputDir dir  = DirectoryInfo.copyRecursiveTo overWrite (DirectoryInfo.ofPath outputDir) (DirectoryInfo.ofPath dir) 

/// Moves a single file to the target and overwrites the existing file.
/// ## Parameters
/// 
///  - `target` - The target directory.
///  - `fileName` - The FileName.
let MoveFile target fileName = 
    let fi = FileSystemInfo.ofPath fileName
    match fi with
    | File f -> 
        let targetName = target @@ fi.Name
        let targetInfo = FileInfo.ofPath targetName
        if targetInfo.Exists then targetInfo.Delete()
        () //TODO: logVerbosefn "Move %s to %s" fileName targetName
        f.MoveTo(targetName) |> ignore
    | Directory _ -> () //TODO: logVerbosefn "Ignoring %s, because it is a directory." fileName

/// Creates a config file with the parameters as "key;value" lines
let WriteConfigFile configFileName parameters = 
    if String.isNullOrEmpty configFileName then ()
    else 
        let fi = FileInfo.ofPath configFileName
        if fi.Exists then fi.Delete()
        use streamWriter = fi.CreateText()
        for (key, value) in parameters do
            streamWriter.WriteLine("{0};{1}", key, value)

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


/// Deletes a file if it exists
let rm fileName = File.DeleteFile fileName

/// Like "rm -rf" in a shell. Removes files recursively, ignoring nonexisting files
let rm_rf f = 
    if Directory.Exists f then Directory.delete f
    else File.Delete f

/// Creates a directory if it doesn't exist.
let mkdir path = Directory.CreateDir path

/// <summary>
/// Like "cp -r" in a shell. Copies a file or directory recursively.
/// </summary>
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
let cp_r src dest = 
    if Directory.Exists src then CopyDir dest src (fun _ -> true)
    else CopyFile dest src

/// Like "cp" in a shell. Copies a single file.
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
let cp src dest = CopyFile dest src

/// Changes working directory
let chdir path = Directory.SetCurrentDirectory path

/// Changes working directory
let cd path = chdir path

/// Gets working directory
let pwd = Directory.GetCurrentDirectory

/// The stack of directories operated on by pushd and popd
let dirStack = new System.Collections.Generic.Stack<string>()

/// Store the current directory in the directory stack before changing to a new one
let pushd path = 
    dirStack.Push(pwd())
    cd path

/// Restore the previous directory stored in the stack
let popd () = 
    cd <| dirStack.Pop()

/// Like "mv" in a shell. Moves/renames a file
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
let mv src dest = MoveFile src dest
