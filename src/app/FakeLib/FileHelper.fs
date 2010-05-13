[<AutoOpen>]
module Fake.FileHelper

open System.IO
open System.Text

/// Checks if all given files exists
let allFilesExist files = Seq.forall File.Exists files

/// Performs the given actions on all files and subdirectories
let rec recursively dirF fileF (dir:DirectoryInfo) =
    dir
      |> subDirectories
      |> Seq.iter (fun dir ->
        recursively dirF fileF dir
        dirF dir)

    dir
      |> filesInDir
      |> Seq.iter fileF

/// Sets the directory readonly 
let setDirectoryReadOnly readOnly (dir:DirectoryInfo) = 
    if dir.Exists then
        let isReadOnly = dir.Attributes &&& FileAttributes.ReadOnly = FileAttributes.ReadOnly
        if readOnly && (not isReadOnly) then 
            dir.Attributes <- dir.Attributes ||| FileAttributes.ReadOnly
        if (not readOnly) && not isReadOnly then               
            dir.Attributes <- dir.Attributes &&& (~~~FileAttributes.ReadOnly)  

/// Sets all files in the directory readonly 
let SetDirReadOnly readOnly dir =
    recursively (setDirectoryReadOnly readOnly) (fun file -> file.IsReadOnly <- readOnly) dir
  
/// Sets all files in the directory readonly 
let SetReadOnly readOnly (files: string seq) =
    files |> Seq.iter (fun file ->
      let fi = fileInfo file
      if fi.Exists then 
          fi.IsReadOnly <- readOnly
      else
          file
            |> directoryInfo
            |> setDirectoryReadOnly readOnly)
      
/// Deletes a directory if it exists
let DeleteDir path =   
    let dir = directoryInfo path
    if dir.Exists then 
        // set all files readonly = false
        !+ "/**/*.*"
          |> SetBaseDir dir.FullName
          |> Scan
          |> (SetReadOnly false)
      
        logfn "Deleting %s" dir.FullName
        dir.Delete true
    else
        logfn "%s does not exist." dir.FullName
    
/// Creates a directory if it does not exist
let CreateDir path =   
    let dir = directoryInfo path
    if not dir.Exists then 
       logfn "Creating %s" dir.FullName
       dir.Create()
    else
       logfn "%s does already exist." dir.FullName
    
/// Creates a file if it does not exist
let CreateFile fileName =   
    let file = fileInfo fileName
    if not file.Exists then 
        logfn "Creating %s" file.FullName
        let newFile = file.Create()
        newFile.Close()
    else
        logfn "%s does already exist." file.FullName
    
/// Deletes a file if it exist
let DeleteFile fileName =   
    let file = fileInfo fileName
    if file.Exists then 
        logfn "Deleting %s" file.FullName
        file.Delete()
    else
        logfn "%s does not exist." file.FullName                
    
let (|File|Directory|) (fileSysInfo : FileSystemInfo) =
    match fileSysInfo with
    | :? FileInfo as file -> File (file.Name)
    | :? DirectoryInfo as dir -> Directory (dir.Name, seq { for x in dir.GetFileSystemInfos() -> x })
    | _ -> failwith "No file or directory given."      
      
/// Active Pattern for determining file extension
let (|EndsWith|_|) extension (file : string) = if file.EndsWith extension then Some() else None
 
/// Active Pattern for determining file name   
let (|FileInfoFullName|) (f:FileInfo) = f.FullName

/// Active Pattern for determining FileInfoNameSections
let (|FileInfoNameSections|) (f:FileInfo) = (f.Name,f.Extension,f.FullName)

/// Copies a single file to a relative subfolder of the target
///   param target: The targetDirectory
///   param fileName: The fileName
let CopyFileIntoSubFolder target fileName =
    let relative = (toRelativePath fileName).TrimStart '.'
    let fi = fileInfo fileName
  
    let targetName = target + relative
    let target = fileInfo targetName
    if not target.Directory.Exists then target.Directory.Create()

    logVerbosefn "Copy %s to %s" fileName targetName
    fi.CopyTo(targetName,true) |> ignore    

/// Copies a single file to the target
///   param target: The target directory.
///   param fileName: The FileName
let CopyFile target fileName =
    let fi = fileInfo fileName
    let targetName = target @@ fi.Name
    logVerbosefn "Copy %s to %s" fileName targetName
    fi.CopyTo(targetName,true) |> ignore    
  
/// Copies the files to the target
///   param target: The target directory.
///   param files: The original FileNames as a sequence.
let Copy target = Seq.iter (CopyFile target)   

/// Renames the files to the target fileName
///   param target: The target FileName.
///   param file: The orginal FileName.
let Rename target fileName = (fileInfo fileName).MoveTo target
  
let SilentCopy target files =
    files |> Seq.iter (fun file ->
        let fi = fileInfo file
        let targetName = target @@ fi.Name
        let targetFI = fileInfo targetName
        if targetFI.Exists then
            if fi.LastWriteTime > targetFI.LastWriteTime then
              targetFI.Attributes <- FileAttributes.Normal
              fi.CopyTo(targetName,true) |> ignore
        else
            fi.CopyTo(targetName) |> ignore)
               

/// Copies the files to the target - Alias for Copy
///   param target: The target directory.
///   param files: The original FileNames as a sequence.
let CopyFiles target files = Copy target files  

/// Exclude SVN files (path with .svn)
let excludeSVNFiles (path:string) = not <| path.Contains ".svn"

/// Includes all files
let allFiles (path:string) = true

