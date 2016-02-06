/// Provides utility tasks for storing and compressing files in archives.
module Fake.ArchiveHelper

open System.IO

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
    let entryPath = replace (baseDir.FullName + directorySeparator) "" file.FullName
    { InputFile = file; ArchiveEntryPath = replace directorySeparator "/" entryPath }

let private doCompression compressor archivePath fileSpecGenerator =
    Seq.map fileSpecGenerator
    >> compressor archivePath

let private buildFileSpec flatten baseDir =
    (if flatten then archiveFileSpec else archiveFileSpecWithBaseDir baseDir)

let private allFilesInDirectory (baseDir : DirectoryInfo) =
    !! (baseDir.FullName @@ "**" @@ "*")
    |> Seq.map fileInfo

/// Provides validation of comression levels used for the zip and gzip compression algorithms.
module CompressionLevel =
    /// Defines the compression level type.
    type T = CompressionLevel of int

    let private clipLevel = max 0 >> min 9

    /// The default compression level.
    let Default = CompressionLevel 7

    /// Constructs a `CompressionLevel`. Level is clipped to a value between 0 and 9.
    let create level = CompressionLevel (clipLevel level)

    /// Retrieves the numeric compression level.
    let value (CompressionLevel l) = l

/// Operations and tasks for working with zip archives.
module Zip =
    open ICSharpCode.SharpZipLib.Zip
    
    /// The zip archive compression parameters.
    type ZipCompressionParams = { Comment : string option; Level : CompressionLevel.T }

    /// The default zip archive compression parameters
    /// ## Defaults
    ///  - `Level` - `CompressionLevel.Default`
    ///  - `Comment` - `None`
    let ZipCompressionDefaults = { Comment = None; Level = CompressionLevel.Default }

    /// Adds a file, specified by an `ArchiveFileSpec`, to a `ZipOutputStream`.
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

    /// Wraps an output stream with a zip compressor.
    let compressStream { Level = level; Comment = comment } inner =
        let zipStream = new ZipOutputStream(inner)
        zipStream.SetLevel <| CompressionLevel.value level
        match comment with | Some c -> zipStream.SetComment <| c | _ -> ()
        zipStream

    /// Wraps an input stream with a zip decompressor.
    let extractStream inner = new ZipInputStream(inner)

    /// Creates a `ZipOutputStream` wrapping a file using the given parameters.
    /// ## Parameters
    ///  - `zipParams` - The zip compression parameters.
    ///  - `file` - The `FileInfo` describing the location to which the archive should be written. Will be overwritten if it exists.
    let createFile zipParams (file : FileInfo) =
        tracefn "Creating zip archive: %s (%A)" file.FullName zipParams.Level
        createArchiveStream (compressStream zipParams) file

    /// Constructs a function that will create a zip archive from a set of files.
    let compress zipParams =
        createArchive (createFile zipParams) addZipEntry

    /// Extracts a zip archive to a given directory.
    /// ## Parameters
    ///  - `extractDir` - The directory into which the archived files will be extracted.
    ///  - `archiveFile` - The archive to be extracted.
    let Extract (extractDir : DirectoryInfo) (archiveFile : FileInfo) =
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

/// Operations and tasks for working with gzip compressed files.
module GZip =
    open ICSharpCode.SharpZipLib.GZip

    /// The gzip archive compression parameters.
    type GZipCompressionParams = { Level : CompressionLevel.T }

    /// The default gzip archive compression parameters
    /// ## Defaults
    ///  - `Level` - `CompressionLevel.Default`
    let GZipCompressionDefaults = { Level = CompressionLevel.Default }

    /// Wraps an output stream with a gzip compressor.
    let compressStream { Level = level } inner =
        let gzipStream = new GZipOutputStream(inner)
        gzipStream.SetLevel <| CompressionLevel.value level
        gzipStream

    /// Wraps an input stream with a zip decompressor.
    let extractStream inner = new GZipInputStream(inner)

    /// Creates a `GZipOutputStream` wrapping a file using the given parameters.
    /// ## Parameters
    ///  - `gzipParams` - The gzip compression parameters.
    ///  - `file` - The `FileInfo` describing the location to which the compressed file should be written. Will be overwritten if it exists.
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

/// Operations and tasks for working with gzip compressed files.
module BZip2 =
    open ICSharpCode.SharpZipLib.BZip2

    /// Wraps an output stream with a bzip2 compressor.
    let compressStream inner = new BZip2OutputStream(inner)

    /// Wraps an input stream with a bzip2 decompressor.
    let extractStream inner = new BZip2InputStream(inner)

    /// Creates a `BZip2OutputStream` wrapping a file.
    /// ## Parameters
    ///  - `file` - The `FileInfo` describing the location to which the compressed file should be written. Will be overwritten if it exists.
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

