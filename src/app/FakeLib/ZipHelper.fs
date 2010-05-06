[<AutoOpen>]
module Fake.ZipHelper

open System.IO
open ICSharpCode.SharpZipLib.Zip
open System.Globalization
open System

/// The default zip level
let DefaultZipLevel = 7

/// Creates a zip file with the given files
let CreateZip workingDir fileName comment level flatten files =
    let workingDir =
        let dir = new DirectoryInfo(workingDir)
        if not dir.Exists then failwithf "Directory not found: %s" dir.FullName
        dir.FullName
  
    use stream = new ZipOutputStream(File.Create(fileName))
    let zipLevel = min (max 0 level) 9

    tracefn "Creating Zipfile: %s (Level: %d)" fileName zipLevel
    stream.SetLevel zipLevel
    if not (String.IsNullOrEmpty comment) then stream.SetComment comment
  
    let buffer = Array.create 32768 0uy

    for item in files do      
        let info = new FileInfo(item)      
        if info.Exists then
          let itemSpec =
              if flatten then info.Name else
              if not (String.IsNullOrEmpty(workingDir)) && 
                  info.FullName.StartsWith(workingDir, true, CultureInfo.InvariantCulture) 
              then
                  info.FullName.Remove(0, workingDir.Length)
              else
                  info.FullName
        
          let itemSpec = ZipEntry.CleanName itemSpec
          let entry = new ZipEntry(itemSpec)
          entry.DateTime <- info.LastWriteTime
          entry.Size <- info.Length
    
          use stream2 = info.OpenRead()

          stream.PutNextEntry(entry)
          let length = ref stream2.Length
          stream2.Seek(0L, SeekOrigin.Begin) |> ignore
          while !length > 0L do
              let count = stream2.Read(buffer, 0, buffer.Length)
              stream.Write(buffer, 0, count)
              length := !length - (int64 count)
    
          logfn "File added %s" itemSpec
    
    stream.Finish()
    tracefn "Zip successfully created %s" fileName
 
/// Creates a zip file with the given files 
///
/// Parameter 1: workingDir - The relative dir of the zip files. Use this parameter to influence directory structure within zip file.
/// Parameter 2: fileName - The fileName of the resulting zip file.
/// Parameter 3: files - A sequence with files to zip.
let Zip workingDir fileName = CreateZip workingDir fileName "" DefaultZipLevel false
  
/// Creates a zip file with the given file 
///
/// Parameter 1: fileName - The fileName of the resulting zip file.
/// Parameter 2: fileName - The file to zip.
let ZipFile fileName file =
    let fi = new FileInfo(file)    
    CreateZip (fi.Directory.FullName) fileName "" DefaultZipLevel false [fi.FullName]

/// Unzips a file with the given fileName
///
/// Parameter 1: target - The target directory.
/// Parameter 2: fileName - The fileName of the zip file.
let Unzip target fileName =  
    let zip = new FastZip()
    zip.ExtractZip (fileName, target, "")