[<AutoOpen>]
/// This module contains helper function to create and extract zip archives.
module Fake.ZipHelper

open Fake.ArchiveHelper

/// The default zip level
let DefaultZipLevel = 7

let inline private setParams level comment p : Zip.ZipCompressionParams =
    { p with
            Comment = match comment with | null -> None | _ -> Some comment
            Level = CompressionLevel.create level }

/// Creates a zip file with the given files
/// ## Parameters
///  - `workingDir` - The relative dir of the zip files. Use this parameter to influence directory structure within zip file.
///  - `fileName` - The fileName of the resulting zip file.
///  - `comment` - A comment for the resulting zip file.
///  - `level` - The compression level.
///  - `flatten` - If set to true then all subfolders are merged into the root folder.
///  - `files` - A sequence with files to zip.
let CreateZip workingDir fileName comment level flatten files =
    files
    |> Seq.map fileInfo
    |> Zip.Compress (setParams level comment) flatten (directoryInfo workingDir) (fileInfo fileName)

/// Unzips a file with the given file name.
/// ## Parameters
///  - `target` - The target directory.
///  - `fileName` - The file name of the zip file.
let Unzip target (fileName : string) =
    Zip.Extract (directoryInfo target) (fileInfo fileName)

/// Creates a zip file with the given file.
/// ## Parameters
///  - `fileName` - The file name of the resulting zip file.
///  - `targetFileName` - The file to zip.
let ZipFile fileName targetFileName =
    let fi = fileInfo targetFileName

    Seq.singleton fi
    |> Zip.CompressWithDefaults fi.Directory (fileInfo fileName)

let inline private toArchiveFileSpecs (archivePath, fileInclude) = seq {
    for file in fileInclude do
        let info = fileInfo file
        yield archiveFileSpecWithAltBaseDir archivePath (directoryInfo fileInclude.BaseDirectory) info }

/// Creates a zip file with the given files.
/// ## Parameters
///  - `fileName` - The file name of the resulting zip file.
///  - `comment` - A comment for the resulting zip file.
///  - `level` - The compression level.
///  - `files` - A sequence of target folders and files to include relative to their base directory.
let CreateZipOfIncludes fileName comment level files =
    files
    |> Seq.map toArchiveFileSpecs
    |> Seq.concat
    |> Zip.CompressSpecs (setParams comment level) (fileInfo fileName)

/// Creates a zip file with the given files.
/// ## Parameters
///  - `fileName` - The file name of the resulting zip file.
///  - `files` - A sequence of target folders and files to include relative to their base directory.
///
/// ## Sample
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
let ZipOfIncludes fileName files =
    files
    |> Seq.map toArchiveFileSpecs
    |> Seq.concat
    |> Zip.CompressSpecs id (fileInfo fileName)

/// Creates a zip file with the given files.
/// ## Parameters
///  - `workingDir` - The relative dir of the zip files. Use this parameter to influence directory structure within zip file.
///  - `fileName` - The file name of the resulting zip file.
///  - `files` - A sequence with files to zip.
let Zip workingDir fileName files =
    files
    |> Seq.map fileInfo
    |> Zip.CompressWithDefaults (directoryInfo workingDir) (fileInfo fileName)

// The following functions do not have analogs in `ArchiveHelper`

open System
open System.IO
open ICSharpCode.SharpZipLib.Zip

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