/// Provides utility tasks for storing and compressing files in archives.
module Fake.ArchiveHelper

open System
open System.IO
open ICSharpCode.SharpZipLib.Core

[<Literal>]
let private DefaultBufferSize = 32768

/// A description of a file to be added to an archive.
type ArchiveFileSpec = { InputFile : FileInfo; ArchiveEntryPath : string }

type private ExtractEntrySpec = { OutputFile : FileInfo; ArchiveEntryPath : string; EntrySize : int64 }

let private copyStreamBuffered bufferSize (outStream : #Stream) (inStream : #Stream) length =
    let rec doCopy buffer (outStream : #Stream) (inStream : #Stream) length =
        if length > 0L then
            let count = inStream.Read(buffer, 0, buffer.Length)
            outStream.Write(buffer, 0, count)
            doCopy buffer outStream inStream (length - int64 count)

    let buffer = Array.zeroCreate<byte> bufferSize
    doCopy buffer outStream inStream length

let private copyFileBuffered bufferSize (outStream : #Stream) (inStream : #Stream) =
    inStream.Seek(0L, SeekOrigin.Begin) |> ignore
    copyStreamBuffered bufferSize outStream inStream inStream.Length
    
let private addEntry prepareEntry afterEntry (outStream : #Stream) { InputFile = inFile; ArchiveEntryPath = entryPath } =
    use inStream = inFile.OpenRead()
    let entryName = prepareEntry inFile entryPath
    copyFileBuffered DefaultBufferSize outStream inStream
    logfn "Compressed %s => %s" inFile.FullName entryName
    afterEntry outStream

let private extractEntry (inStream : #Stream) entry =
    entry.OutputFile.Directory |> ensureDirExists
    use outStream = entry.OutputFile.Create()
    copyStreamBuffered DefaultBufferSize outStream inStream entry.EntrySize
    logfn "Extracted %s => %s" entry.ArchiveEntryPath entry.OutputFile.FullName

let private createArchiveStream (streamCreator : Stream -> #Stream) (archiveFile : FileInfo) =
    archiveFile.Create() :> Stream
    |> streamCreator

let private openArchiveStream (streamCreator : Stream -> #Stream) (archiveFile : FileInfo) =
    archiveFile.OpenRead() :> Stream
    |> streamCreator

let private createArchive streamCreator addEntry (archiveFile : FileInfo) items =
    use stream = streamCreator archiveFile
    Seq.iter (addEntry stream) items
    tracefn "Archive successfully created %s" archiveFile.FullName

let rec private extractEntries getNextEntry (inStream : #Stream) =
    match getNextEntry inStream with
    | Some entry -> 
        extractEntry inStream entry
        extractEntries getNextEntry inStream
    | None -> ()

/// Constructs a file specification which will archive the file at the root.
let archiveFileSpec (file : FileInfo) =
    { InputFile = file; ArchiveEntryPath = file.Name }

/// Constructs a file specification which will archive the file with a path relative to the `baseDir`.
let archiveFileSpecWithBaseDir (baseDir : DirectoryInfo) (file : FileInfo) =
    if not baseDir.Exists then failwithf "Directory not found: %s" baseDir.FullName
    if not <| isInFolder baseDir file then failwithf "File not in base directory: (BaseDir: %s, File: %s)" baseDir.FullName file.FullName
    { InputFile = file; ArchiveEntryPath = replace (baseDir.FullName + directorySeparator) "" file.FullName }

let private doCompression compressor archivePath fileSpecGenerator =
    Seq.map fileSpecGenerator
    >> compressor archivePath

let private buildFileSpec flatten baseDir =
    (if flatten then archiveFileSpec else archiveFileSpecWithBaseDir baseDir)

let private allFilesInDirectory (baseDir : DirectoryInfo) =
    !! (baseDir.FullName @@ "**" @@ "*")
    |> Seq.map fileInfo

module CompressionLevel =
    type T = CompressionLevel of int

    let private clipLevel = max 0 >> min 9

    let Default = CompressionLevel 7

    let create level = CompressionLevel (clipLevel level)

    let value (CompressionLevel l) = l

module Zip =
    open ICSharpCode.SharpZipLib.Zip
    
    type ZipCompressionParams = { Comment : string option; Level : CompressionLevel.T }

    let ZipCompressionDefaults = { Comment = None; Level = CompressionLevel.Default }

    let addZipEntry (outStream : ZipOutputStream) =
        let prepareEntry (fileInfo : FileInfo) itemSpec =
            let entry = new ZipEntry(ZipEntry.CleanName itemSpec)
            entry.DateTime <- fileInfo.LastWriteTime
            entry.Size <- fileInfo.Length
            outStream.PutNextEntry(entry)
            entry.Name
        let afterEntry (outStream : ZipOutputStream) =
            outStream.CloseEntry()
        addEntry prepareEntry afterEntry outStream

    let compressStream { Level = level; Comment = comment } inner =
        let zipStream = new ZipOutputStream(inner)
        zipStream.SetLevel <| CompressionLevel.value level
        match comment with | Some c -> zipStream.SetComment <| c | _ -> ()
        zipStream

    let extractStream inner = new ZipInputStream(inner)

    let createFile zipParams (file : FileInfo) =
        tracefn "Creating zip archive: %s (%A)" file.FullName zipParams.Level
        createArchiveStream (compressStream zipParams) file

    let compress zipParams =
        createArchive (createFile zipParams) addZipEntry

    let extract (extractDir : DirectoryInfo) (archiveFile : FileInfo) =
        let rec getNextEntry (stream : ZipInputStream) =
            let entry = stream.GetNextEntry()
            match entry with
            | null -> None
            | _ when not entry.IsFile -> getNextEntry stream
            | _ ->
                let outFile = extractDir.FullName @@ entry.Name |> fileInfo
                Some { OutputFile = outFile; ArchiveEntryPath = entry.Name; EntrySize = entry.Size }

        openArchiveStream extractStream archiveFile
        |> extractEntries getNextEntry 
        logfn "Extracted %s" archiveFile.FullName

    /// Extracts a zip archive to a given directory.
    /// ## Parameters
    ///  - `targetDir` - The directory into which the archived files will be extracted.
    ///  - `archivePath` - The archive to be extracted.
    let Extract targetDir archivePath =
        extract targetDir archivePath

    /// Creates a zip archive with the given files.
    /// ## Parameters
    ///  - `setParams` - A function which modifies the default compression parameters.
    ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
    ///  - `baseDir` - The relative directory of the files to be compressed. Use this parameter to influence directory structure within the archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `files` - A sequence of files to compress.
    let Compress setParams flatten baseDir archiveFile files =
        doCompression (setParams ZipCompressionDefaults |> compress) archiveFile (buildFileSpec flatten baseDir) files

    /// Creates a zip archive with the given archive file specifications.
    /// ## Parameters
    ///  - `setParams` - A function which modifies the default compression parameters.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `fileSpecs` - A sequence of archive file specifications.
    let CompressSpecs setParams archiveFile fileSpecs =
        doCompression (setParams ZipCompressionDefaults |> compress) archiveFile id fileSpecs

    /// Creates a zip archive with the given files with default parameters.
    /// ## Parameters
    ///  - `baseDir` - The relative directory of the files to be compressed. Use this parameter to influence directory structure within the archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `files` - A sequence of files to compress.
    let CompressWithDefaults baseDir archiveFile files =
        Compress id false baseDir archiveFile files

    /// Creates a zip archive containing all the files in a directory.
    /// ## Parameters
    ///  - `setParams` - A function which modifies the default compression parameters.
    ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
    ///  - `baseDir` - The base directory to be compressed. This directory will be the root of the resulting archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    let CompressDir setParams flatten (baseDir : DirectoryInfo) archiveFile =
        allFilesInDirectory baseDir |> Compress setParams flatten baseDir archiveFile

    /// Creates a zip archive containing all the files in a directory.
    /// ## Parameters
    ///  - `baseDir` - The base directory to be compressed. This directory will be the root of the resulting archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    let CompressDirWithDefaults (baseDir : DirectoryInfo) archiveFile =
        allFilesInDirectory baseDir |> CompressWithDefaults baseDir archiveFile

module GZip =
    open ICSharpCode.SharpZipLib.GZip

    type GZipCompressionParams = { Level : CompressionLevel.T }

    let GZipCompressionDefaults = { Level = CompressionLevel.Default }

    let compressStream { Level = level } inner =
        let gzipStream = new GZipOutputStream(inner)
        gzipStream.SetLevel <| CompressionLevel.value level
        gzipStream

    let extractStream inner = new GZipInputStream(inner)

    let createFile gzipParams (file : FileInfo) =
        tracefn "Creating gz archive: %s (%A)" file.FullName gzipParams.Level
        createArchiveStream (compressStream gzipParams) file

    /// Extracts a file compressed with gzip.
    /// ## Parameters
    ///  - `outFile` - The extracted output file. If existing, will be overwritten.
    ///  - `file` - The compressed file.
    let ExtractFile outFile (file : FileInfo) =
        use inStream = openArchiveStream extractStream file
        extractEntry inStream { OutputFile = outFile; ArchiveEntryPath = file.FullName; EntrySize = inStream.Length }

    /// Compresses a file using gzip.
    /// ## Parameters
    ///  - `setParams` - A function which modifies the default compression parameters.
    ///  - `outFile` - The compressed output file. If existing, will be overwritten.
    ///  - `file` - The file to be compressed.
    let CompressFile setParams outFile (file : FileInfo) =
        use inStream = file.OpenRead()
        createArchive (setParams GZipCompressionDefaults |> createFile) (copyFileBuffered DefaultBufferSize) outFile (Seq.singleton inStream)

    /// Compresses a file using gzip.
    /// ## Parameters
    ///  - `outFile` - The compressed output file. If existing, will be overwritten.
    ///  - `file` - The file to be compressed.
    let CompressFileWithDefaults outFile (file : FileInfo) =
        use inStream = file.OpenRead()
        createArchive (createFile GZipCompressionDefaults) (copyFileBuffered DefaultBufferSize) outFile (Seq.singleton inStream)

module BZip2 =
    open ICSharpCode.SharpZipLib.BZip2

    let compressStream inner = new BZip2OutputStream(inner)

    let extractStream inner = new BZip2InputStream(inner)

    let createFile (file : FileInfo) =
        tracefn "Creating bz2 archive: %s" file.FullName
        createArchiveStream compressStream file

    /// Extracts a file compressed with bzip2.
    /// ## Parameters
    ///  - `outFile` - The extracted output file. If existing, will be overwritten.
    ///  - `file` - The compressed file.
    let ExtractFile outFile (file : FileInfo) =
        use inStream = openArchiveStream extractStream file
        extractEntry inStream { OutputFile = outFile; ArchiveEntryPath = file.FullName; EntrySize = inStream.Length }

    /// Compresses a file using bzip2.
    /// ## Parameters
    ///  - `outFile` - The compressed output file. If existing, will be overwritten.
    ///  - `file` - The file to be compressed.
    let CompressFile outFile (file : FileInfo) =
        use inStream = file.OpenRead()
        createArchive createFile (copyFileBuffered DefaultBufferSize) outFile (Seq.singleton inStream)

module Tar =
    open ICSharpCode.SharpZipLib.Tar

    let addEntry (outStream : TarOutputStream) =
        let prepareEntry (fileInfo : FileInfo) itemSpec =
            let entry = TarEntry.CreateTarEntry itemSpec
            entry.ModTime <- fileInfo.LastWriteTime
            entry.Size <- fileInfo.Length
            outStream.PutNextEntry(entry)
            entry.Name
        let afterEntry (outStream : TarOutputStream) =
            outStream.CloseEntry()
        addEntry prepareEntry afterEntry outStream

    let compressStream inner = new TarOutputStream(inner)

    let extractStream inner = new TarInputStream(inner)

    let createFile (file : FileInfo) =
        tracefn "Creating tar archive: %s" file.FullName
        createArchiveStream compressStream file

    let compress file items =
        createArchive createFile addEntry file items

    let rec private getNextEntry (extractDir : DirectoryInfo) (stream : TarInputStream) =
        let entry = stream.GetNextEntry()
        match entry with
        | null -> None
        | _ when entry.IsDirectory -> getNextEntry extractDir stream
        | _ ->
            let outFile = extractDir.FullName @@ entry.Name |> fileInfo
            Some { OutputFile = outFile; ArchiveEntryPath = entry.Name; EntrySize = entry.Size }

    let extract (extractDir : DirectoryInfo) (archiveFile : FileInfo) =
        openArchiveStream extractStream archiveFile
        |> extractEntries (getNextEntry extractDir)
        logfn "Extracted %s" archiveFile.FullName

    /// Extracts a tar archive to a given directory.
    /// ## Parameters
    ///  - `targetDir` - The directory into which the archived files will be extracted.
    ///  - `archivePath` - The archive to be extracted.
    let Extract targetDir archivePath =
        extract targetDir archivePath

    /// Creates a tar archive with the given files.
    /// ## Parameters
    ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
    ///  - `baseDir` - The relative directory of the files to be archived. Use this parameter to influence directory structure within the archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `files` - A sequence of files to store.
    let Store flatten baseDir archiveFile files =
        doCompression compress archiveFile (buildFileSpec flatten baseDir) files

    /// Creates a tar archive with the given archive file specifications.
    /// ## Parameters
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `fileSpecs` - A sequence of archive file specifications.
    let StoreSpecs archiveFile fileSpecs =
        doCompression compress archiveFile id fileSpecs

    /// Creates a tar archive with the given files with default parameters.
    /// ## Parameters
    ///  - `baseDir` - The relative directory of the files to be archived. Use this parameter to influence directory structure within the archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `files` - A sequence of files to store.
    let StoreWithDefaults baseDir archiveFile files =
        Store false baseDir archiveFile files

    /// Creates a tar archive containing all the files in a directory.
    /// ## Parameters
    ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
    ///  - `baseDir` - The base directory to be archived. This directory will be the root of the resulting archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    let CompressDir flatten (baseDir : DirectoryInfo) archiveFile =
        allFilesInDirectory baseDir |> Store flatten baseDir archiveFile

    /// Creates a tar.gz archive containing all the files in a directory.
    /// ## Parameters
    ///  - `baseDir` - The base directory to be archived. This directory will be the root of the resulting archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    let CompressDirWithDefaults (baseDir : DirectoryInfo) archiveFile =
        allFilesInDirectory baseDir |> StoreWithDefaults baseDir archiveFile

    module GZip =
        let compressStream gzipParams = GZip.compressStream gzipParams >> compressStream

        let extractStream = GZip.extractStream >> extractStream

        let createFile (gzipParams : GZip.GZipCompressionParams) (file : FileInfo) =
            tracefn "Creating tar.gz archive: %s (%A)" file.FullName gzipParams.Level
            createArchiveStream (compressStream gzipParams) file
    
        let compress gzipParam =
            createArchive (createFile gzipParam) addEntry

        let extract extractDir archiveFile =
            openArchiveStream extractStream archiveFile
            |> extractEntries (getNextEntry extractDir)
            logfn "Extracted %s" archiveFile.FullName

        /// Extracts a tar.gz archive to a given directory.
        /// ## Parameters
        ///  - `targetDir` - The directory into which the archived files will be extracted.
        ///  - `archivePath` - The archive to be extracted.
        let Extract targetDir archivePath =
            extract targetDir archivePath

        /// Creates a tar.gz archive with the given files.
        /// ## Parameters
        ///  - `setParams` - A function which modifies the default compression parameters.
        ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
        ///  - `baseDir` - The relative directory of the files to be compressed. Use this parameter to influence directory structure within the archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        ///  - `files` - A sequence of files to compress.
        let Compress setParams flatten baseDir archiveFile files =
            doCompression (setParams GZip.GZipCompressionDefaults |> compress) archiveFile (buildFileSpec flatten baseDir) files

        /// Creates a tar.gz archive with the given archive file specifications.
        /// ## Parameters
        ///  - `setParams` - A function which modifies the default compression parameters.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        ///  - `fileSpecs` - A sequence of archive file specifications.
        let CompressSpecs setParams archiveFile fileSpecs =
            doCompression (setParams GZip.GZipCompressionDefaults |> compress) archiveFile id fileSpecs

        /// Creates a tar.gz archive with the given files with default parameters.
        /// ## Parameters
        ///  - `baseDir` - The relative directory of the files to be compressed. Use this parameter to influence directory structure within the archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        ///  - `files` - A sequence of files to compress.
        let CompressWithDefaults baseDir archiveFile files =
            Compress id false baseDir archiveFile files

        /// Creates a tar.gz archive containing all the files in a directory.
        /// ## Parameters
        ///  - `setParams` - A function which modifies the default compression parameters.
        ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
        ///  - `baseDir` - The base directory to be compressed. This directory will be the root of the resulting archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        let CompressDir setParams flatten (baseDir : DirectoryInfo) archiveFile =
            allFilesInDirectory baseDir |> Compress setParams flatten baseDir archiveFile

        /// Creates a tar.gz archive containing all the files in a directory.
        /// ## Parameters
        ///  - `baseDir` - The base directory to be compressed. This directory will be the root of the resulting archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        let CompressDirWithDefaults (baseDir : DirectoryInfo) archiveFile =
            allFilesInDirectory baseDir |> CompressWithDefaults baseDir archiveFile

    module BZip2 =
        let compressStream = BZip2.compressStream >> compressStream

        let extractStream = BZip2.extractStream >> extractStream

        let createFile (file : FileInfo) =
            tracefn "Creating tar.bz2 archive: %s" file.FullName
            createArchiveStream compressStream file

        let compress =
            createArchive createFile addEntry

        let extract extractDir archiveFile =
            openArchiveStream extractStream archiveFile
            |> extractEntries (getNextEntry extractDir)
            logfn "Extracted %s" archiveFile.FullName

        /// Extracts a tar.bz2 archive to a given directory.
        /// ## Parameters
        ///  - `targetDir` - The directory into which the archived files will be extracted.
        ///  - `archivePath` - The archive to be extracted.
        let Extract targetDir archivePath =
            extract targetDir archivePath

        /// Creates a tar.bz2 archive with the given files.
        /// ## Parameters
        ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
        ///  - `baseDir` - The relative directory of the files to be compressed. Use this parameter to influence directory structure within the archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        ///  - `files` - A sequence of files to compress.
        let Compress flatten baseDir archiveFile files =
            doCompression compress archiveFile (buildFileSpec flatten baseDir) files

        /// Creates a tar.bz2 archive with the given archive file specifications.
        /// ## Parameters
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        ///  - `fileSpecs` - A sequence of archive file specifications.
        let CompressSpecs archiveFile fileSpecs =
            doCompression compress archiveFile id fileSpecs

        /// Creates a tar.bz2 archive with the given files with default parameters.
        /// ## Parameters
        ///  - `baseDir` - The relative directory of the files to be compressed. Use this parameter to influence directory structure within the archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        ///  - `files` - A sequence of files to compress.
        let CompressWithDefaults baseDir archiveFile files =
            Compress false baseDir archiveFile files

        /// Creates a tar.bz2 archive containing all the files in a directory.
        /// ## Parameters
        ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
        ///  - `baseDir` - The base directory to be compressed. This directory will be the root of the resulting archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        let CompressDir flatten (baseDir : DirectoryInfo) archiveFile =
            allFilesInDirectory baseDir |> Compress flatten baseDir archiveFile

        /// Creates a tar.bz2 archive containing all the files in a directory.
        /// ## Parameters
        ///  - `baseDir` - The base directory to be compressed. This directory will be the root of the resulting archive.
        ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
        let CompressDirWithDefaults (baseDir : DirectoryInfo) archiveFile =
            allFilesInDirectory baseDir |> CompressWithDefaults baseDir archiveFile