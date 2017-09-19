namespace Fake.IO.FileSystem

open System.IO

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

