/// Contains functions which can be used to start other tools.

namespace Fake.Core

open Fake.IO
open Fake.IO.FileSystemOperators

[<RequireQualifiedAccess>]
module ProcessUtils =

    /// Searches the given directories for all occurrences of the given file name
    /// [omit]
    let private findFilesInternal dirs file = 
        let files = 
            dirs
            |> Seq.map (fun (path : string) -> 
                   let replacedPath = 
                       path
                       |> String.replace "[ProgramFiles]" Environment.ProgramFiles
                       |> String.replace "[ProgramFilesX86]" Environment.ProgramFilesX86
                       |> String.replace "[SystemRoot]" Environment.SystemRoot
                   try
                       let dir =
                           replacedPath   
                           |> DirectoryInfo.ofPath
                       if not dir.Exists then ""
                       else 
                           let fi = dir.FullName @@ file
                                    |> FileInfo.ofPath
                           if fi.Exists then fi.FullName
                           else ""
                   with e ->
                       raise <| exn(sprintf "Error while trying to find files like '%s' in path '%s' (replaced '%s'). Please report this issue to FAKE and reference https://github.com/fsharp/FAKE/issues/2136." file path replacedPath, e))
            |> Seq.filter ((<>) "")
            |> Seq.cache
        files

    /// Searches the given directories for all occurrences of the given file name, on windows PATHEXT is considered (and preferred when searching)
    let findFiles dirs file =
        // See https://unix.stackexchange.com/questions/280528/is-there-a-unix-equivalent-of-the-windows-environment-variable-pathext
        if Environment.isWindows then
            // Prefer PATHEXT, see https://github.com/fsharp/FAKE/issues/1911
            // and https://github.com/fsharp/FAKE/issues/1899
            Environment.environVarOrDefault "PATHEXT" ".COM;.EXE;.BAT"
            |> String.split ';'
            |> Seq.collect (fun postFix -> findFilesInternal dirs (file + postFix))
            |> fun findings -> Seq.append findings (findFilesInternal dirs file)
        else findFilesInternal dirs file

    /// Searches the given directories for all occurrences of the given file name
    /// [omit]
    let tryFindFile dirs file =
        let files = findFiles dirs file
        if not (Seq.isEmpty files) then Some(Seq.head files)
        else None

    /// Searches the given directories for the given file, failing if not found.
    /// [omit]
    let findFile dirs file = 
        match tryFindFile dirs file with
        | Some found -> found
        | None -> failwithf "%s not found in %A." file dirs

    /// Searches in PATH for the given file and returnes the result ordered by precendence
    let findFilesOnPath (file : string) : string seq =
        Environment.pathDirectories
        |> Seq.filter Path.isValidPath
        |> Seq.append [ "." ]
        |> fun dirs -> findFiles dirs file

    /// Searches the current directory and the directories within the PATH
    /// environment variable for the given file. If successful returns the full
    /// path to the file.
    /// ## Parameters
    ///  - `file` - The file to locate
    let tryFindFileOnPath (file : string) : string option =
        findFilesOnPath file |> Seq.tryHead


    /// Tries to find the tool via Env-Var. If no path has the right tool we are trying the PATH system variable.
    let tryFindTool envVar tool =
        match Environment.environVarOrNone envVar with
        | Some path -> Some path
        | None -> tryFindFileOnPath tool

    /// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
    /// [omit]
    let tryFindPath fallbackValue tool =
        match tryFindFile fallbackValue tool with
        | Some path -> Some path
        | None -> tryFindFileOnPath tool

    /// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
    /// [omit]
    let findPath fallbackValue tool = 
        match tryFindPath fallbackValue tool with
        | Some file -> file
        | None -> tool
