namespace Fake.IO

open System.Text
open System.IO
open Operators

/// Contains helpers which allow to interact with the file system.
[<RequireQualifiedAccess>]
module File =
    /// see https://stackoverflow.com/questions/2502990/create-text-file-without-bom
    let internal utf8WithoutBom = UTF8Encoding(false)

    // Detect the encoding, from https://stackoverflow.com/questions/3825390/effective-way-to-find-any-files-encoding
    /// Detect the encoding
    /// 
    /// ## Parameters
    /// 
    ///  - `def` - The encoding to detect for
    ///  - `filename` - The file name to check
    let getEncoding def filename =
        // Read the BOM
        let bom = Array.zeroCreate 4
        let read =
            use file = new FileStream(filename, FileMode.Open, FileAccess.Read)
            file.Read(bom, 0, 4)

        match bom |> Array.toList with
        | _ when read < 2 -> def
        | 0xffuy :: 0xfeuy :: _ -> Encoding.Unicode //UTF-16LE
        | 0xfeuy :: 0xffuy :: _ -> Encoding.BigEndianUnicode //UTF-16BE
        | _ when read < 3 -> def
        | 0xefuy :: 0xbbuy :: 0xbfuy :: _ -> Encoding.UTF8
        | _ when read < 4 -> def
        | 0uy :: 0uy :: 0xfeuy :: 0xffuy :: _ -> Encoding.UTF32
        | _ -> def


    /// Checks if the file exists on disk.
    /// 
    /// ## Parameters
    /// 
    ///  - `fileName` - The file name to check
    let exists fileName = File.Exists fileName

    /// Gets the encoding from the file or the default of the file doesn't exist
    /// 
    /// ## Parameters
    /// 
    ///  - `def` - The encoding to detect for
    ///  - `filename` - The file name to check
    let getEncodingOrDefault def filename =
        if not (exists filename) then def
        else getEncoding def filename

    /// Get the encoding from the file or utf8 without BOM if unknown or the file doesn't exist
    let getEncodingOrUtf8WithoutBom = getEncodingOrDefault utf8WithoutBom

    /// Raises an exception if the file doesn't exist on disk.
    /// 
    /// ## Parameters
    /// 
    ///  - `filename` - The file name to check
    let checkExists fileName =
        if not <| exists fileName then
            FileNotFoundException(sprintf "File %s does not exist." fileName) |> raise

    /// Checks if all given files exist.
    /// 
    /// ## Parameters
    /// 
    ///  - `files` - The files names to check
    let allExist files = Seq.forall File.Exists files

    /// Tries to get the version of a file. Throws `FileNotFoundException` if the file doesn't exist.
    /// Returns None if the file doesn't contain a `FileVersion` component.
    /// On non-windows platforms this API returns assembly metadata instead,
    /// see [FileVersionInfo.Unix.cs](https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28)
    ///
    /// ## Parameters
    ///
    ///  - 'fileName' - Name of file from which the version is retrieved. The path can be relative.
    let tryGetVersion (fileName : string) : string option =
        Path.getFullName fileName
        |> System.Diagnostics.FileVersionInfo.GetVersionInfo
        |> fun x -> if isNull x.FileVersion then None else Some x.FileVersion

    /// Get the version a file. This overload throws when the file has no version,
    /// consider using `tryGetVersion` instead. On non-windows platforms this API returns assembly metadata instead,
    /// [FileVersionInfo.Unix.cs](see https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28)
    ///
    /// ## Parameters
    ///
    ///  - 'fileName' - Name of file from which the version is retrieved. The path can be relative.
    let getVersion (fileName : string) : string =
        match tryGetVersion fileName with
        | Some v -> v
        | None -> raise <| System.InvalidOperationException(sprintf "The file '%s' doesn't contain a file version" fileName)

    /// Creates a file if it does not exist.
    /// 
    /// ## Parameters
    ///
    ///  - 'fileName' - The name of the file to create
    let create fileName =
        let file = FileInfo.ofPath fileName
        if not file.Exists then
            file.Create().Dispose()

    /// Deletes a file if it exists.
    /// 
    /// ## Parameters
    ///
    ///  - 'fileName' - The name of the file to delete
    let delete fileName =
        let file = FileInfo.ofPath fileName
        if file.Exists then
            file.Delete()

    /// Deletes the given files.
    /// 
    /// ## Parameters
    ///
    ///  - 'files' - The name of the files to delete
    let deleteAll files = Seq.iter delete files

    /// Active Pattern for determining file extension.
    /// 
    /// ## Parameters
    ///
    ///  - 'extension' - The extension to look for
    ///  - 'file' - The file name to use
    let (|EndsWith|_|) (extension : string) (file : string) =
        if file.EndsWith extension then Some()
        else None

    /// Reads a file line by line
    /// 
    /// ## Parameters
    ///
    ///  - 'encoding' - The encoding to use
    ///  - 'file' - The file name to use
    let readWithEncoding (encoding : Encoding) (file : string) =
        seq {
            use stream = File.OpenRead(file)
            use textReader = new StreamReader(stream, encoding)
            while not textReader.EndOfStream do
                yield textReader.ReadLine()
        }
    let read (file : string) = readWithEncoding (getEncodingOrUtf8WithoutBom file) file

    /// Reads the first line of a file. This can be helpful to read a password from file.
    /// 
    /// ## Parameters
    ///
    ///  - 'encoding' - The encoding to use
    ///  - 'file' - The file name to use
    let readLineWithEncoding (encoding:Encoding) (file : string) =
        use stream = File.OpenRead file
        use sr = new StreamReader(stream, encoding)
        sr.ReadLine()

    /// Reads the first line of a file. This can be helpful to read a password from file.
    /// 
    /// ## Parameters
    ///
    ///  - 'file' - The file name to use
    let readLine(file : string) = readLineWithEncoding (getEncodingOrUtf8WithoutBom file) file

    /// Writes a file line by line
    /// 
    /// ## Parameters
    ///
    ///  - 'encoding' - The encoding to use
    ///  - 'append' - Flag to check if to append content or overwrite
    ///  - 'filename' - The file name to use
    ///  - 'lines' - The lines to write
    let writeWithEncoding (encoding:Encoding) append fileName (lines : seq<string>) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        lines |> Seq.iter writer.WriteLine

    /// Write the given sequence of lines to the file. Either append to the end of the file or overwrite
    /// 
    /// ## Parameters
    ///
    ///  - 'append' - Flag to check if to append content or overwrite
    ///  - 'filename' - The file name to use
    ///  - 'lines' - The lines to write
    let write append fileName (lines : seq<string>) = writeWithEncoding (getEncodingOrUtf8WithoutBom fileName) append fileName lines

    /// Writes a byte array to a file
    /// 
    /// ## Parameters
    ///
    ///  - 'file' - The file name to use
    ///  - 'bytes' - The bytes to write
    let writeBytes file bytes = File.WriteAllBytes(file, bytes)

    /// Writes a string to a file
    /// 
    /// ## Parameters
    ///
    ///  - 'encoding' - The encoding to use
    ///  - 'append' - Flag to check if to append content or overwrite
    ///  - 'filename' - The file name to use
    ///  - 'text' - The string text to write
    let writeStringWithEncoding (encoding:Encoding) append fileName (text : string) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        ( use writer = new StreamWriter(file, encoding)
          writer.Write text)
        file.Close()

    /// Writes a string to a file
    /// 
    /// ## Parameters
    ///
    ///  - 'append' - Flag to check if to append content or overwrite
    ///  - 'filename' - The file name to use
    ///  - 'text' - The string text to write
    let writeString append fileName (text : string) = writeStringWithEncoding (getEncodingOrUtf8WithoutBom fileName) append fileName text

    /// Replaces the file with the given string
    /// 
    /// ## Parameters
    ///
    ///  - 'filename' - The file name to use
    ///  - 'text' - The string text to write
    let replaceContent fileName text =
        let fi = FileInfo.ofPath fileName
        if fi.Exists then
            fi.IsReadOnly <- false
            fi.Delete()
        writeString false fileName text

    /// Writes a file line by line
    /// 
    /// ## Parameters
    ///
    ///  - 'file' - The file name to use
    ///  - 'lines' - The lines to write
    let writeNew file lines = write false file lines

    /// Appends all lines to a file line by line
    /// 
    /// ## Parameters
    ///
    ///  - 'file' - The file name to use
    ///  - 'lines' - The lines to append
    let append file lines = write true file lines

    /// Reads a file as one text
    /// 
    /// ## Parameters
    ///
    ///  - 'encoding' - The encoding to use
    ///  - 'file' - The file name to use
    let inline readAsStringWithEncoding encoding file = File.ReadAllText(file, encoding)
    
    /// Reads a file as one text
    /// 
    /// ## Parameters
    ///
    ///  - 'file' - The file name to use
    let inline readAsString file = File.ReadAllText(file, (getEncodingOrUtf8WithoutBom file))

    /// Reads a file as one array of bytes
    /// 
    /// ## Parameters
    ///
    ///  - 'file' - The file name to use
    let readAsBytes file = File.ReadAllBytes file

    /// Replaces the text in the given file
    /// 
    /// ## Parameters
    ///
    ///  - 'replaceF' - The callback to execute when replacing content
    ///  - 'fileName' - The file name to use
    let applyReplace replaceF fileName =
        fileName
        |> readAsString
        |> replaceF
        |> replaceContent fileName

