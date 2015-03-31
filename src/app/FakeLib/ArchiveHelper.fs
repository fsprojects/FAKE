/// Provides utility tasks for storing and compressing files in archives.
module Fake.ArchiveHelper

open System
open System.IO
open ICSharpCode.SharpZipLib.Core
open ICSharpCode.SharpZipLib.BZip2
open ICSharpCode.SharpZipLib.GZip
open ICSharpCode.SharpZipLib.Tar
open ICSharpCode.SharpZipLib.Zip

[<Literal>]
let private DefaultBufferSize = 32768

/// A description of a file to be added to an archive.
type ArchiveFileSpec = { InputFilePath : string; ArchiveFilePath : string }

let private copyFileBuffered bufferSize (outStream : #Stream) (inStream : #Stream) =
    let rec doCopy buffer (outStream : #Stream) (inStream : #Stream) length =
        if length > 0L then
            let count = inStream.Read(buffer, 0, buffer.Length)
            outStream.Write(buffer, 0, count)
            doCopy buffer outStream inStream (length - int64 count)

    let length = inStream.Length
    inStream.Seek(0L, SeekOrigin.Begin) |> ignore
    let buffer = Array.zeroCreate<byte> bufferSize
    doCopy buffer outStream inStream length

let private addEntry prepareEntry afterEntry (outStream : #Stream) fileSpec =
    let info = fileInfo fileSpec.InputFilePath
    use inStream = info.OpenRead()
    let entryName = prepareEntry info fileSpec.ArchiveFilePath
    logfn "Compressed %s => %s" fileSpec.InputFilePath entryName
    copyFileBuffered DefaultBufferSize outStream inStream
    afterEntry outStream

let private createArchiveStream (streamCreator : Stream -> #Stream) fileName =
    File.Create(fileName) :> Stream
    |> streamCreator

let private createArchive streamCreator addEntry archiveName items =
    use stream = streamCreator archiveName
    Seq.iter (addEntry stream) items
    tracefn "Archive successfully created %s" archiveName

/// Constructs a file specification which will archive the file at the root.
let archiveFileSpec filePath =
    { InputFilePath = Path.GetFullPath(filePath)
      ArchiveFilePath = Path.GetFileName(filePath) }

/// Constructs a file specification which will archive the file with a path relative to the `baseDir`.
let archiveFileSpecWithBaseDir baseDir filePath =
    let baseDir = 
        let dir = directoryInfo baseDir
        if not dir.Exists then failwithf "Directory not found: %s" dir.FullName
        dir.FullName

    let fullFilePath = Path.GetFullPath(filePath)
    { InputFilePath = fullFilePath
      ArchiveFilePath = if isNotNullOrEmpty baseDir && fullFilePath.StartsWith(baseDir, true, Globalization.CultureInfo.InvariantCulture) then
                            fullFilePath.[(baseDir.Length + 1)..]
                        else
                            failwithf "File to be included (%s) was not in base directory: %s" fullFilePath baseDir }

module CompressionLevel =
    type T = CompressionLevel of int

    let private clipLevel = max 0 >> min 9

    let Default = CompressionLevel 7

    let create level = CompressionLevel (clipLevel level)

    let value (CompressionLevel l) = l

module Zip =
    type ZipCompressionParams = { Comment : string option; Level : CompressionLevel.T }

    let ZipCompressionDefaults = { Comment = None; Level = CompressionLevel.Default }

    let private addEntry (outStream : ZipOutputStream) =
        let prepareEntry (fileInfo : FileInfo) itemSpec =
            let entry = new ZipEntry(ZipEntry.CleanName itemSpec)
            entry.DateTime <- fileInfo.LastWriteTime
            entry.Size <- fileInfo.Length
            outStream.PutNextEntry(entry)
            entry.Name
        let afterEntry (outStream : ZipOutputStream) =
            outStream.CloseEntry()
        addEntry prepareEntry afterEntry outStream

    let stream { Level = level; Comment = comment } inner =
        let zipStream = new ZipOutputStream(inner)
        zipStream.SetLevel <| CompressionLevel.value level
        match comment with | Some c -> zipStream.SetComment <| c | _ -> ()
        zipStream

    let createFile zipParams fileName =
        tracefn "Creating zip archive: %s (%A)" fileName zipParams.Level
        createArchiveStream (stream zipParams) fileName

    let compress zipParams =
        createArchive (createFile zipParams) addEntry

module GZip =
    type GZipCompressionParams = { Level : CompressionLevel.T }

    let GZipCompressionDefaults = { Level = CompressionLevel.Default }

    let stream { Level = level } inner =
        let gzipStream = new GZipOutputStream(inner)
        gzipStream.SetLevel <| CompressionLevel.value level
        gzipStream

    let createFile gzipParams fileName =
        tracefn "Creating gz archive: %s (%A)" fileName gzipParams.Level
        createArchiveStream (stream gzipParams) fileName

    let compressFile level outFile fileName =
        use inStream = (fileInfo fileName).OpenRead()
        createArchive (createFile level) (copyFileBuffered DefaultBufferSize) outFile (Seq.singleton inStream)

module BZip2 =
    let stream inner = new BZip2OutputStream(inner)

    let createFile fileName =
        tracefn "Creating bz2 archive: %s" fileName
        createArchiveStream stream fileName

    let compressFile level outFile fileName =
        use inStream = (fileInfo fileName).OpenRead()
        createArchive createFile (copyFileBuffered DefaultBufferSize) outFile (Seq.singleton inStream)

module Tar =
    let private addEntry (outStream : TarOutputStream) =
        let prepareEntry (fileInfo : FileInfo) itemSpec =
            let entry = TarEntry.CreateTarEntry itemSpec
            entry.ModTime <- fileInfo.LastWriteTime
            entry.Size <- fileInfo.Length
            outStream.PutNextEntry(entry)
            entry.Name
        let afterEntry (outStream : TarOutputStream) =
            outStream.CloseEntry()
        addEntry prepareEntry afterEntry outStream

    let stream inner = new TarOutputStream(inner)

    let createFile fileName =
        tracefn "Creating tar archive: %s" fileName
        createArchiveStream stream fileName

    let compress fileName items =
        createArchive createFile addEntry fileName items

    module GZip =
        let stream gzipParams = GZip.stream gzipParams >> stream

        let createFile (gzipParams : GZip.GZipCompressionParams) fileName =
            tracefn "Creating tar.gz archive: %s (%A)" fileName gzipParams.Level
            createArchiveStream (stream gzipParams) fileName
    
        let compress gzipParam =
            createArchive (createFile gzipParam) addEntry

    module BZip2 =
        let stream = BZip2.stream >> stream

        let createFile fileName =
            tracefn "Creating tar.bz2 archive: %s" fileName
            createArchiveStream stream fileName

        let compress fileName items =
            createArchive createFile addEntry fileName items

let private doCompression compressor archivePath fileSpecGenerator =
    Seq.map fileSpecGenerator
    >> compressor archivePath

let private buildFileSpec flatten baseDir =
    (if flatten then archiveFileSpec else archiveFileSpecWithBaseDir baseDir)

/// Creates a zip archive with the given files.
/// ## Parameters
///  - `zipParams` - The compression parameters.
///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
///  - `baseDir` - The relative dir of the files to be compressed. Use this parameter to influence directory structure within the archive.
///  - `archivePath` - The fileName of the resulting archive.
///  - `files` - A sequence with files to compress.
let CreateZip zipParams flatten baseDir archivePath files =
    doCompression (Zip.compress zipParams) archivePath (buildFileSpec flatten baseDir) files

/// Creates a zip archive with the given files with default parameters.
/// ## Parameters
///  - `baseDir` - The relative dir of the files to be compressed. Use this parameter to influence directory structure within the archive.
///  - `archivePath` - The fileName of the resulting archive.
///  - `files` - A sequence with files to compress.
let Zip baseDir archivePath files =
    doCompression (Zip.compress Zip.ZipCompressionDefaults) archivePath (buildFileSpec false baseDir) files

/// Creates a tar.gz archive with the given files.
/// ## Parameters
///  - `gzipParams` - The compression parameters.
///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
///  - `baseDir` - The relative dir of the files to be compressed. Use this parameter to influence directory structure within the archive.
///  - `archivePath` - The fileName of the resulting archive.
///  - `files` - A sequence with files to compress.
let CreateTarGZip gzipParams flatten baseDir archivePath files =
    doCompression (Tar.GZip.compress gzipParams) archivePath (buildFileSpec flatten baseDir) files

/// Creates a tar.gz archive with the given files with default parameters.
/// ## Parameters
///  - `baseDir` - The relative dir of the files to be compressed. Use this parameter to influence directory structure within the archive.
///  - `archivePath` - The fileName of the resulting archive.
///  - `files` - A sequence with files to compress.
let TarGZip baseDir archivePath files =
    doCompression (Tar.GZip.compress GZip.GZipCompressionDefaults) archivePath (buildFileSpec false baseDir) files

/// Creates a tar.bz2 archive with the given files.
/// ## Parameters
///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
///  - `baseDir` - The relative dir of the files to be compressed. Use this parameter to influence directory structure within the archive.
///  - `archivePath` - The fileName of the resulting archive.
///  - `files` - A sequence with files to compress.
let CreateTarBZip2 flatten baseDir archivePath files =
    doCompression Tar.BZip2.compress archivePath (buildFileSpec flatten baseDir) files

/// Creates a tar.gz archive with the given files with default parameters.
/// ## Parameters
///  - `baseDir` - The relative dir of the files to be compressed. Use this parameter to influence directory structure within the archive.
///  - `archivePath` - The fileName of the resulting archive.
///  - `files` - A sequence with files to compress.
let TarBZip2 baseDir archivePath files =
    doCompression Tar.BZip2.compress archivePath (buildFileSpec false baseDir) files