/// Operations and tasks for working with tar archives.
module Tar =
    open ICSharpCode.SharpZipLib.Tar

    /// Adds a file, specified by an `ArchiveFileSpec`, to a `TarOutputStream`.
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

    /// Wraps an output stream with a tar container store.
    let storeStream inner = new TarOutputStream(inner)

    /// Wraps an input stream with a tar container extractor.
    let extractStream inner = new TarInputStream(inner)

    /// Creates a `TarOutputStream` wrapping a file using the given parameters.
    /// ## Parameters
    ///  - `file` - The `FileInfo` describing the location to which the archive should be written. Will be overwritten if it exists.
    let createFile (file : FileInfo) =
        tracefn "Creating tar archive: %s" file.FullName
        createArchiveStream storeStream file

    /// Constructs a function that will create a tar archive from a set of files.
    let store file items =
        createArchive createFile addEntry file items

    let rec private getNextEntry (extractDir : DirectoryInfo) (stream : TarInputStream) =
        let entry = stream.GetNextEntry()
        match entry with
        | null -> None
        | _ when entry.IsDirectory -> getNextEntry extractDir stream
        | _ ->
            let outFile = extractDir.FullName @@ entry.Name |> fileInfo
            Some { OutputFile = outFile; ArchiveEntryPath = entry.Name; EntrySize = entry.Size }

    /// Extracts a tar archive to a given directory.
    /// ## Parameters
    ///  - `targetDir` - The directory into which the archived files will be extracted.
    ///  - `archiveFile` - The archive to be extracted.
    let Extract (extractDir : DirectoryInfo) (archiveFile : FileInfo) =
        openArchiveStream extractStream archiveFile
        |> extractEntries (getNextEntry extractDir)
        logfn "Extracted %s" archiveFile.FullName

    /// Creates a tar archive with the given files.
    /// ## Parameters
    ///  - `flatten` - If set to true then all subfolders are merged into the root folder of the archive.
    ///  - `baseDir` - The relative directory of the files to be archived. Use this parameter to influence directory structure within the archive.
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `files` - A sequence of files to store.
    let Store flatten baseDir archiveFile files =
        doCompression store archiveFile (buildFileSpec flatten baseDir) files

    /// Creates a tar archive with the given archive file specifications.
    /// ## Parameters
    ///  - `archiveFile` - The output archive file. If existing, will be overwritten.
    ///  - `fileSpecs` - A sequence of archive file specifications.
    let StoreSpecs archiveFile fileSpecs =
        doCompression store archiveFile id fileSpecs

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

    /// Operations and tasks for working with tar archives compressed with GZip.
    module GZip =
        /// Wraps an output stream with a tar.gz compressor.
        let compressStream gzipParams = GZip.compressStream gzipParams >> storeStream

        /// Wraps an input stream with a tar.gz decompressor.
        let extractStream = GZip.extractStream >> extractStream

        /// Creates a `TarOutputStream` wrapping a file using the given parameters.
        /// ## Parameters
        ///  - `gzipParams` - The gzip compression parameters.
        ///  - `file` - The `FileInfo` describing the location to which the archive should be written. Will be overwritten if it exists.
        let createFile (gzipParams : GZip.GZipCompressionParams) (file : FileInfo) =
            tracefn "Creating tar.gz archive: %s (%A)" file.FullName gzipParams.Level
            createArchiveStream (compressStream gzipParams) file
    
        /// Constructs a function that will create a tar.gz archive from a set of files.
        let compress gzipParam =
            createArchive (createFile gzipParam) addEntry

        /// Extracts a tar.gz archive to a given directory.
        /// ## Parameters
        ///  - `extractDir` - The directory into which the archived files will be extracted.
        ///  - `archiveFile` - The archive to be extracted.
        let Extract extractDir archiveFile =
            openArchiveStream extractStream archiveFile
            |> extractEntries (getNextEntry extractDir)
            logfn "Extracted %s" archiveFile.FullName

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

    /// Operations and tasks for working with tar archives compressed with BZip2.
    module BZip2 =
        /// Wraps an output stream with a tar.bz2 compressor.
        let compressStream = BZip2.compressStream >> storeStream

        /// Wraps an input stream with a tar.gz decompressor.
        let extractStream = BZip2.extractStream >> extractStream

        /// Creates a `TarOutputStream` wrapping a file.
        /// ## Parameters
        ///  - `file` - The `FileInfo` describing the location to which the archive should be written. Will be overwritten if it exists.
        let createFile (file : FileInfo) =
            tracefn "Creating tar.bz2 archive: %s" file.FullName
            createArchiveStream compressStream file

        /// Constructs a function that will create a tar.bz2 archive from a set of files.
        let compress =
            createArchive createFile addEntry

        /// Extracts a tar.bz2 archive to a given directory.
        /// ## Parameters
        ///  - `extractDir` - The directory into which the archived files will be extracted.
        ///  - `archiveFile` - The archive to be extracted.
        let extract extractDir archiveFile =
            openArchiveStream extractStream archiveFile
            |> extractEntries (getNextEntry extractDir)
            logfn "Extracted %s" archiveFile.FullName

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