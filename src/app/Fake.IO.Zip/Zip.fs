namespace Fake.IO

/// <summary>
/// This module contains helper function to create and extract zip archives.
/// </summary>
[<RequireQualifiedAccess>]
module Zip =

    open System.IO
#if DOTNETCORE // Wait for SharpZipLib to become available for netcore
    open System.IO.Compression
#if NETSTANDARD1_6
    open System.Reflection
#endif
#else
    // No SharpZipLib for netcore
    open ICSharpCode.SharpZipLib.Zip
    open ICSharpCode.SharpZipLib.Core
#endif
    open System
    open Fake.Core
    open Fake.IO

    /// The default zip level
    let DefaultZipLevel = 7


#if DOTNETCORE // Wait for SharpZipLib to become available for netcore

    let private createZipInternal fileName (comment: string) level (items: (string * string) seq) =
        ignore comment
        use stream = new ZipArchive(File.Create(fileName), ZipArchiveMode.Create)
        let zipLevel = min (max 0 level) 9
        let buffer = Array.create 32768 0uy

        for item, itemSpec in items do
            let fixedSpec = itemSpec.Replace(@"\", "/").TrimStart('/')
            let entry = stream.CreateEntryFromFile(item, fixedSpec)
            ()

#else

    let private addZipEntry (stream: ZipOutputStream) (buffer: byte[]) (item: string) (itemSpec: string) =
        let info = FileInfo.ofPath item
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


    let private createZipInternal fileName comment level (items: (string * string) seq) =
        use stream = new ZipOutputStream(File.Create(fileName))
        let zipLevel = min (max 0 level) 9
        stream.SetLevel zipLevel

        if not (String.IsNullOrEmpty comment) then
            stream.SetComment comment

        let buffer = Array.create 32768 0uy

        for item, itemSpec in items do
            addZipEntry stream buffer item itemSpec

        stream.Finish()

#endif

    /// <summary>
    /// Creates a zip file with the given files
    /// </summary>
    ///
    /// <param name="workingDir">The relative dir of the zip files. Use this parameter to influence directory structure
    /// within zip file.</param>
    /// <param name="fileName">The fileName of the resulting zip file.</param>
    /// <param name="comment">A comment for the resulting zip file (currently ignored in fake 5 and above).</param>
    /// <param name="level">The compression level.</param>
    /// <param name="flatten">If set to true then all subfolders are merged into the root folder.</param>
    /// <param name="files">A sequence with files to zip.</param>
    let createZip workingDir fileName comment level flatten files =
        let workingDir =
            let dir = DirectoryInfo.ofPath workingDir

            if not dir.Exists then
                failwithf "Directory not found: %s" dir.FullName

            dir.FullName

        let items =
            seq {
                for item in files do
                    let info = FileInfo.ofPath item

                    if info.Exists then
                        let itemSpec =
                            if flatten then
                                info.Name
                            else if
                                not (String.IsNullOrEmpty(workingDir))
                                && info.FullName.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase)
                            then
                                info.FullName.Remove(0, workingDir.Length)
                            else
                                info.FullName

                        yield item, itemSpec
            }

        createZipInternal fileName comment level items

    /// <summary>
    /// Creates a zip file with the given files.
    /// </summary>
    ///
    /// <param name="workingDir">The relative dir of the zip files. Use this parameter to influence directory structure
    /// within zip file.</param>
    /// <param name="fileName">The file name of the resulting zip file.</param>
    /// <param name="files">A sequence with files to zip.</param>
    let zip workingDir fileName files =
        createZip workingDir fileName "" DefaultZipLevel false files

    /// <summary>
    /// Creates a zip file with the given files and specs.
    /// </summary>
    ///
    /// <param name="fileName">The fileName of the resulting zip file.</param>
    /// <param name="comment">A comment for the resulting zip file (currently ignored in fake 5 and above).</param>
    /// <param name="level">The compression level.</param>
    /// <param name="items">A sequence with files and their target location in the zip.</param>
    let createZipSpec fileName comment level items =
        createZipInternal fileName comment level items

    /// <summary>
    /// Creates a zip file with the given files and specs.
    /// </summary>
    ///
    /// <param name="fileName">The fileName of the resulting zip file.</param>
    /// <param name="items">A sequence with files and their target location in the zip.</param>
    let zipSpec fileName items =
        createZipSpec fileName "" DefaultZipLevel items

    /// <summary>
    /// Creates a zip file with the given file.
    /// </summary>
    ///
    /// <param name="fileName">The file name of the resulting zip file.</param>
    /// <param name="targetFileName">The file to zip.</param>
    let zipFile fileName targetFileName =
        let fi = FileInfo.ofPath targetFileName
        createZip fi.Directory.FullName fileName "" DefaultZipLevel false [ fi.FullName ]


    /// <summary>
    /// Unzips a file with the given file name.
    /// </summary>
    ///
    /// <param name="target">The target directory.</param>
    /// <param name="fileName">The file name of the zip file.</param>
    let unzip target (fileName: string) =
