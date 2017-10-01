/// Contains helpers which allow to interact with the file system.
namespace Fake.IO

open System.Text
open System.IO
open Fake.Core
open Operators

module FileFilter =
    let allFiles file = true

module File =
    /// Checks if the file exists on disk.
    let exists fileName = File.Exists fileName

    /// Raises an exception if the file doesn't exist on disk.
    let checkExists fileName = 
        if not <| exists fileName then 
            FileNotFoundException(sprintf "File %s does not exist." fileName) |> raise

    /// Checks if all given files exist.
    let allExist files = Seq.forall File.Exists files

    /// Get the version a file.
    /// ## Parameters
    ///
    ///  - 'fileName' - Name of file from which the version is retrieved. The path can be relative.
    let getVersion (fileName : string) = 
        Path.getFullName fileName
        |> System.Diagnostics.FileVersionInfo.GetVersionInfo
        |> fun x -> x.FileVersion.ToString()

    /// Creates a file if it does not exist.
    let create fileName = 
        let file = FileInfo.ofPath fileName
        if not file.Exists then 
            file.Create() |> ignore

    /// Deletes a file if it exists.
    let delete fileName = 
        let file = FileInfo.ofPath fileName
        if file.Exists then 
            file.Delete()

    /// Deletes the given files.
    let deleteAll files = Seq.iter delete files

    /// Active Pattern for determining file extension.
    let (|EndsWith|_|) extension (file : string) = 
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
    let read (file : string) = readWithEncoding (Encoding.UTF8) file
    
    /// Reads the first line of a file. This can be helpful to read a password from file.
    let readLineWithEncoding (encoding:Encoding) (file : string) =
        use stream = File.OpenRead file
        use sr = new StreamReader(stream, encoding)
        sr.ReadLine()

    /// Reads the first line of a file. This can be helpful to read a password from file.
    let readLine(file : string) = readLineWithEncoding Encoding.UTF8 file

    /// Writes a file line by line
    let writeWithEncoding (encoding:Encoding) append fileName (lines : seq<string>) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        lines |> Seq.iter writer.WriteLine

    let write append fileName (lines : seq<string>) = writeWithEncoding Encoding.UTF8 append fileName lines
        
    /// Writes a byte array to a file
    let writeBytes file bytes = File.WriteAllBytes(file, bytes)

    /// Writes a string to a file
    let writeStringWithEncoding (encoding:Encoding) append fileName (text : string) = 
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        writer.Write text

    let writeString append fileName (text : string) = writeStringWithEncoding Encoding.UTF8 append fileName text

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
    let inline readAsString file = File.ReadAllText(file, Encoding.UTF8)

    /// Reads a file as one array of bytes
    let readAsBytes file = File.ReadAllBytes file

    /// Replaces the text in the given file
    let applyReplace replaceF fileName = 
        fileName
        |> readAsString
        |> replaceF
        |> replaceContent fileName

