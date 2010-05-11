[<AutoOpen>]
module Fake.FileHelper

open System.IO
open System.Text

/// Checks if all given files exists
let allFilesExist files = Seq.forall File.Exists files

/// Sets the directory readonly 
let setDirectoryReadOnly readOnly (dir:DirectoryInfo) = 
    if dir.Exists then
        let isReadOnly = dir.Attributes &&& FileAttributes.ReadOnly = FileAttributes.ReadOnly
        if readOnly && (not isReadOnly) then 
            dir.Attributes <- dir.Attributes ||| FileAttributes.ReadOnly
        if (not readOnly) && not isReadOnly then               
            dir.Attributes <- dir.Attributes &&& (~~~FileAttributes.ReadOnly)  

/// Sets all files in the directory readonly 
let rec SetDirReadOnly readOnly (dir:DirectoryInfo) =
    dir.GetDirectories() |> Seq.iter (fun dir ->
        SetDirReadOnly readOnly dir
        setDirectoryReadOnly readOnly dir)
    dir.GetFiles() |> Seq.iter (fun file -> file.IsReadOnly <- readOnly)    
  
/// Sets all files in the directory readonly 
let SetReadOnly readOnly (files: string seq) =
    files |> Seq.iter (fun file ->
      let fi = new FileInfo(file)
      if fi.Exists then 
          fi.IsReadOnly <- readOnly
      else
          let di = new DirectoryInfo(file)
          setDirectoryReadOnly readOnly di)
      
      
/// Deletes a directory if it exists
let DeleteDir x =   
    let dir = new DirectoryInfo(x)    
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
let CreateDir x =   
    let dir = new DirectoryInfo(x)
    if not dir.Exists then 
       logfn "Creating %s" dir.FullName
       dir.Create()
    else
       logfn "%s does already exist." dir.FullName
    
/// Creates a file if it does not exist
let CreateFile x =   
    let file = new FileInfo(x)
    if not file.Exists then 
        logfn "Creating %s" file.FullName
        let newFile = file.Create()
        newFile.Close()
    else
        logfn "%s does already exist." file.FullName
    
/// Deletes a file if it exist
let DeleteFile x =   
    let file = new FileInfo(x)    
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
///   param file: The fileName
let CopyFileIntoSubFolder target file =
    let relative = (toRelativePath file).TrimStart('.')
    let fi = new FileInfo(file)
  
    let targetName = target + relative
    let target = new FileInfo(targetName)
    if not target.Directory.Exists then
        target.Directory.Create()
    logVerbosefn "Copy %s to %s" file targetName
    fi.CopyTo(targetName,true) |> ignore    

/// Copies a single file to the target
///   param target: The target directory.
///   param file: The FileName
let CopyFile target file =
    let fi = new FileInfo(file)
    let targetName = target @@ fi.Name
    logVerbosefn "Copy %s to %s" file targetName
    fi.CopyTo(targetName,true) |> ignore    
  
/// Copies the files to the target
///   param target: The target directory.
///   param files: The original FileNames as a sequence.
let Copy target = Seq.iter (CopyFile target)   

/// Renames the files to the target fileName
///   param target: The target FileName.
///   param file: The orginal FileName.
let Rename target file = (new FileInfo(file)).MoveTo target   
  
let SilentCopy target files =
    files |> Seq.iter (fun file ->
        let fi = new FileInfo(file)
        let targetName = target @@ fi.Name
        let targetFI = new FileInfo(targetName)
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
            let newFile = target + file.Remove(0, source.Length)
            logVerbosefn "%s => %s" file newFile
            Directory.CreateDirectory(Path.GetDirectoryName(newFile)) |> ignore
            File.Copy(file, newFile, true))
  
/// Cleans a directory
let CleanDir dir =
    let di = new DirectoryInfo(dir)
    if di.Exists then
        logfn "Deleting content of %s" dir
        // delete all files
        Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
          |> Seq.iter (fun file -> 
                let fi = new FileInfo(file)
                fi.IsReadOnly <- false
                fi.Delete())
    
        // deletes all subdirectories
        let rec deleteDirs actDir =
            Directory.GetDirectories(actDir) |> Seq.iter deleteDirs
            Directory.Delete(actDir,true)
    
        Directory.GetDirectories dir |> Seq.iter deleteDirs      
    else
        CreateDir dir
    
    // set writeable
    File.SetAttributes(dir,FileAttributes.Normal)        

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
    let fi = new FileInfo(newFileName)
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

/// Converts a file to it's full file system name
let FullName fileName = Path.GetFullPath fileName
  
/// Compares the given files for changes
/// If delete = true then equal files will be removed  
let CompareFiles delete originalFileName compareFileName =  
    let ori = new FileInfo(originalFileName)
    let comp = new FileInfo(compareFileName)

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
let TestDir dir =
    let di = new DirectoryInfo(dir)
  
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
      |> Seq.map (fun n -> 
            let newFile = toRelativePath n 
            let oldFile = findOldFileF newFile (lastReleaseDir + newFile.TrimStart('.'))
            let fi = new FileInfo(oldFile)
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
      dir.GetDirectories() 
        |> Seq.fold 
             (fun acc (d:DirectoryInfo) ->
               let newDir = new DirectoryInfo(outputDir.FullName @@ d.Name)
               if not newDir.Exists then
                 newDir.Create()
               copyRecursive d newDir overwrite @ acc)
           []
  
    (dir.GetFiles()
      |> Seq.map
          (fun f ->
             let newFileName = outputDir.FullName @@ f.Name
             f.CopyTo(newFileName, overwrite) |> ignore
             newFileName)
      |> Seq.toList) @ files
  
/// Copies the file structure recursive
let CopyRecursive dir outputDir = copyRecursive (new DirectoryInfo(dir)) (new DirectoryInfo(outputDir))