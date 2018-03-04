namespace Fake.IO

open System.IO

[<RequireQualifiedAccess>]
module Directory =

    /// Checks if the given directory exists. If not then this functions creates the directory.
    let inline ensure dir =
        dir |> DirectoryInfo.ofPath |> DirectoryInfo.ensure

    /// Creates a directory if it does not exist.
    let create = ensure

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
        | None -> FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir) |> raise
        
    /// Deletes a directory if it exists (including all contained elements).
    let delete path = 
        let dir = DirectoryInfo.ofPath path
        if dir.Exists then 
            DirectoryInfo.setReadOnlyRecursive false dir
            dir.Delete true
