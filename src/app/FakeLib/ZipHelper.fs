[<AutoOpen>]
module Fake.ZipHelper

open System.IO
open ICSharpCode.SharpZipLib.Zip
open System

/// The default zip level
let DefaultZipLevel = 7

/// Creates a zip file with the given files
let CreateZip workingDir fileName comment level flatten files =
    let workingDir =
        let dir = directoryInfo workingDir
        if not dir.Exists then failwithf "Directory not found: %s" dir.FullName
        dir.FullName
  
    use stream = new ZipOutputStream(File.Create(fileName))
    let zipLevel = min (max 0 level) 9

    tracefn "Creating Zipfile: %s (Level: %d)" fileName zipLevel
    stream.SetLevel zipLevel
    if not (String.IsNullOrEmpty comment) then stream.SetComment comment
  
    let buffer = Array.create 32768 0uy

    for item in files do      
        let info = fileInfo item      
        if info.Exists then
          let itemSpec =
              if flatten then info.Name else
              if not (String.IsNullOrEmpty(workingDir)) && 
                  info.FullName.StartsWith(workingDir, true, Globalization.CultureInfo.InvariantCulture) 
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
 
/// <summary>Creates a zip file with the given files.</summary>
/// <param name="workingDir">The relative dir of the zip files. Use this parameter to influence directory structure within zip file.</param>
/// <param name="fileName">The fileName of the resulting zip file.</param>
/// <param name="files">A sequence with files to zip.</param>
let Zip workingDir fileName = CreateZip workingDir fileName "" DefaultZipLevel false
  
/// <summary>Creates a zip file with the given file.</summary>
/// <param name="fileName">The fileName of the resulting zip file.</param>
/// <param name="targetFileName">The file to zip.</param>
let ZipFile fileName targetFileName =
    let fi = fileInfo targetFileName    
    CreateZip (fi.Directory.FullName) fileName "" DefaultZipLevel false [fi.FullName]

/// <summary>Unzips a file with the given fileName.</summary>
/// <param name="target">The target directory.</param>
/// <param name="fileName">The fileName of the zip file.</param>
let Unzip target fileName =  
    let zip = new FastZip()
    zip.ExtractZip (fileName, target, "")

/// <summary>Unzips a single file from the archive with the given fileName.</summary>
/// <param name="fileToUnzip">The file inside the archive.</param>
/// <param name="zipFileName">The FileName of the zip file.</param>
let UnzipSingleFileInMemory fileToUnzip (zipFileName:string) =
    use zf = new ZipFile(zipFileName)
    let ze = zf.GetEntry fileToUnzip
    if ze = null then
        raise <| ArgumentException(fileToUnzip, "not found in zip")

    use stream = zf.GetInputStream(ze)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()
    
/// <summary>Unzips a single file from the archive with the given fileName.</summary>
/// <param name="predicate">The predictae for the searched file in the archive.</param>
/// <param name="zipFileName">The FileName of the zip file.</param>
let UnzipFirstMatchingFileInMemory predicate (zipFileName:string) =
    use zf = new ZipFile(zipFileName)

    let ze = 
        seq { for ze in zf do yield ze :?> ZipEntry }
        |> Seq.find predicate

    use stream = zf.GetInputStream(ze)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()         