/// Contains helpers which allow to interact with the file system.
namespace Fake.IO

open System.Text
open System.IO
open Fake.Core
open Operators

[<RequireQualifiedAccess>]
module FileFilter =
    let allFiles _ = true

[<RequireQualifiedAccess>]
module File =
    /// see https://stackoverflow.com/questions/2502990/create-text-file-without-bom
    let internal utf8WithoutBom = new UTF8Encoding(false)

    // Detect the encoding, from https://stackoverflow.com/questions/3825390/effective-way-to-find-any-files-encoding
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
        | 0x2buy :: 0x2fuy :: 0x76uy :: _ -> Encoding.UTF7
        | 0xefuy :: 0xbbuy :: 0xbfuy :: _ -> Encoding.UTF8
        | _ when read < 4 -> def
        | 0uy :: 0uy :: 0xfeuy :: 0xffuy :: _ -> Encoding.UTF32
        | _ -> def


    /// Checks if the file exists on disk.
    let exists fileName = File.Exists fileName

    /// Gets the encoding from the file or the default of the file doesn't exist
    let getEncodingOrDefault def filename =
        if not (exists filename) then def
        else getEncoding def filename

    /// Get the encoding from the file or utf8 without BOM if unknown or the file doesn't exist
    let getEncodingOrUtf8WithoutBom = getEncodingOrDefault utf8WithoutBom

    /// Raises an exception if the file doesn't exist on disk.
    let checkExists fileName =
        if not <| exists fileName then
            FileNotFoundException(sprintf "File %s does not exist." fileName) |> raise

    /// Checks if all given files exist.
    let allExist files = Seq.forall File.Exists files

    /// Tries to get the version a file. Throws FileNotFoundException if the file doesn't exist.
    /// Returns None if the file doesn't contain a FileVersion component.
    /// On non-windows platforms this API returns assembly metadata instead, see https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28
    /// ## Parameters
    ///
    ///  - 'fileName' - Name of file from which the version is retrieved. The path can be relative.
    let tryGetVersion (fileName : string) : string option =
        Path.getFullName fileName
        |> System.Diagnostics.FileVersionInfo.GetVersionInfo
        |> fun x -> if isNull x.FileVersion then None else Some x.FileVersion

    /// Get the version a file. This overload throws when the file has no version, consider using tryGetVersion instead.
    /// On non-windows platforms this API returns assembly metadata instead, see https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28
    /// ## Parameters
    ///
    ///  - 'fileName' - Name of file from which the version is retrieved. The path can be relative.
    let getVersion (fileName : string) : string =
        match tryGetVersion fileName with
        | Some v -> v
        | None -> raise <| System.InvalidOperationException(sprintf "The file '%s' doesn't contain a file version" fileName)

    /// Creates a file if it does not exist.
    let create fileName =
        let file = FileInfo.ofPath fileName
        if not file.Exists then
            file.Create().Dispose()

    /// Deletes a file if it exists.
    let delete fileName =
        let file = FileInfo.ofPath fileName
        if file.Exists then
            file.Delete()

    /// Deletes the given files.
    let deleteAll files = Seq.iter delete files

    /// Active Pattern for determining file extension.
    let (|EndsWith|_|) (extension : string) (file : string) =
        if file.EndsWith extension then Some()
        else None

    /// Reads a file line by line
    let readWithEncoding (encoding : Encoding) (file : string) =
        seq {
            use stream = File.OpenRead(file)
            use textReader = new StreamReader(stream, encoding)
            while not textReader.EndOfStream do
                yield textReader.ReadLine()
        }
    let read (file : string) = readWithEncoding (getEncodingOrUtf8WithoutBom file) file

    /// Reads the first line of a file. This can be helpful to read a password from file.
    let readLineWithEncoding (encoding:Encoding) (file : string) =
        use stream = File.OpenRead file
        use sr = new StreamReader(stream, encoding)
        sr.ReadLine()

    /// Reads the first line of a file. This can be helpful to read a password from file.
    let readLine(file : string) = readLineWithEncoding (getEncodingOrUtf8WithoutBom file) file

    /// Writes a file line by line
    let writeWithEncoding (encoding:Encoding) append fileName (lines : seq<string>) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        lines |> Seq.iter writer.WriteLine

    let write append fileName (lines : seq<string>) = writeWithEncoding (getEncodingOrUtf8WithoutBom fileName) append fileName lines

    /// Writes a byte array to a file
    let writeBytes file bytes = File.WriteAllBytes(file, bytes)

    /// Writes a string to a file
    let writeStringWithEncoding (encoding:Encoding) append fileName (text : string) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        ( use writer = new StreamWriter(file, encoding)
          writer.Write text)
        file.Close()

    let writeString append fileName (text : string) = writeStringWithEncoding (getEncodingOrUtf8WithoutBom fileName) append fileName text

    /// Replaces the file with the given string
    let replaceContent fileName text =
        let fi = FileInfo.ofPath fileName
        if fi.Exists then
            fi.IsReadOnly <- false
            fi.Delete()
        writeString false fileName text

    /// Writes a file line by line
    let writeNew file lines = write false file lines

    /// Appends all lines to a file line by line
    let append file lines = write true file lines

    /// Reads a file as one text
    let inline readAsStringWithEncoding encoding file = File.ReadAllText(file, encoding)
    let inline readAsString file = File.ReadAllText(file, (getEncodingOrUtf8WithoutBom file))

    /// Reads a file as one array of bytes
    let readAsBytes file = File.ReadAllBytes file

    /// Replaces the text in the given file
    let applyReplace replaceF fileName =
        fileName
        |> readAsString
        |> replaceF
        |> replaceContent fileName