#if DOTNETCORE
        use stream = new FileStream(fileName, FileMode.Open)
        use zipFile = new ZipArchive(stream)

        for zipEntry in zipFile.Entries do
            let unzipPath = Path.Combine(target, zipEntry.FullName)
            let directoryPath = Path.GetDirectoryName(unzipPath)

            if unzipPath.EndsWith "/" then
                Directory.CreateDirectory(unzipPath) |> ignore
            else
                // unzip the file
                Directory.ensure directoryPath
                let zipStream = zipEntry.Open()

                if unzipPath.EndsWith "/" |> not then
                    use unzippedFileStream = File.Create(unzipPath)
                    zipStream.CopyTo(unzippedFileStream)

#else
        use zipFile = new ZipFile(fileName)

        for entry in zipFile do
            match entry with
            | :? ZipEntry as zipEntry ->
                let unzipPath = Path.Combine(target, zipEntry.Name)
                let directoryPath = Path.GetDirectoryName(unzipPath)
                // create directory if needed
                if directoryPath.Length > 0 then
                    Directory.CreateDirectory(directoryPath) |> ignore
                // unzip the file
                let zipStream = zipFile.GetInputStream(zipEntry)
                let buffer = Array.create 32768 0uy

                if unzipPath.EndsWith "/" |> not then
                    use unzippedFileStream = File.Create(unzipPath)
                    StreamUtils.Copy(zipStream, unzippedFileStream, buffer)
            | _ -> ()
#endif

    /// <summary>
    /// Unzips a single file from the archive with the given file name.
    /// </summary>
    ///
    /// <param name="fileToUnzip">The file inside the archive.</param>
    /// <param name="zipFileName">The file name of the zip file.</param>
    let unzipSingleFileInMemory fileToUnzip (zipFileName: string) =
#if DOTNETCORE
        use stream = new FileStream(zipFileName, FileMode.Open)
        use zf = new ZipArchive(stream)
        let ze = zf.GetEntry fileToUnzip

        if isNull ze then
            raise <| ArgumentException(fileToUnzip, "not found in zip")

        use stream = ze.Open()
        use reader = new StreamReader(stream)
        reader.ReadToEnd()
#else
        use zf = new ZipFile(zipFileName)
        let ze = zf.GetEntry fileToUnzip

        if isNull ze then
            raise <| ArgumentException(fileToUnzip, "not found in zip")

        use stream = zf.GetInputStream(ze)
        use reader = new StreamReader(stream)
        reader.ReadToEnd()
#endif

    /// <summary>
    /// Unzips a single file from the archive with the given file name.
    /// </summary>
    ///
    /// <param name="predicate">The predicate for the searched file in the archive.</param>
    /// <param name="zipFileName">The file name of the zip file.</param>
    let unzipFirstMatchingFileInMemory predicate (zipFileName: string) =
#if DOTNETCORE
        use st = new FileStream(zipFileName, FileMode.Open)
        use zf = new ZipArchive(st)
        let ze = zf.Entries |> Seq.find predicate

        use stream = ze.Open()
