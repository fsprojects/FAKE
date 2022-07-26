namespace Fake.IO

open System.IO

/// Contains helpers which allow to interact with the directory in file system.
[<RequireQualifiedAccess>]
module Directory =

    /// Checks if the given directory exists. If not then this functions creates the directory.
    ///
    /// ## Parameters
    /// 
    /// - `dir` - The directory to check
    let inline ensure dir =
        dir |> DirectoryInfo.ofPath |> DirectoryInfo.ensure

    /// Creates a directory if it does not exist.
    ///
    /// ## Parameters
    /// 
    /// - `dir` - The directory to check
    let create = ensure

    /// Gets the first file in the directory matching the search pattern as an option value.
    ///
    /// ## Parameters
    /// 
    /// - `pattern` - The glob pattern to use in matching
    /// - `dir` - The directory to check
    let tryFindFirstMatchingFile pattern dir = 
        dir
        |> DirectoryInfo.ofPath
        |> DirectoryInfo.getMatchingFiles pattern
        |> fun files -> 
            if Seq.isEmpty files then None
            else (Seq.head files).FullName |> Some

    /// Gets the first file in the directory matching the search pattern or throws an error if nothing was found.
    ///
    /// ## Parameters
    ///
    /// - `pattern` - The glob pattern to use in matching
    /// - `dir` - The directory to check
    let findFirstMatchingFile pattern dir = 
        match tryFindFirstMatchingFile pattern dir with
        | Some x -> x
        | None -> FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir) |> raise
        
    /// Deletes a directory if it exists (including all contained elements).
    ///
    /// ## Parameters
    ///
    /// - `path` - The path to delete
    let delete path = 
        let dir = DirectoryInfo.ofPath path
        if dir.Exists then 
            DirectoryInfo.setReadOnlyRecursive false dir
            dir.Delete true
