namespace Fake.IO

open System.IO
open Fake.Core
open Fake.IO.FileSystemOperators

/// <summary>
/// Shell-like functions. Similar to
/// <a href="http://www.ruby-doc.org/stdlib-2.0.0/libdoc/rake/rdoc/FileUtils.html">Ruby's FileUtils</a>.
/// </summary>
[<RequireQualifiedAccess>]
module Shell =

    /// <summary>
    /// Copies a single file to the target and overwrites the existing file.
    /// </summary>
    /// 
    /// <param name="target">The target directory or file.</param>
    /// <param name="fileName">The FileName.</param>
    let copyFile target fileName =
        let fi = FileSystemInfo.ofPath fileName
        match fi with
        | FileSystemInfo.File f ->
            let targetName =
                match FileSystemInfo.ofPath target with
                | FileSystemInfo.Directory _ -> target @@ fi.Name
                | FileSystemInfo.File f' -> f'.FullName
            Trace.traceVerbose <| sprintf "Copy %s to %s" fileName targetName
            f.CopyTo(targetName, true) |> ignore
        | FileSystemInfo.Directory _ ->
            Trace.traceVerbose <| sprintf "Ignoring %s, because it is a directory." fileName
            ()

    let private doCopyFile targetName fileName =
        let fi = FileInfo.ofPath fileName
        let target = FileInfo.ofPath targetName
        DirectoryInfo.ensure target.Directory
        Trace.traceVerbose <| sprintf "Copy %s to %s" fileName targetName
        fi.CopyTo(targetName, true) |> ignore

    /// <summary>
    /// Copies a single file to a relative subfolder of the target.
    /// </summary>
    /// 
    /// <param name="target">The target directory</param>
    /// <param name="fileName">The FileName.</param>
    let copyFileIntoSubFolder target fileName =
        let relative = (Path.toRelativeFromCurrent fileName).TrimStart '.'
        doCopyFile (target + relative) fileName

    /// <summary>
    /// Copies a single file to the target folder preserving the folder structure
    /// starting from the specified base folder.
    /// </summary>
    /// 
    /// <param name="baseDir">The base directory.</param>
    /// <param name="target">The target directory.</param>
    /// <param name="fileName">The file name.</param>
    let copyFileWithSubfolder baseDir target fileName =
        let fileName = Path.GetFullPath fileName
        let baseDir = Path.GetFullPath baseDir
        let relative = (Path.toRelativeFrom baseDir fileName).TrimStart '.'
        doCopyFile (target + relative) fileName

    /// <summary>
    /// Copies the files to the target.
    /// </summary>
    /// 
    /// <param name="target">The target directory.</param>
    /// <param name="files">The original file names as a sequence.</param>
    let copy target files =
        Directory.ensure target
        files |> Seq.iter (copyFile target)

    /// <summary>
    /// Copies the given files to the target.
    /// </summary>
    /// 
    /// <param name="target">The target directory.</param>
    /// <param name="files">The original file names as a sequence.</param>
    let copyTo target files = copy target files

    /// <summary>
    /// Copies the files from a cache folder.
    /// If the files are not cached or the original files have a different write time the cache will be refreshed.
    /// </summary>
    /// 
    /// <param name="target">The target FileName.</param>
    /// <param name="cacheDir">The cache directory.</param>
    /// <param name="files">The original files.</param>
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
                   else
                       Trace.traceVerbose <| sprintf "Original file %s does not exist, using cached file %s." fileName cached
                       ()
               else if not cachedFi.Exists || cachedFi.LastWriteTime <> fi.LastWriteTime then
                   Trace.traceVerbose <| sprintf "Cached file %s doesn't exist or is not up to date. Copying file to cache." cached
                   ()
                   copyFile cacheDir fi.FullName
               else
                   Trace.traceVerbose <| sprintf "Cached file %s is up to date." cached
                   ()
               copyFile target cached
               target @@ fi.Name)
        |> Seq.toList

    /// <summary>
    /// Renames the file or directory to the target name.
    /// </summary>
    /// 
    /// <param name="target">The target file or directory name.</param>
    /// <param name="fileName">The original file or directory name.</param>
    let rename target fileName =
        let fsi = FileSystemInfo.ofPath fileName
        FileSystemInfo.moveTo fsi target

    /// <summary>
    /// Copies a list of files to the specified directory without any output.
    /// </summary>
    ///
    /// <param name="target">The target directory.</param>
    /// <param name="files">List of files to copy.</param>
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

    /// <summary>
    /// Copies the files to the target - Alias for Copy
    /// </summary>
    /// 
    /// <param name="target">The target directory.</param>
    /// <param name="files">The original file names.</param>
    let copyFiles target files = copy target files

    /// <summary>
    /// Copies the given glob-matches into another directory by leaving relative paths in place based on the
    /// globbing base-directory
    /// </summary>
    ///
    /// <param name="targetDir">The target directory.</param>
    /// <param name="files">The file names.</param>
    /// 
    /// <example>
    /// <code lang="fsharp">
    /// !! "**/My*Glob*.exe"
    ///      |> GlobbingPattern.setBaseDir "baseDir"
    ///      |> Shell.copyFilesWithSubFolder "targetDir"
    /// </code>
    /// </example>
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


    /// <summary>
    /// Copies a directory recursively. If the target directory does not exist, it will be created
    /// </summary>
    /// 
    /// <param name="target">The target directory</param>
    /// <param name="source">The source directory</param>
    /// <param name="filterFile">A file filter predicate</param>
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
               Trace.traceVerbose <| sprintf "%s => %s" file newFile
               Path.getDirectory newFile |> Directory.ensure
               File.Copy(file, newFile, true))

    /// <summary>
    /// Cleans a directory by removing all files and sub-directories
    /// </summary>
    ///
    /// <param name="path">The directory path</param>
    let cleanDir path =
        let di = DirectoryInfo.ofPath path
        if di.Exists then
            Trace.traceVerbose <| sprintf "Deleting contents of %s" path
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

    /// <summary>
    /// Cleans multiple directories
    /// </summary>
    ///
    /// <param name="dirs">The directories to clean</param>
    let cleanDirs dirs = Seq.iter cleanDir dirs

    /// <summary>
    /// Delete a directory
    /// </summary>
    ///
    /// <param name="dir">The directory path to delete</param>
    let deleteDir dir = Directory.delete dir

    /// <summary>
    /// Deletes multiple directories
    /// </summary>
    ///
    /// <param name="dirs">The directories to delete</param>
    let deleteDirs dirs = Seq.iter Directory.delete dirs

    /// <summary>
    /// Appends all given files to one file.
    /// </summary>
    /// 
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="newFileName">The target FileName.</param>
    /// <param name="files">The original FileNames as a sequence.</param>
    let appendTextFilesWithEncoding encoding newFileName files =
        let fi = FileInfo.ofPath newFileName
        if fi.Exists then failwithf "File %s already exists." fi.FullName
        use file = fi.Open(FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        files |> Seq.iter (File.read >> Seq.iter writer.WriteLine)
        Trace.traceVerbose <| sprintf"Appending %s to %s" file.Name fi.FullName

    /// <summary>
    /// Appends all given files to one file.
    /// </summary>
    /// 
    /// <param name="newFileName">The target FileName.</param>
    /// <param name="files">The original FileNames as a sequence.</param>
    let appendTextFiles newFileName files = appendTextFilesWithEncoding System.Text.Encoding.UTF8 newFileName files

    /// <summary>
    /// Compares the given files for changes.
    /// If delete is set to true then equal files will be removed.
    /// </summary>
    ///
    /// <param name="delete">Mark if to delete same files or not</param>
    /// <param name="originalFileName">Original directory to use in comparision</param>
    /// <param name="compareFileName">Other directory to use in comparision</param>
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
                Trace.traceVerbose <| sprintf "Deleting File: %s" comp.FullName
            else Trace.traceVerbose <| sprintf "Files equal: %s" comp.FullName
            true

    /// <summary>
    /// Checks the srcFiles for changes to the last release.
    /// </summary>
    /// 
    /// <param name="lastReleaseDir">The directory of the last release</param>
    /// <param name="patchDir">The target directory</param>
    /// <param name="srcFiles">The source files</param>
    /// <param name="findOldFileF">A function which finds the old file</param>
    let generatePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles findOldFileF =
        let i = ref 0
        for file in srcFiles do
            let newFile = Path.toRelativeFromCurrent file
            let oldFile = findOldFileF newFile (lastReleaseDir + newFile.TrimStart('.'))
            let fi = FileInfo.ofPath oldFile
            if not fi.Exists then Trace.traceVerbose <| sprintf "LastRelease has no file like %s" fi.FullName
            if compareFiles false oldFile newFile |> not then
                i.Value <- i.Value + 1
                copyFileIntoSubFolder patchDir newFile
        Trace.traceVerbose <| sprintf "Patch contains %d files." i.Value

    /// <summary>
    /// Checks the srcFiles for changes to the last release.
    /// </summary>
    /// 
    /// <param name="lastReleaseDir">The directory of the last release.</param>
    /// <param name="patchDir">The target directory.</param>
    /// <param name="srcFiles">The source files.</param>
    let generatePatch lastReleaseDir patchDir srcFiles =
        generatePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles (fun _ b -> b)

    /// <summary>
    /// Checks if the directory exists
    /// </summary>
    ///
    /// <param name="path">Directory path to check</param>
    let testDir path =
        let di = DirectoryInfo.ofPath path
        if di.Exists then true
        else
            Trace.logfn "%s not found" di.FullName
            false

    /// <summary>
    /// Checks if the file exists
    /// </summary>
    ///
    /// <param name="path">Directory path to check</param>
    let testFile path =
        let fi = FileInfo.ofPath path
        if fi.Exists then true
        else
            Trace.logfn "%s not found" fi.FullName
            false


    /// <summary>
    /// Copies the file structure recursively.
    /// </summary>
    ///
    /// <param name="dir">Directory path to copy</param>
    /// <param name="outputDir">The target directory to copy to</param>
    /// <param name="overWrite">Flag to overwrite any matching files/directories or not</param>
    let copyRecursive dir outputDir overWrite =
        DirectoryInfo.copyRecursiveTo overWrite (DirectoryInfo.ofPath outputDir) (DirectoryInfo.ofPath dir)
    
    /// <summary>
    /// Copies the file structure recursively.
    /// </summary>
    /// 
    /// <param name="overWrite">Flag to overwrite any matching files/directories or not</param>
    /// <param name="outputDir">The target directory to copy to</param>
    /// <param name="dir">Directory path to copy</param>
    let inline copyRecursiveTo overWrite outputDir dir  = copyRecursive dir outputDir overWrite

    /// Copying methods
    [<NoComparison; NoEquality>]
    type CopyRecursiveMethod =
    | Overwrite
    | NoOverwrite
    | Skip
    | IncludePattern of string
    | ExcludePattern of string
    | Filter of (DirectoryInfo -> FileInfo -> bool)

    open Fake.IO.Globbing
    
    /// <summary>
    /// Copies the file structure recursively.
    /// </summary>
    /// 
    /// <param name="method">The method to decide which files get copied</param>
    /// <param name="dir">The source directory.</param>
    /// <param name="outputDir">The target directory.</param>
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

    ///<summary>
    /// Moves a single file to the target and overwrites the existing file.
    /// If <c>fileName</c> is a directory the functions does nothing.
    /// </summary>
    /// 
    /// <param name="target">The target directory.</param>
    /// <param name="fileName">The FileName.</param>
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

    /// <summary>
    /// Creates a config file with the parameters as "key;value" lines
    /// </summary>
    /// 
    /// <param name="configFileName">The configuration file name</param>
    /// <param name="parameters">The parameters to write to config file</param>
    let writeConfigFile configFileName parameters =
        if String.isNullOrEmpty configFileName then ()
        else
            let fi = FileInfo.ofPath configFileName
            if fi.Exists then fi.Delete()
            use streamWriter = fi.CreateText()
            for key, value in parameters do
                streamWriter.WriteLine("{0};{1}", key, value)

    /// <summary>
    /// Replaces all occurrences of the patterns in the given files with the given replacements.
    /// </summary>
    /// 
    /// <param name="replacements">A sequence of tuples with the patterns and the replacements.</param>
    /// <param name="files">The files to process.</param>
    let replaceInFiles replacements files = Templates.replaceInFiles replacements files

    /// <summary>
    /// Replace all occurrences of the regex pattern with the given replacement in the specified file
    /// </summary>
    /// 
    /// <param name="pattern">The string to search for a match</param>
    /// <param name="replacement">The replacement string</param>
    /// <param name="encoding">The encoding to use when reading and writing the file</param>
    /// <param name="file">The path of the file to process</param>
    let regexReplaceInFileWithEncoding pattern (replacement:string) encoding file =
        let oldContent = File.ReadAllText(file, encoding)
        let newContent = System.Text.RegularExpressions.Regex.Replace(oldContent, pattern, replacement)
        File.WriteAllText(file, newContent, encoding)

    /// <summary>
    /// Replace all occurrences of the regex pattern with the given replacement in the specified files
    /// </summary>
    /// 
    /// <param name="pattern">The string to search for a match</param>
    /// <param name="replacement">The replacement string</param>
    /// <param name="encoding">The encoding to use when reading and writing the files</param>
    /// <param name="files">The paths of the files to process</param>
    let regexReplaceInFilesWithEncoding pattern (replacement:string) encoding files =
        files |> Seq.iter (regexReplaceInFileWithEncoding pattern replacement encoding)

    /// <summary>
    /// Deletes a file if it exists
    /// </summary>
    /// 
    /// <param name="fileName">The file name to delete</param>
    let rm fileName = File.delete fileName

    /// <summary>
    /// Like "rm -rf" in a shell. Removes files recursively, ignoring non-existing files
    /// </summary>
    ///
    /// <param name="f">The file name to delete</param>
    let rm_rf f =
        if Directory.Exists f then Directory.delete f
        else File.delete f

    /// <summary>
    /// Creates a directory if it doesn't exist.
    /// </summary>
    ///
    /// <param name="path">The path to create directory in</param>
    let mkdir path = Directory.create path

    /// <summary>
    /// Like "cp -r" in a shell. Copies a file or directory recursively.
    /// </summary>
    /// 
    /// <param name="src">The source</param>
    /// <param name="dest">The destination</param>
    let cp_r src dest =
        if Directory.Exists src then copyDir dest src (fun _ -> true)
        else copyFile dest src

    /// <summary>
    /// Like "cp" in a shell. Copies a single file.
    /// </summary>
    /// 
    /// <param name="src">The source</param>
    /// <param name="dest">The destination</param>
    let cp src dest = copyFile dest src

    /// <summary>
    /// Changes working directory
    /// </summary>
    ///
    /// <param name="path">The path to directory to change to</param>
    let chdir path = Directory.SetCurrentDirectory path

    /// <summary>
    /// Changes working directory
    /// </summary>
    ///
    /// <param name="path">The path to directory to change to</param>
    let cd path = chdir path

    /// <summary>
    /// Gets working directory
    /// </summary>
    let pwd = Directory.GetCurrentDirectory

    /// The stack of directories operated on by pushd and popd
    let private dirStack = System.Collections.Generic.Stack<string>()

    /// <summary>
    /// Store the current directory in the directory stack before changing to a new one
    /// </summary>
    ///
    /// <param name="path">The path to directory to push</param>
    let pushd path =
        dirStack.Push(pwd())
        cd path

    /// <summary>
    /// Restore the previous directory stored in the stack
    /// </summary>
    let popd () =
        cd <| dirStack.Pop()

    /// <summary>
    /// Like "mv" in a shell. Moves/renames a file
    /// </summary>
    /// 
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
        | FileSystemInfo.File _, FileSystemInfo.Directory _ ->
            moveFile dest src
        | FileSystemInfo.Directory _, FileSystemInfo.Directory _ ->
            moveDir dest src