#else
        use zf = new ZipFile(zipFileName)

        let ze =
            seq {
                for ze in zf do
                    yield ze :?> ZipEntry
            }
            |> Seq.find predicate

        use stream = zf.GetInputStream(ze)
#endif
        use reader = new StreamReader(stream)
        reader.ReadToEnd()

    let internal filesAsSpecsExt flatten workingDir (files: IGlobbingPattern) =
        seq {
            let baseFull = (Path.GetFullPath files.BaseDirectory).TrimEnd [| '/'; '\\' |]

            for item in files do
                let info = FileInfo.ofPath item

                let itemSpec =
                    if flatten then
                        info.Name
                    else
                        // first get relative path from the globbing
                        let relative = (info.FullName.Substring(baseFull.Length + 1))

                        if
                            not (String.IsNullOrEmpty(workingDir))
                            && relative.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase)
                        then
                            relative.Remove(0, workingDir.Length).TrimStart [| '/'; '\\' |]
                        else
                            relative

                yield item, itemSpec
        }

    /// <summary>
    /// This helper helps with creating complex zip file with multiple include patterns.
    /// This method will convert a given glob pattern with the given workingDir to a sequence of zip specifications.
    /// </summary>
    ///
    /// <param name="workingDir">The relative dir of the zip files. Use this parameter to influence directory structure
    /// within zip file.</param>
    /// <param name="files">A sequence of target folders and files to include relative to their base directory.</param>
    /// <param name="user">The user name</param>
    ///
    /// <example>
    /// The following sample creates a zip file containing the files from multiple patterns and moves them to different
    /// folders within the zip file.
    /// <code lang="fsharp">
    /// Target "Zip" (fun _ ->
    ///         [   !! "ci/build/project1/**/*"
    ///                 |> Zip.filesAsSpecs "ci/build/project1"
    ///                 |> Zip.moveToFolder "project1"
    ///             !! "ci/build/project2/**/*"
    ///                 |> Zip.filesAsSpecs "ci/build/project2"
    ///                 |> Zip.moveToFolder "project2"
    ///             !! "ci/build/project3/sub/dir/**/*"
    ///                 |> Zip.filesAsSpecs "ci/build/project3"
    ///                 |> Zip.moveToFolder "project3"
    ///         ]
    ///         |> Seq.concat
    ///         |> Zip.zipSpec (sprintf @"ci/deploy/project.%s.zip" buildVersion)
    ///     )
    /// </code>
    /// </example>
    let filesAsSpecs workingDir files = filesAsSpecsExt false workingDir files

    /// <summary>
    /// This helper helps with creating complex zip file with multiple include patterns.
    /// </summary>
    ///
    /// <param name="workingDir">The relative dir of the zip files. Use this parameter to influence directory structure
    /// within zip file.</param>
    /// <param name="files">A sequence of target folders and files to include relative to their base directory.</param>
    ///
    /// <example>
    /// The following sample creates a zip file containing the files from multiple patterns and moves them to different
    /// folders within the zip file.
    /// <code lang="fsharp">
    /// Target "Zip" (fun _ ->
    ///         [   !! "ci/build/project1/**/*"
    ///                 |> Zip.filesAsSpecsFlatten
    ///                 |> Zip.moveToFolder "project1"
    ///             !! "ci/build/project2/**/*"
    ///                 |> Zip.filesAsSpecsFlatten
    ///                 |> Zip.moveToFolder "project2"
    ///             !! "ci/build/project3/sub/dir/**/*"
    ///                 |> Zip.filesAsSpecs "ci/build/project3"
    ///                 |> Zip.moveToFolder "project3"
    ///         ]
    ///         |> Seq.concat
    ///         |> Zip.zipSpec (sprintf @"ci/deploy/project.%s.zip" buildVersion)
    ///     )
    /// </code>
    /// </example>
    let filesAsSpecsFlatten files = filesAsSpecsExt true "" files

    /// <summary>
    /// This helper helps with creating complex zip file with multiple include patterns.
    /// This function will move a given list of zip specifications to the given folder (while keeping original folder
    /// structure intact).
    /// </summary>
    ///
    /// <param name="workingDir">The relative dir of the zip files. Use this parameter to influence directory structure
    /// within zip file.</param>
    /// <param name="files">A sequence of target folders and files to include relative to their base directory.</param>
    ///
    /// <example>
    /// The following sample creates a zip file containing the files from multiple patterns and moves them to different
    /// folders within the zip file.
    /// <code lang="fsharp">
    /// Target "Zip" (fun _ ->
    ///         [   !! "ci/build/project1/**/*"
    ///                 |> Zip.filesAsSpecsFlatten
    ///                 |> Zip.moveToFolder "project1"
    ///             !! "ci/build/project2/**/*"
    ///                 |> Zip.filesAsSpecsFlatten
    ///                 |> Zip.moveToFolder "project2"
    ///             !! "ci/build/project3/sub/dir/**/*"
    ///                 |> Zip.filesAsSpecs "ci/build/project3"
    ///                 |> Zip.moveToFolder "project3"
    ///         ]
    ///         |> Seq.concat
    ///         |> Zip.zipSpec (sprintf @"ci/deploy/project.%s.zip" buildVersion)
    ///     )
    /// </code>
    /// </example>
    let moveToFolder path items =
        seq {
            for file, oldSpec in items do
                let info = FileInfo.ofPath file
                //if info.Exists then
                let path =
                    if String.IsNullOrEmpty path then
                        ""
                    else
                        sprintf "%s%c" (path.TrimEnd [| '/'; '\\' |]) Path.DirectorySeparatorChar

                let spec = sprintf "%s%s" path oldSpec
                yield file, spec
        }


    /// <summary>
    /// Creates a zip file with the given files.
    /// </summary>
    ///
    /// <param name="fileName">The file name of the resulting zip file.</param>
    /// <param name="comment">A comment for the resulting zip file (currently ignored in fake 5 and above).</param>
    /// <param name="level">The compression level.</param>
    /// <param name="files">A sequence of target folders and files to include relative to their base directory.</param>
    let createZipOfIncludes fileName comment level (files: (string * IGlobbingPattern) seq) =
        files
        |> Seq.map (fun (wd, glob) -> glob |> filesAsSpecs "" |> moveToFolder wd)
        |> Seq.concat
        |> createZipSpec fileName comment level
    //let items = seq {
    //    for path, incl in files do
    //        for file in incl do
    //            let info = FileInfo.ofPath file
    //            if info.Exists then
    //                let baseFull = (Path.GetFullPath incl.BaseDirectory).TrimEnd [|'/';'\\'|]
    //                let path =
    //                    if String.IsNullOrEmpty path then ""
    //                    else sprintf "%s%c" (path.TrimEnd [|'/';'\\'|]) Path.DirectorySeparatorChar
    //                let spec = sprintf "%s%s" path (info.FullName.Substring (baseFull.Length+1))
    //                yield file, spec }
    //createZip fileName comment level items

    /// <summary>
    /// Creates a zip file with the given files.
    /// </summary>
    ///
    /// <param name="fileName">The file name of the resulting zip file.</param>
    /// <param name="files">A sequence of target folders and files to include relative to their base directory.</param>
    ///
    /// <example>
    /// The following sample creates a zip file containing the files from the two target folders and FileIncludes.
    /// <list type="number">
    /// <item>
    /// The files from the first FileInclude will be placed in the root of the zip file.
    /// </item>
    /// <item>
    /// The files from the second FileInclude will be placed under the directory <c>app_data\jobs\continuous\MyWebJob</c>
    /// </item>
    /// </list>
    /// in the zip file.
    /// <code lang="fsharp">
    /// Target "Zip" (fun _ ->
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
    ///         |> Zip.zipOfIncludes (sprintf @"bin\MyWebApp.%s.zip" buildVersion)
    ///     )
    /// </code>
    /// </example>
    let zipOfIncludes fileName files =
        createZipOfIncludes fileName "" DefaultZipLevel files
