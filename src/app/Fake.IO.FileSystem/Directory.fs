namespace Fake.IO

open System.IO

/// <summary>
/// Contains helpers which allow to interact with the directory in file system.
/// </summary>
[<RequireQualifiedAccess>]
module Directory =

    /// <summary>
    /// Checks if the given directory exists. If not then this functions creates the directory.
    /// </summary>
    ///
    /// <param name="dir">The directory to check</param>
    let inline ensure dir =
        dir |> DirectoryInfo.ofPath |> DirectoryInfo.ensure

    /// <summary>
    /// Creates a directory if it does not exist.
    /// </summary>
    ///
    /// <param name="dir">The directory to check</param>
    let create = ensure

    /// <summary>
    /// Gets the first file in the directory matching the search pattern as an option value.
    /// </summary>
    ///
    /// <param name="pattern">The glob pattern to use in matching</param>
    /// <param name="dir">The directory to check</param>
    let tryFindFirstMatchingFile pattern dir = 
        dir
        |> DirectoryInfo.ofPath
        |> DirectoryInfo.getMatchingFiles pattern
        |> fun files -> 
            if Seq.isEmpty files then None
            else (Seq.head files).FullName |> Some

    /// <summary>
    /// Gets the first file in the directory matching the search pattern or throws an error if nothing was found.
    /// </summary>
    ///
    /// <param name="pattern">The glob pattern to use in matching</param>
    /// <param name="dir">The directory to check</param>
    let findFirstMatchingFile pattern dir = 
        match tryFindFirstMatchingFile pattern dir with
        | Some x -> x
        | None -> FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir) |> raise
        
    /// <summary>
    /// Deletes a directory if it exists (including all contained elements).
    /// </summary>
    ///
    /// <param name="path">The path to delete</param>
    let delete path = 
        let dir = DirectoryInfo.ofPath path
        if dir.Exists then 
            DirectoryInfo.setReadOnlyRecursive false dir
            dir.Delete true
