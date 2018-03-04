[<AutoOpen>]
/// This module contains helper function to create and extract zip archives.
module Fake.ZipHelper

open System.IO
open ICSharpCode.SharpZipLib.Zip
open ICSharpCode.SharpZipLib.Core
open System

/// The default zip level
let DefaultZipLevel = 7

let private addZipEntry (stream : ZipOutputStream) (buffer : byte[]) (item : string) (itemSpec : string) =
    let info = fileInfo item
    let itemSpec = ZipEntry.CleanName itemSpec
    logfn "Adding File %s" itemSpec
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

let private createZip fileName comment level (items : (string * string) seq) =
    use stream = new ZipOutputStream(File.Create(fileName))
    let zipLevel = min (max 0 level) 9
    tracefn "Creating Zipfile: %s (Level: %d)" fileName zipLevel
    stream.SetLevel zipLevel
    if not (String.IsNullOrEmpty comment) then stream.SetComment comment
    let buffer = Array.create 32768 0uy
    for item, itemSpec in items do
        addZipEntry stream buffer item itemSpec
    stream.Finish()
    tracefn "Zip successfully created %s" fileName

/// Creates a zip file with the given files
/// ## Parameters
///  - `workingDir` - The relative dir of the zip files. Use this parameter to influence directory structure within zip file.
///  - `fileName` - The fileName of the resulting zip file.
///  - `comment` - A comment for the resulting zip file.
///  - `level` - The compression level.
///  - `flatten` - If set to true then all subfolders are merged into the root folder.
///  - `files` - A sequence with files to zip.
let CreateZip workingDir fileName comment level flatten files =
    let workingDir = 
        let dir = directoryInfo workingDir
        if not dir.Exists then failwithf "Directory not found: %s" dir.FullName
        dir.FullName

    let items = seq {
        for item in files do
            let info = fileInfo item
            if info.Exists then 
                let itemSpec = 
                    if flatten then info.Name
                    else if not (String.IsNullOrEmpty(workingDir)) 
                            && info.FullName.StartsWith(workingDir, true, Globalization.CultureInfo.InvariantCulture) then 
                        info.FullName.Remove(0, workingDir.Length)
                    else info.FullName
                yield item, itemSpec }

    createZip fileName comment level items

/// Creates a zip file with the given files.
/// ## Parameters
///  - `workingDir` - The relative dir of the zip files. Use this parameter to influence directory structure within zip file.
///  - `fileName` - The file name of the resulting zip file.
///  - `files` - A sequence with files to zip.
let Zip workingDir fileName files = CreateZip workingDir fileName "" DefaultZipLevel false files

/// Creates a zip file with the given file.
/// ## Parameters
///  - `fileName` - The file name of the resulting zip file.
///  - `targetFileName` - The file to zip.
let ZipFile fileName targetFileName = 
    let fi = fileInfo targetFileName
    CreateZip (fi.Directory.FullName) fileName "" DefaultZipLevel false [ fi.FullName ]

/// Unzips a file with the given file name.
/// ## Parameters
///  - `target` - The target directory.
///  - `fileName` - The file name of the zip file.
let Unzip target (fileName : string) = 
    use zipFile = new ZipFile(fileName)
    for entry in zipFile do
        match entry with
        | :? ZipEntry as zipEntry -> 
            let unzipPath = Path.Combine(target, zipEntry.Name)
            let directoryPath = Path.GetDirectoryName(unzipPath)
            // create directory if needed
            if directoryPath.Length > 0 then Directory.CreateDirectory(directoryPath) |> ignore
            // unzip the file
            let zipStream = zipFile.GetInputStream(zipEntry)
            let buffer = Array.create 32768 0uy
            if unzipPath.EndsWith "/" |> not then 
                use unzippedFileStream = File.Create(unzipPath)
                StreamUtils.Copy(zipStream, unzippedFileStream, buffer)
        | _ -> ()

/// Unzips a single file from the archive with the given file name.
/// ## Parameters
///  - `fileToUnzip` - The file inside the archive.
///  - `zipFileName` - The file name of the zip file.
let UnzipSingleFileInMemory fileToUnzip (zipFileName : string) = 
    use zf = new ZipFile(zipFileName)
    let ze = zf.GetEntry fileToUnzip
    if ze = null then raise <| ArgumentException(fileToUnzip, "not found in zip")
    use stream = zf.GetInputStream(ze)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

/// Unzips a single file from the archive with the given file name.
/// ## Parameters
///  - `predicate` - The predictae for the searched file in the archive.
///  - `zipFileName` - The file name of the zip file.
let UnzipFirstMatchingFileInMemory predicate (zipFileName : string) = 
    use zf = new ZipFile(zipFileName)
    
    let ze = 
        seq { 
            for ze in zf do
                yield ze :?> ZipEntry
        }
        |> Seq.find predicate
    
    use stream = zf.GetInputStream(ze)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

/// Creates a zip file with the given files.
/// ## Parameters
///  - `fileName` - The file name of the resulting zip file.
///  - `comment` - A comment for the resulting zip file.
///  - `level` - The compression level.
///  - `files` - A sequence of target folders and files to include relative to their base directory.
let CreateZipOfIncludes fileName comment level (files : (string * FileIncludes) seq) =
    let items = seq {
        for path, incl in files do
            for file in incl do
                let info = fileInfo file
                if info.Exists then
                    let baseFull = (Path.GetFullPath incl.BaseDirectory).TrimEnd [|'/';'\\'|]
                    let path =
                        if String.IsNullOrEmpty path then ""
                        else sprintf "%s%c" (path.TrimEnd [|'/';'\\'|]) Path.DirectorySeparatorChar
                    let spec = sprintf "%s%s" path (info.FullName.Substring (baseFull.Length+1))
                    yield file, spec }

    createZip fileName comment level items

/// Creates a zip file with the given files.
/// ## Parameters
///  - `fileName` - The file name of the resulting zip file.
///  - `files` - A sequence of target folders and files to include relative to their base directory.
///
/// ## Sample
///
/// The following sample creates a zip file containing the files from the two target folders and FileIncludes.
///
/// - The files from the first FileInclude will be placed in the root of the zip file.
/// - The files from the second FileInclude will be placed under the directory `app_data\jobs\continuous\MyWebJob` in the zip file.
///
///
///     Target "Zip" (fun _ ->
///         [   "", !! "MyWebApp/*.html"
///                 ++ "MyWebApp/bin/**/*.dll"
///                 ++ "MyWebApp/bin/**/*.pdb"
///                 ++ "MyWebApp/fonts/**"
///                 ++ "MyWebApp/img/**"
///                 ++ "MyWebApp/js/**"
///                 -- "MyWebApp/js/_references.js"
///                 ++ "MyWebApp/web.config"
///             @"app_data\jobs\continuous\MyWebJob", !! "MyWebJob/bin/Release/*.*"
///         ]
///         |> ZipOfIncludes (sprintf @"bin\MyWebApp.%s.zip" buildVersion)
///     )
///
let ZipOfIncludes fileName files = CreateZipOfIncludes fileName "" DefaultZipLevel files
