/// Contains helpers which allow to interact with the file system.
namespace Fake.IO.FileSystem

open System.Text
open System.IO
open Fake.Core

module Operators =
    /// Combines two path strings using Path.Combine
    let inline (@@) path1 path2 = Path.combineTrimEnd path1 path2
    /// Combines two path strings using Path.Combine
    let inline (</>) path1 path2 = Path.combine path1 path2

open Operators

module FileInfo =
    /// Creates a FileInfo for the given path.
    let inline ofPath path = new FileInfo(path)
    
    /// Active Pattern for determining file name.
    let (|FileInfoFullName|) (f : FileInfo) = f.FullName

    /// Active Pattern for determining FileInfoNameSections.
    let (|FileInfoNameSections|) (f : FileInfo) = (f.Name, f.Extension, f.FullName)
    
    /// Checks if the two files are byte-to-byte equal.
    let contentIsEqualTo (first : FileInfo) (second : FileInfo) = 
        if first.Length <> second.Length then false
        else 
            let BYTES_TO_READ = 32768
            use fs1 = first.OpenRead()
            use fs2 = second.OpenRead()
            let one = Array.create BYTES_TO_READ (byte 0)
            let two = Array.create BYTES_TO_READ (byte 0)
            let mutable eq = true
            while eq && fs1.Read(one, 0, BYTES_TO_READ) <> 0 && fs2.Read(two, 0, BYTES_TO_READ) <> 0 do
                if one <> two then eq <- false
            eq
module DirectoryInfo =
    /// Creates a DirectoryInfo for the given path.
    let inline ofPath path = new DirectoryInfo(path)

    /// Gets all subdirectories of a given directory.
    let inline getDirectories (dir : DirectoryInfo) = dir.GetDirectories()

    /// Gets all files in the directory.
    let inline getFiles (dir : DirectoryInfo) = dir.GetFiles()

    /// Finds all the files in the directory matching the search pattern.
    let getMatchingFiles pattern (dir : DirectoryInfo) = 
        if dir.Exists then dir.GetFiles pattern
        else [||]
        
    /// Checks if dir1 is a subfolder of dir2. If dir1 equals dir2 the function returns also true.
    let rec isSubfolderOf (dir2 : DirectoryInfo) (dir1 : DirectoryInfo) = 
        if Path.normalizeFileName dir1.FullName = Path.normalizeFileName dir2.FullName then true
        else if isNull dir1.Parent then false
        else dir1.Parent |> isSubfolderOf dir2
        
    /// Checks if the file is in a subfolder of the dir.
    let containsFile (fileInfo : FileInfo) (dir : DirectoryInfo) = isSubfolderOf dir fileInfo.Directory
    
    /// Checks if the directory exists on disk.
    let exists (dir : DirectoryInfo) = dir.Exists
    

    /// Ensure that directory chain exists. Create necessary directories if necessary.
    let inline ensure (dir : DirectoryInfo) =
        if not dir.Exists then dir.Create()

 
    /// Performs the given actions on all files and subdirectories
    let rec recursively dirF fileF (dir : DirectoryInfo) = 
        dir
        |> getDirectories
        |> Seq.iter (fun dir -> 
               recursively dirF fileF dir
               dirF dir)
        dir
        |> getFiles
        |> Seq.iter fileF

    /// Sets the directory readonly 
    let setDirectoryReadOnly readOnly (dir : DirectoryInfo) = 
        if dir.Exists then 
            let isReadOnly = dir.Attributes &&& FileAttributes.ReadOnly = FileAttributes.ReadOnly
            if readOnly && (not isReadOnly) then dir.Attributes <- dir.Attributes ||| FileAttributes.ReadOnly
            if (not readOnly) && not isReadOnly then dir.Attributes <- dir.Attributes &&& (~~~FileAttributes.ReadOnly)

    /// Sets all files in the directory readonly.
    let SetDirReadOnly readOnly dir = 
        recursively (setDirectoryReadOnly readOnly) (fun file -> file.IsReadOnly <- readOnly) dir
    
    /// Copies the file structure recursively.
    let rec copyRecursiveTo overwrite (outputDir : DirectoryInfo) (dir : DirectoryInfo)  = 
        let files = 
            dir
            |> getDirectories
            |> Seq.fold (fun acc (d : DirectoryInfo) -> 
                   let newDir = outputDir.FullName @@ d.Name
                                |> ofPath
                   ensure newDir
                   d
                   |> copyRecursiveTo overwrite newDir
                   |> fun r -> r @ acc) []
        (dir
         |> getFiles
         |> Seq.map (fun f -> 
                let newFileName = outputDir.FullName @@ f.Name
                f.CopyTo(newFileName, overwrite) |> ignore
                newFileName)
         |> Seq.toList) @ files

    let copyRecursive (dir : DirectoryInfo) (outputDir : DirectoryInfo) overwrite = dir |> copyRecursiveTo overwrite outputDir

module FileSystemInfo =
    /// Creates a FileInfo or a DirectoryInfo for the given path
    let inline ofPath path : FileSystemInfo = 
        if Directory.Exists path then upcast DirectoryInfo.ofPath path
        else upcast FileInfo.ofPath path
    
    /// Sets all given files or directories readonly.
    let SetReadOnly readOnly (items : string seq) = 
        items |> Seq.iter (fun item ->
            let fi = FileInfo.ofPath item
            if fi.Exists then fi.IsReadOnly <- readOnly
            else 
                item
                |> DirectoryInfo.ofPath
                |> DirectoryInfo.setDirectoryReadOnly readOnly)

    /// Active pattern which discriminates between files and directories.
    let (|File|Directory|) (fileSysInfo : FileSystemInfo) = 
        match fileSysInfo with
        | :? FileInfo as file -> File(file)
        | :? DirectoryInfo as dir -> Directory(dir, dir.EnumerateFileSystemInfos())
        | _ -> failwith "No file or directory given."

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

module Directory =

    /// Creates a directory if it does not exist.
    let CreateDir path = 
        let dir = DirectoryInfo.ofPath path
        if not dir.Exists then 
            // TODO: logfn "Creating %s" dir.FullName
            dir.Create()
        else () //TODO: logfn "%s already exists." dir.FullName

    /// Checks if the given directory exists. If not then this functions creates the directory.
    let inline ensure dir =
        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore
            
    let isDirectory path = Path.isDirectory path

    /// Gets the first file in the directory matching the search pattern as an option value.
    let tryFindFirstMatchingFile pattern dir = 
        dir
        |> DirectoryInfo.ofPath
        |> DirectoryInfo.getMatchingFiles pattern
        |> fun files -> 
            if Seq.isEmpty files then None
            else (Seq.head files).FullName |> Some

    /// Gets the first file in the directory matching the search pattern or throws an error if nothing was found.
    let findFirstMatchingFile pattern dir = 
        match tryFindFirstMatchingFile pattern dir with
        | Some x -> x
        | None -> new FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir) |> raise
        
    /// Deletes a directory if it exists (including all contained elements).
    let delete path = 
        let dir = DirectoryInfo.ofPath path
        if dir.Exists then 
            // set all files readonly = false
            DirectoryInfo.setDirectoryReadOnly false dir
            //!!"/**/*.*"
            //|> SetBaseDir dir.FullName
            //|> (SetReadOnly false)
            //logfn "Deleting %s" dir.FullName
            dir.Delete true
        else () //TODO: logfn "%s does not exist." dir.FullName