/// Copies a directory recursivly
/// If the target directory does not exist, it will be created
///   param target: The target directory.
///   param files: The source directory.
///   param filterFile: A file filter.
let CopyDir target source filterFile =
    CreateDir target
    Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)
      |> Seq.filter filterFile
      |> Seq.iter (fun file -> 
            let fi = file |> replace source "" |> trimSeparator
            let newFile = target @@ fi
            logVerbosefn "%s => %s" file newFile
            Path.GetDirectoryName newFile
              |> Directory.CreateDirectory
              |> ignore
            File.Copy(file, newFile, true))
  
/// Cleans a directory
let CleanDir path =
    let di = directoryInfo path
    if di.Exists then
        logfn "Deleting contents of %s" path
        // delete all files
        Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
          |> Seq.iter (fun file -> 
                let fi = fileInfo file
                fi.IsReadOnly <- false
                fi.Delete())
    
        // deletes all subdirectories
        let rec deleteDirs actDir =
            Directory.GetDirectories(actDir) |> Seq.iter deleteDirs
            Directory.Delete(actDir,true)
    
        Directory.GetDirectories path 
          |> Seq.iter deleteDirs      
    else
        CreateDir path
    
    // set writeable
    File.SetAttributes(path,FileAttributes.Normal)        

/// Clean multiple directories
let CleanDirs dirs = Seq.iter CleanDir dirs

/// Reads a csv file line by line
/// delimiter is a ,
let ReadCSVFile(file:string) =             
    let csvRegEx = new RegularExpressions.Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))")   
         
    ReadFile file
      |> Seq.map csvRegEx.Split 
      |> Seq.map (Array.map (fun s -> s.Trim([| '"' |])))
             
/// Appends all given files to one file 
///   param newFileName: The target FileName.
///   param files: The original FileNames as a sequence.
let AppendTextFiles newFileName files =    
    let fi = fileInfo newFileName
    if fi.Exists then failwithf "File %s already exists." (fi.FullName)
    use writer = new StreamWriter(fi.FullName, false, Encoding.Default);
  
    files 
      |> Seq.iter (fun file ->       
            logVerbosefn "Appending %s to %s" file fi.FullName
            ReadFile file |> Seq.iter writer.WriteLine)

/// Checks if the two files are byte-to-byte equal
let FilesAreEqual (first:FileInfo) (second:FileInfo) =   
    if first.Length <> second.Length then false else
    let BYTES_TO_READ = 32768
    let iterations = System.Math.Ceiling((float first.Length) / (float BYTES_TO_READ)) |> int

    use fs1 = first.OpenRead()
    use fs2 = second.OpenRead()
   
    let one = Array.create BYTES_TO_READ (byte 0)
    let two = Array.create BYTES_TO_READ (byte 0)

    let mutable eq = true
    for i in 0..iterations do        
        if eq then
          fs1.Read(one, 0, BYTES_TO_READ) |> ignore
          fs2.Read(two, 0, BYTES_TO_READ) |> ignore

          if one <> two then eq <- false

    eq
  
/// Compares the given files for changes
/// If delete = true then equal files will be removed  
let CompareFiles delete originalFileName compareFileName =  
    let ori = fileInfo originalFileName
    let comp = fileInfo compareFileName

    let identical = 
        if not (ori.Exists && comp.Exists && ori.Length = comp.Length) then false else
        if ori.LastWriteTime = comp.LastWriteTime then true else FilesAreEqual ori comp

    if not identical then false else
    if delete then      
        comp.Attributes <- FileAttributes.Normal
        comp.Delete()
        logVerbosefn "Deleting File: %s" comp.FullName
    else
        logVerbosefn "Files equal: %s" comp.FullName
    true

  
/// Checks if the directory exists
let TestDir path =
    let di = directoryInfo path
  
    if di.Exists then true else
    logfn "%s not found" di.FullName
    false

/// Checks the srcFiles for changes to the last release
///  param lastReleaseDir: The directory of the last release
///  param patchDir: The target directory
///  param srcFiles: The source files
///  param findOldFileF: A function which finds the old file
///            (string -> string -> string)
///            (newFile -> oldFileProposal)
let GeneratePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles findOldFileF =
    srcFiles
      |> Seq.map (fun file -> 
            let newFile = toRelativePath file
            let oldFile = findOldFileF newFile (lastReleaseDir + newFile.TrimStart('.'))
            let fi = fileInfo oldFile
            if not fi.Exists then logVerbosefn "LastRelease has no file like %s" fi.FullName
            newFile,oldFile)         
      |> Seq.filter (fun (newF,oldF) -> CompareFiles false oldF newF |> not)
      |> Seq.iter (fun (newF,oldF) -> CopyFileIntoSubFolder patchDir newF)

/// Checks the srcFiles for changes to the last release
///  param lastReleaseDir: The directory of the last release
///  param patchDir: The target directory
///  param srcFiles: The source files
let GeneratePatch lastReleaseDir patchDir srcFiles =
    GeneratePatchWithFindOldFileFunction lastReleaseDir patchDir srcFiles (fun a b -> b)

/// Copies the file structure recursive
let rec copyRecursive (dir:DirectoryInfo) (outputDir:DirectoryInfo) overwrite =
    let files =    
      dir
        |> subDirectories 
        |> Seq.fold 
             (fun acc (d:DirectoryInfo) ->
               let newDir = outputDir.FullName @@ d.Name |> directoryInfo
               if not newDir.Exists then newDir.Create()

               copyRecursive d newDir overwrite @ acc)
           []
  
    (dir
      |> filesInDir
      |> Seq.map
          (fun f ->
             let newFileName = outputDir.FullName @@ f.Name
             f.CopyTo(newFileName, overwrite) |> ignore
             newFileName)
      |> Seq.toList) @ files
  
/// Copies the file structure recursive
let CopyRecursive dir outputDir = copyRecursive (directoryInfo dir) (directoryInfo outputDir)