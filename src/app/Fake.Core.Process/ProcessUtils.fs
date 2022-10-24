namespace Fake.Core

open Fake.IO
open Fake.IO.FileSystemOperators

/// <summary>
/// Contains functions which can be used to start other tools.
/// </summary>
[<RequireQualifiedAccess>]
module ProcessUtils =

    /// <summary>
    /// Searches the given directories for all occurrences of the given file name
    /// </summary>
    let private findFilesInternal dirs file =
        let files =
            dirs
            |> Seq.choose (fun (path: string) ->
                let replacedPath =
                    path
                    |> String.replace "[ProgramFiles]" Environment.ProgramFiles
                    |> String.replace "[ProgramFilesX86]" Environment.ProgramFilesX86
                    |> String.replace "[SystemRoot]" Environment.SystemRoot

                try
                    if not (System.IO.Directory.Exists replacedPath) then
                        None
                    else
                        let filePath = replacedPath </> file
                        if File.exists filePath then Some filePath else None
                with e ->
                    raise
                    <| exn (
                        sprintf
                            "Error while trying to find files like '%s' in path '%s' (replaced '%s'). Please report this issue to FAKE and reference https://github.com/fsharp/FAKE/issues/2136."
                            file
                            path
                            replacedPath,
                        e
                    ))
            |> Seq.cache

        files

    /// <summary>
    /// Searches the given directories for all occurrences of the given file name, on windows PATHEXT is considered
    /// (and preferred when searching)
    /// </summary>
    ///
    /// <param name="dirs">The directories list</param>
    /// <param name="tool">The file name (tool) to search for</param>
    let findFiles dirs tool =
        // See https://unix.stackexchange.com/questions/280528/is-there-a-unix-equivalent-of-the-windows-environment-variable-pathext
        if Environment.isWindows then
            // Prefer PATHEXT, see https://github.com/fsharp/FAKE/issues/1911
            // and https://github.com/fsharp/FAKE/issues/1899
            Environment.environVarOrDefault "PATHEXT" ".COM;.EXE;.BAT"
            |> String.split ';'
            |> Seq.collect (fun postFix -> findFilesInternal dirs (tool + postFix))
            |> fun findings -> Seq.append findings (findFilesInternal dirs tool)
        else
            // On unix we still want to find some extensions (paket.exe!), but we prefer without
            // filesystem is case sensitive!
            Environment.environVarOrDefault "PATHEXT" ".exe;.sh"
            |> String.split ';'
            |> Seq.collect (fun postFix -> findFilesInternal dirs (tool + postFix))
            |> fun findings -> Seq.append (findFilesInternal dirs tool) findings

    /// <summary>
    /// Searches the given directories for all occurrences of the given file name. Considers PATHEXT on Windows.
    /// </summary>
    ///
    /// <param name="dirs">The directories list</param>
    /// <param name="tool">The file name (tool) to search for</param>
    let tryFindFile dirs tool =
        let files = findFiles dirs tool

        if not (Seq.isEmpty files) then
            Some(Seq.head files)
        else
            None

    /// <summary>
    /// Searches the given directories for the given file, failing if not found. Considers PATHEXT on Windows.
    /// </summary>
    ///
    /// <param name="dirs">The directories list</param>
    /// <param name="tool">The file name (tool) to search for</param>
    let findFile dirs tool =
        match tryFindFile dirs tool with
        | Some found -> found
        | None -> failwithf "%s not found in %A." tool dirs

    let private getCurrentAndPathDirs () =
        Environment.pathDirectories |> Seq.filter Path.isValidPath |> Seq.append [ "." ]

    /// <summary>
    /// Searches the current directory and in PATH for the given file and returns the result ordered by precedence.
    /// Considers PATHEXT on Windows.
    /// </summary>
    ///
    /// <param name="tool">The file name (tool) to search for</param>
    let findFilesOnPath (tool: string) : string seq =
        getCurrentAndPathDirs () |> fun dirs -> findFiles dirs tool

    /// <summary>
    /// Searches the current directory and the directories within the PATH
    /// environment variable for the given file. If successful returns the full
    /// path to the file. Considers PATHEXT on Windows.
    /// </summary>
    ///
    /// <param name="file">The file to search for</param>
    let tryFindFileOnPath (tool: string) : string option = findFilesOnPath tool |> Seq.tryHead

    /// <summary>
    /// Tries to find the tool via Env-Var. If no path has the right tool we are trying the PATH system variable.
    /// Considers PATHEXT on Windows.
    /// </summary>
    ///
    /// <param name="envVar">The environment variable name</param>
    /// <param name="tool">The file name (tool) to search for</param>
    let tryFindTool envVar tool =
        match Environment.environVarOrNone envVar with
        | Some path -> Some path
        | None -> tryFindFileOnPath tool

    /// <summary>
    /// Tries to find the tool via given directories. If no path has the right tool we are trying the current
    /// directory and the PATH system variable. Considers PATHEXT on Windows.
    /// </summary>
    ///
    /// <param name="additionalDirs">The list of directories to consider in search</param>
    /// <param name="tool">The file name (tool) to search for</param>
    let tryFindPath additionalDirs tool =
        Seq.append additionalDirs (getCurrentAndPathDirs ())
        |> fun dirs -> findFiles dirs tool
        |> Seq.tryHead

    /// <summary>
    /// Tries to find the tool via Env-Var. If no path has the right tool we are trying the PATH system variable.
    /// Considers PATHEXT on Windows.
    /// </summary>
    /// [omit]
    let findPath fallbackValue tool =
        match tryFindPath fallbackValue tool with
        | Some file -> file
        | None -> tool

    /// <summary>
    /// Walks directories via breadth first search (BFS)
    /// </summary>
    let private walkDirectories dirs =
        let rec enumerateDirs dirs =
            let subDirs =
                dirs
                |> Seq.collect (fun dir ->
                    try
                        if not (System.IO.Directory.Exists dir) then
                            Seq.empty
                        else
                            System.IO.Directory.EnumerateDirectories dir
                    with
                    | :? System.IO.DirectoryNotFoundException -> Seq.empty
                    | :? System.IO.IOException
                    | :? System.Security.SecurityException
                    | :? System.UnauthorizedAccessException as e ->
                        if Trace.isVerbose (true) then
                            Trace.traceErrorfn "Ignoring directory listing of '%s', due to %s" dir e.Message
                        else
                            Trace.traceErrorfn "Ignoring directory listing of '%s', due to %O" dir e

                        Seq.empty)
                |> Seq.cache

            if Seq.isEmpty subDirs then
                Seq.empty
            else
                seq {
                    yield! subDirs
                    yield! subDirs |> enumerateDirs
                }

        seq {
            yield! dirs
            yield! enumerateDirs dirs
        }

    /// <summary>
    /// Find a local tool in the given envar the given directories, the current directory or PATH (in this order)
    /// Recommended usage <c>tryFindLocalTool "TOOL" "tool" [ "." ]</c>
    /// </summary>
    ///
    /// <param name="envVar">The environment variable name</param>
    /// <param name="tool">The file name (tool) to search for</param>
    /// <param name="recursiveDirs">The list of directories to use</param>
    let tryFindLocalTool envVar tool recursiveDirs =
        let envDir =
            match Environment.environVarOrNone envVar with
            | Some path when File.exists path -> [ System.IO.Path.GetDirectoryName path ]
            | Some path when System.IO.Directory.Exists path -> [ path ]
            | _ -> []

        let dirs =
            getCurrentAndPathDirs ()
            |> Seq.append (walkDirectories recursiveDirs)
            |> Seq.append envDir

        findFiles dirs tool |> Seq.tryHead

    /// <summary>
    /// Like <c>tryFindLocalTool</c> but returns the <c>tool</c> string if nothing is found (will probably error
    /// later, but this function is OK to be used for fake default values.
    /// </summary>
    ///
    /// <param name="envVar">The environment variable name</param>
    /// <param name="tool">The file name (tool) to search for</param>
    /// <param name="recursiveDirs">The list of directories to use</param>
    let findLocalTool envVar tool recursiveDirs =
        match tryFindLocalTool envVar tool recursiveDirs with
        | Some p -> p
        | None -> tool
