/// Shell-like functions. Similar to [Ruby's FileUtils](http://www.ruby-doc.org/stdlib-2.0.0/libdoc/rake/rdoc/FileUtils.html).
namespace Fake.IO

open System
open System.IO
open Fake.Core
open Fake.IO.FileSystemOperators

[<RequireQualifiedAccess>]
module Shell =

    /// Copies a single file to the target and overwrites the existing file.
    /// ## Parameters
    ///
    ///  - `target` - The target directory or file.
    ///  - `fileName` - The FileName.
    let copyFile target fileName =
        let fi = FileSystemInfo.ofPath fileName
        match fi with
        | FileSystemInfo.File f ->
            let targetName =
                match FileSystemInfo.ofPath target with
                | FileSystemInfo.Directory _ -> target @@ fi.Name
                | FileSystemInfo.File f' -> f'.FullName
            //TODO: logVerbosefn "Copy %s to %s" fileName targetName
            f.CopyTo(targetName, true) |> ignore
        | FileSystemInfo.Directory _ -> () //TODO: logVerbosefn "Ignoring %s, because it is a directory." fileName

    let private doCopyFile targetName fileName =
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
    let copyFileIntoSubFolder target fileName =
        let relative = (Path.toRelativeFromCurrent fileName).TrimStart '.'
        doCopyFile (target + relative) fileName

    /// Copies a single file to the target folder preserving the folder structure
    /// starting from the specified base folder.
    /// ## Parameters
    ///
    ///  - `baseDir` - The base directory.
    ///  - `target` - The target directory.
    ///  - `fileName` - The file name.
    let copyFileWithSubfolder baseDir target fileName =
        let fileName = Path.GetFullPath fileName
        let baseDir = Path.GetFullPath baseDir
        let relative = (Path.toRelativeFrom baseDir fileName).TrimStart '.'
        doCopyFile (target + relative) fileName

    /// Copies the files to the target.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `files` - The original file names as a sequence.
    let copy target files =
        Directory.ensure target
        files |> Seq.iter (copyFile target)

    /// Copies the given files to the target.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `files` - The original file names as a sequence.
    let copyTo target files = copy target files

    /// Copies the files from a cache folder.
    /// If the files are not cached or the original files have a different write time the cache will be refreshed.
    /// ## Parameters
    ///
    ///  - `target` - The target FileName.
    ///  - `cacheDir` - The cache directory.
    ///  - `files` - The orginal files.
    let copyCached target cacheDir files =
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
                   copyFile cacheDir fi.FullName
               else () //TODO: tracefn "Cached file %s is up to date." cached
               copyFile target cached
               target @@ fi.Name)
        |> Seq.toList

    /// Renames the file or directory to the target name.
    /// ## Parameters
    ///
    ///  - `target` - The target file or directory name.
    ///  - `fileName` - The orginal file or directory name.
    let rename target fileName =
        let fsi = FileSystemInfo.ofPath fileName
        FileSystemInfo.moveTo fsi target

    /// Copies a list of files to the specified directory without any output.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `files` - List of files to copy.
    let silentCopy target files =
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
    let copyFiles target files = copy target files

    /// Copies the given glob-matches into another directory by leaving relative paths in place based on the globbing base-directory
    ///
    /// ## Sample
    /// 
    ///      !! "**/My*Glob*.exe"
    ///      |> GlobbingPattern.setBaseDir "baseDir"
    ///      |> Shell.copyFilesWithSubFolder "targetDir"
    ///
    let copyFilesWithSubFolder targetDir (files:IGlobbingPattern) =
      let baseFull = (Path.GetFullPath files.BaseDirectory).TrimEnd [|'/';'\\'|]
      for item in files do
        let info = FileInfo.ofPath item
        let itemSpec =
          // first get relative path from the globbing
          let relative = (info.FullName.Substring (baseFull.Length+1))
          relative
        let parent = Path.GetDirectoryName (targetDir</>itemSpec)
        Directory.ensure parent
        File.Copy(item, targetDir</>itemSpec, true)


    /// Copies a directory recursivly. If the target directory does not exist, it will be created.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `source` - The source directory.
    ///  - `filterFile` - A file filter predicate.
    let copyDir target source filterFile =
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
    let cleanDir path =
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
    let cleanDirs dirs = Seq.iter cleanDir dirs

    /// Compat
    let deleteDir dir = Directory.delete dir

    /// Deletes multiple directories
    let deleteDirs dirs = Seq.iter Directory.delete dirs

    /// Appends all given files to one file.
    /// ## Parameters
    ///
    ///  - `newFileName` - The target FileName.
    ///  - `files` - The original FileNames as a sequence.
    let appendTextFilesWithEncoding encoding newFileName files =
        let fi = FileInfo.ofPath newFileName
        if fi.Exists then failwithf "File %s already exists." (fi.FullName)
        use file = fi.Open(FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        files |> Seq.iter (File.read >> Seq.iter writer.WriteLine)
                     //() //TODO: logVerbosefn "Appending %s to %s" file fi.FullName
                     //)

    /// Appends all given files to one file.
    /// ## Parameters
    ///
    ///  - `newFileName` - The target FileName.
    ///  - `files` - The original FileNames as a sequence.
    let appendTextFiles newFileName files = appendTextFilesWithEncoding System.Text.Encoding.UTF8 newFileName files

    /// Compares the given files for changes.
    /// If delete is set to true then equal files will be removed.
    let compareFiles delete originalFileName compareFileName =
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
    let generatePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles findOldFileF =
        let i = ref 0
        for file in srcFiles do
            let newFile = Path.toRelativeFromCurrent file
            let oldFile = findOldFileF newFile (lastReleaseDir + newFile.TrimStart('.'))
            let fi = FileInfo.ofPath oldFile
            if not fi.Exists then () //TODO: logVerbosefn "LastRelease has no file like %s" fi.FullName
            if compareFiles false oldFile newFile |> not then
                i := !i + 1
                copyFileIntoSubFolder patchDir newFile
        () //TODO: tracefn "Patch contains %d files." !i

    /// Checks the srcFiles for changes to the last release.
    /// ## Parameters
    ///
    ///  - `lastReleaseDir` - The directory of the last release.
    ///  - `patchDir` - The target directory.
    ///  - `srcFiles` - The source files.
    let generatePatch lastReleaseDir patchDir srcFiles =
        generatePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles (fun _ b -> b)

    /// Checks if the directory exists
    let testDir path =
        let di = DirectoryInfo.ofPath path
        if di.Exists then true
        else
            () //TODO: logfn "%s not found" di.FullName
            false

    /// Checks if the file exists
    let testFile path =
        let fi = FileInfo.ofPath path
        if fi.Exists then true
        else
            () //TODO: logfn "%s not found" fi.FullName
            false


    /// Copies the file structure recursively.
    let copyRecursive dir outputDir overWrite = DirectoryInfo.copyRecursiveTo overWrite (DirectoryInfo.ofPath outputDir) (DirectoryInfo.ofPath dir)
    let inline copyRecursiveTo overWrite outputDir dir  = copyRecursive dir outputDir overWrite

    [<NoComparison; NoEquality>]
    type CopyRecursiveMethod =
    | Overwrite
    | NoOverwrite
    | Skip
    | IncludePattern of string
    | ExcludePattern of string
    | Filter of (DirectoryInfo -> FileInfo -> bool)

    open Fake.IO.Globbing
    /// Copies the file structure recursively.
    /// ## Parameters
    ///
    ///  - `method` - the method to decide which files get copied
    ///  - `dir` - The source directory.
    ///  - `outputDir` - The target directory.
    let copyRecursive2 method dir outputDir =
        let dirInfo = DirectoryInfo.ofPath dir
        let outputDirInfo = DirectoryInfo.ofPath outputDir
        let copyRecursiveWithFilter f = DirectoryInfo.copyRecursiveToWithFilter false f outputDirInfo dirInfo
        match method with
        | Overwrite -> DirectoryInfo.copyRecursiveTo true dirInfo outputDirInfo
        | NoOverwrite -> DirectoryInfo.copyRecursiveTo false dirInfo outputDirInfo
        | Skip -> copyRecursiveWithFilter <| fun d f -> d.FullName @@ f.Name |> File.Exists |> not
        | IncludePattern(pattern) ->
            copyRecursiveWithFilter <| fun d f -> d.FullName @@ f.Name |> (Glob.isMatch pattern)
        | ExcludePattern(pattern) ->
            copyRecursiveWithFilter <| fun d f -> d.FullName @@ f.Name |> (Glob.isMatch pattern) |> not
        | Filter(f) -> copyRecursiveWithFilter f

    /// Moves a single file to the target and overwrites the existing file.
    /// If `fileName` is a directory the functions does nothing.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `fileName` - The FileName.
    let moveFile target fileName =
        let fi = FileSystemInfo.ofPath fileName
        match fi with
        | FileSystemInfo.File f ->
            let targetName = target @@ fi.Name
            let targetInfo = FileInfo.ofPath targetName
            if targetInfo.Exists then targetInfo.Delete()
            f.MoveTo(targetName)
        | FileSystemInfo.Directory _ ->
            // HISTORIC: Ideally we would throw here.
            ()

    let private moveDir target fileName =
        let fi = FileSystemInfo.ofPath fileName
        match fi with
        | FileSystemInfo.Directory (d, _) ->
            let targetName = target @@ fi.Name
            d.MoveTo(targetName)
        | FileSystemInfo.File _ ->
            failwithf "moveDir only works on directories but '%s' was a file." fileName

    /// Creates a config file with the parameters as "key;value" lines
    let writeConfigFile configFileName parameters =
        if String.isNullOrEmpty configFileName then ()
        else
            let fi = FileInfo.ofPath configFileName
            if fi.Exists then fi.Delete()
            use streamWriter = fi.CreateText()
            for (key, value) in parameters do
                streamWriter.WriteLine("{0};{1}", key, value)

    /// Replaces all occurences of the patterns in the given files with the given replacements.
    /// ## Parameters
    ///
    ///  - `replacements` - A sequence of tuples with the patterns and the replacements.
    ///  - `files` - The files to process.
    let replaceInFiles replacements files = Templates.replaceInFiles replacements files

    /// Replace all occurences of the regex pattern with the given replacement in the specified file
    /// ## Parameters
    ///
    /// - `pattern` - The string to search for a match
    /// - `replacement` - The replacement string
    /// - `encoding` - The encoding to use when reading and writing the file
    /// - `file` - The path of the file to process
    let regexReplaceInFileWithEncoding pattern (replacement:string) encoding file =
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
    let regexReplaceInFilesWithEncoding pattern (replacement:string) encoding files =
        files |> Seq.iter (regexReplaceInFileWithEncoding pattern replacement encoding)

    /// Copies a single file to the target and overwrites the existing file.
    /// ## Parameters
    ///
    ///  - `target` - The target directory or file.
    ///  - `fileName` - The FileName.
    [<Obsolete("Please use copyFile instead")>]
    let CopyFile target fileName = copyFile target fileName

    /// Copies a single file to a relative subfolder of the target.
    /// ## Parameters
    ///
    ///  - `target` - The target directory
    ///  - `fileName` - The fileName
    [<Obsolete("Please use copyFileIntoSubFolder instead")>]
    let CopyFileIntoSubFolder target fileName = copyFileIntoSubFolder target fileName

    /// Copies a single file to the target folder preserving the folder structure
    /// starting from the specified base folder.
    /// ## Parameters
    ///
    ///  - `baseDir` - The base directory.
    ///  - `target` - The target directory.
    ///  - `fileName` - The file name.
    [<Obsolete("Please use copyFileWithSubfolder instead")>]
    let CopyFileWithSubfolder baseDir target fileName =
        copyFileWithSubfolder baseDir target fileName

    /// Copies the files to the target.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `files` - The original file names as a sequence.
    [<Obsolete("Please use copy instead")>]
    let Copy target files = copy target files

    /// Copies the given files to the target.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `files` - The original file names as a sequence.
    [<Obsolete("Please use copyTo instead")>]
    let CopyTo target files = copyTo target files

    /// Copies the files from a cache folder.
    /// If the files are not cached or the original files have a different write time the cache will be refreshed.
    /// ## Parameters
    ///
    ///  - `target` - The target FileName.
    ///  - `cacheDir` - The cache directory.
    ///  - `files` - The orginal files.
    [<Obsolete("Please use copyCached instead")>]
    let CopyCached target cacheDir files =
        copyCached target cacheDir files

    /// Renames the file or directory to the target name.
    /// ## Parameters
    ///
    ///  - `target` - The target file or directory name.
    ///  - `fileName` - The orginal file or directory name.
    [<Obsolete("Please use rename instead")>]
    let Rename target fileName = rename target fileName

    /// Copies a list of files to the specified directory without any output.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `files` - List of files to copy.
    [<Obsolete("Please use silentCopy instead")>]
    let SilentCopy target files =
        silentCopy target files

    /// Copies the files to the target - Alias for Copy
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `files` - The orginal file names.
    [<Obsolete("Please use copyFiles instead")>]
    let CopyFiles target files = copyFiles target files


    /// Copies a directory recursivly. If the target directory does not exist, it will be created.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `source` - The source directory.
    ///  - `filterFile` - A file filter predicate.
    [<Obsolete("Please use copyDir instead")>]
    let CopyDir target source filterFile =
        copyDir target source filterFile

    /// Cleans a directory by removing all files and sub-directories.
    [<Obsolete("Please use cleanDir instead")>]
    let CleanDir path = cleanDir path

    /// Cleans multiple directories
    [<Obsolete("Please use cleanDirs instead")>]
    let CleanDirs dirs = cleanDirs dirs

    /// Compat
    [<Obsolete("Please use deleteDir instead")>]
    let DeleteDir dir = deleteDir dir

    /// Deletes multiple directories
    [<Obsolete("Please use deleteDirs instead")>]
    let DeleteDirs dirs = deleteDirs dirs

    /// Appends all given files to one file.
    /// ## Parameters
    ///
    ///  - `newFileName` - The target FileName.
    ///  - `files` - The original FileNames as a sequence.
    [<Obsolete("Please use appendTextFilesWithEncoding instead")>]
    let AppendTextFilesWithEncoding encoding newFileName files =
        appendTextFilesWithEncoding encoding newFileName files

    /// Appends all given files to one file.
    /// ## Parameters
    ///
    ///  - `newFileName` - The target FileName.
    ///  - `files` - The original FileNames as a sequence.
    [<Obsolete("Please use appendTextFiles instead")>]
    let AppendTextFiles newFileName files =
        appendTextFiles newFileName files

    /// Compares the given files for changes.
    /// If delete is set to true then equal files will be removed.
    [<Obsolete("Please use compareFiles instead")>]
    let CompareFiles delete originalFileName compareFileName =
        compareFiles delete originalFileName compareFileName

    /// Checks the srcFiles for changes to the last release.
    /// ## Parameters
    ///
    ///  - `lastReleaseDir` - The directory of the last release
    ///  - `patchDir` - The target directory
    ///  - `srcFiles` - The source files
    ///  - `findOldFileF` - A function which finds the old file
    [<Obsolete("Please use generatePatchWithFindOldFileFunction instead")>]
    let GeneratePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles findOldFileF =
        generatePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles findOldFileF

    /// Checks the srcFiles for changes to the last release.
    /// ## Parameters
    ///
    ///  - `lastReleaseDir` - The directory of the last release.
    ///  - `patchDir` - The target directory.
    ///  - `srcFiles` - The source files.
    [<Obsolete("Please use generatePatch instead")>]
    let GeneratePatch lastReleaseDir patchDir srcFiles =
        generatePatch lastReleaseDir patchDir srcFiles

    /// Checks if the directory exists
    [<Obsolete("Please use testDir instead")>]
    let TestDir path = testDir path

    /// Checks if the file exists
    [<Obsolete("Please use testFile instead")>]
    let TestFile path = testFile path

    /// Copies the file structure recursively.
    [<Obsolete("Please use copyRecursive instead")>]
    let CopyRecursive dir outputDir overWrite = copyRecursive dir outputDir overWrite
    [<Obsolete("Please use copyRecursiveTo instead")>]
    let inline CopyRecursiveTo overWrite outputDir dir  = copyRecursiveTo overWrite outputDir dir

    /// Copies the file structure recursively.
    /// ## Parameters
    ///
    ///  - `method` - the method to decide which files get copied
    ///  - `dir` - The source directory.
    ///  - `outputDir` - The target directory.
    [<Obsolete("Please use copyRecursive2 instead")>]
    let CopyRecursive2 method dir outputDir =
        copyRecursive2 method dir outputDir

    /// Moves a single file to the target and overwrites the existing file.
    /// ## Parameters
    ///
    ///  - `target` - The target directory.
    ///  - `fileName` - The FileName.
    [<Obsolete("Please use moveFile instead")>]
    let MoveFile target fileName =
        moveFile target fileName

    /// Creates a config file with the parameters as "key;value" lines
    [<Obsolete("Please use writeConfigFile instead")>]
    let WriteConfigFile configFileName parameters =
        writeConfigFile configFileName parameters

    /// Replaces all occurences of the patterns in the given files with the given replacements.
    /// ## Parameters
    ///
    ///  - `replacements` - A sequence of tuples with the patterns and the replacements.
    ///  - `files` - The files to process.
    [<Obsolete("Please use replaceInFiles instead")>]
    let ReplaceInFiles replacements files =
        replaceInFiles replacements files

    /// Replace all occurences of the regex pattern with the given replacement in the specified file
    /// ## Parameters
    ///
    /// - `pattern` - The string to search for a match
    /// - `replacement` - The replacement string
    /// - `encoding` - The encoding to use when reading and writing the file
    /// - `file` - The path of the file to process
    [<Obsolete("Please use regexReplaceInFileWithEncoding instead")>]
    let RegexReplaceInFileWithEncoding pattern (replacement:string) encoding file =
        regexReplaceInFileWithEncoding pattern replacement encoding file

    /// Replace all occurences of the regex pattern with the given replacement in the specified files
    /// ## Parameters
    ///
    /// - `pattern` - The string to search for a match
    /// - `replacement` - The replacement string
    /// - `encoding` - The encoding to use when reading and writing the files
    /// - `files` - The paths of the files to process
    [<Obsolete("Please use regexReplaceInFilesWithEncoding instead")>]
    let RegexReplaceInFilesWithEncoding pattern (replacement:string) encoding files =
        regexReplaceInFilesWithEncoding pattern replacement encoding files


    /// Deletes a file if it exists
    let rm fileName = File.delete fileName

    /// Like "rm -rf" in a shell. Removes files recursively, ignoring nonexisting files
    let rm_rf f =
        if Directory.Exists f then Directory.delete f
        else File.delete f

    /// Creates a directory if it doesn't exist.
    let mkdir path = Directory.create path

    /// <summary>
    /// Like "cp -r" in a shell. Copies a file or directory recursively.
    /// </summary>
    /// <param name="src">The source</param>
    /// <param name="dest">The destination</param>
    let cp_r src dest =
        if Directory.Exists src then copyDir dest src (fun _ -> true)
        else copyFile dest src

    /// Like "cp" in a shell. Copies a single file.
    /// <param name="src">The source</param>
    /// <param name="dest">The destination</param>
    let cp src dest = copyFile dest src

    /// Changes working directory
    let chdir path = Directory.SetCurrentDirectory path

    /// Changes working directory
    let cd path = chdir path

    /// Gets working directory
    let pwd = Directory.GetCurrentDirectory

    /// The stack of directories operated on by pushd and popd
    let private dirStack = new System.Collections.Generic.Stack<string>()

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
    let mv src dest =
        match FileSystemInfo.ofPath src, FileSystemInfo.ofPath dest with
        | _, destFi when not destFi.Exists -> rename dest src
        | FileSystemInfo.File _, FileSystemInfo.File destFi ->
            destFi.Delete()
            rename dest src
        | FileSystemInfo.Directory _, FileSystemInfo.File _ ->
            failwithf "Cannot move a directory %s to a file %s" src dest
        | FileSystemInfo.File srcFi, FileSystemInfo.Directory _ ->
            moveFile dest src
        | FileSystemInfo.Directory srcFi, FileSystemInfo.Directory _ ->
            moveDir dest src
