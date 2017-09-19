/// Contains helpers which allow to interact with the file system.
namespace Fake.IO.FileSystem

open System.Text
open System.IO
open Fake.Core
open Operators

module File =
    /// Raises an exception if the file doesn't exist on disk.
    let checkFileExists fileName = 
        if not <| File.Exists fileName then new FileNotFoundException(sprintf "File %s does not exist." fileName) |> raise

    /// Checks if all given files exist.
    let allFilesExist files = Seq.forall File.Exists files

    let isFile path = Path.isFile path
    
    /// Get the version a file.
    /// ## Parameters
    ///
    ///  - 'fileName' - Name of file from which the version is retrieved. The path can be relative.
    let getVersion (fileName : string) = 
        Path.getFullName fileName
        |> System.Diagnostics.FileVersionInfo.GetVersionInfo
        |> fun x -> x.FileVersion.ToString()

    /// Creates a file if it does not exist.
    let CreateFile fileName = 
        let file = FileInfo.ofPath fileName
        if not file.Exists then 
            //TODO: logfn "Creating %s" file.FullName
            use newFile = file.Create()
            ()
        else () //TODO: logfn "%s already exists." file.FullName

    /// Deletes a file if it exists.
    let DeleteFile fileName = 
        let file = FileInfo.ofPath fileName
        if file.Exists then 
            //TODO: logfn "Deleting %s" file.FullName
            file.Delete()
        else () // TODO: logfn "%s does not exist." file.FullName

    /// Deletes the given files.
    let DeleteFiles files = Seq.iter DeleteFile files

    /// Active Pattern for determining file extension.
    let (|EndsWith|_|) extension (file : string) = 
        if file.EndsWith extension then Some()
        else None
        
    /// Reads a file line by line
    let ReadWithEncoding (encoding : Encoding) (file : string) = 
        seq {
            use stream = File.OpenRead(file)
            use textReader = new StreamReader(stream, encoding)
            while not textReader.EndOfStream do
                yield textReader.ReadLine()
        }
    let Read (file : string) = ReadWithEncoding (Encoding.UTF8) file
    
    /// Reads the first line of a file. This can be helpful to read a password from file.
    let ReadLineWithEncoding (encoding:Encoding) (file : string) =
        use stream = File.OpenRead file
        use sr = new StreamReader(stream, encoding)
        sr.ReadLine()

    /// Reads the first line of a file. This can be helpful to read a password from file.
    let ReadLine(file : string) = ReadLineWithEncoding Encoding.UTF8 file

    /// Writes a file line by line
    let WriteToFileWithEncoding (encoding:Encoding) append fileName (lines : seq<string>) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        lines |> Seq.iter writer.WriteLine

    let WriteToFile append fileName (lines : seq<string>) =  WriteToFileWithEncoding Encoding.UTF8 append fileName lines

    let Write file lines = WriteToFile false file lines
    let Append file lines = WriteToFile true file lines

    /// Writes a byte array to a file
    let WriteBytesToFile file bytes = File.WriteAllBytes(file, bytes)

    /// Writes a string to a file
    let WriteStringToFileWithEncoding (encoding:Encoding) append fileName (text : string) = 
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        writer.Write text

    let WriteStringToFile append fileName (text : string) = WriteStringToFileWithEncoding Encoding.UTF8 append fileName text

    /// Replaces the file with the given string
    let ReplaceFile fileName text = 
        let fi = FileInfo.ofPath fileName
        if fi.Exists then 
            fi.IsReadOnly <- false
            fi.Delete()
        WriteStringToFile false fileName text

    /// Writes a file line by line
    let WriteFile file lines = WriteToFile false file lines

    /// Appends all lines to a file line by line
    let AppendToFile file lines = WriteToFile true file lines

    /// Reads a file as one text
    let inline ReadFileAsStringWithEncoding encoding file = File.ReadAllText(file, encoding)
    let inline ReadFileAsString file = File.ReadAllText(file, Encoding.UTF8)

    /// Reads a file as one array of bytes
    let ReadFileAsBytes file = File.ReadAllBytes file

    /// Replaces the text in the given file
    let ReplaceInFile replaceF fileName = 
        fileName
        |> ReadFileAsString
        |> replaceF
        |> ReplaceFile fileName

