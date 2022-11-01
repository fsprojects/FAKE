namespace Fake.IO

/// <summary>
/// Contains helper function which allow to deal with files and directories.
/// </summary>
[<RequireQualifiedAccess>]
module Path =

    open Fake.Core
    open Fake.Core.String.Operators
    open System
    open System.IO
    open System.Collections.Concurrent

    /// <summary>
    /// Combines two path strings using Path.Combine. Trims leading slashes of path2.
    /// This makes <c>combineTrimEnd "/test" "/sub"</c> return <c>/test/sub</c>
    /// </summary>
    ///
    /// <param name="path1">The first path to combine</param>
    /// <param name="path2">The second path to combine</param>
    let inline combineTrimEnd path1 (path2: string) =
        Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])

    /// <summary>
    /// Combines two path strings using Path.Combine
    /// </summary>
    ///
    /// <param name="path1">The first path to combine</param>
    /// <param name="path2">The second path to combine</param>
    let inline combine path1 path2 = Path.Combine(path1, path2)

    /// <summary>
    /// Detects whether the given path is a directory.
    /// </summary>
    ///
    /// <param name="path">The path to check</param>
    let isDirectory path =
        let attr = File.GetAttributes path
        attr &&& FileAttributes.Directory = FileAttributes.Directory

    /// <summary>
    /// Detects whether the given path is a file.
    /// </summary>
    ///
    /// <param name="path">The path to check</param>
    let isFile path = isDirectory path |> not

    /// <summary>
    /// Normalizes a filename.
    /// </summary>
    ///
    /// <param name="fileName">The file name to normalize</param>
    let normalizeFileName (fileName: string) =
        let dirSepChar = Path.DirectorySeparatorChar
        let dirSep = dirSepChar.ToString()

        fileName
            .Replace("\\", dirSep)
            .Replace("/", dirSep)
            .TrimEnd(dirSepChar)
            .ToLower()

    /// <summary>
    /// Detects whether the given path does not contains invalid characters.
    /// </summary>Detects whether the given path does not contains invalid characters.
    ///
    /// <param name="fileName">The path to operate on</param>
    let isValidPath (path: string) =
        Path.GetInvalidPathChars()
        |> Array.exists (fun char -> path.Contains(char.ToString()))
        |> not

    /// <summary>
    /// Change the extension of the file.
    /// </summary>
    ///
    /// <param name="extension">The new extension containing the leading '.'.</param>
    /// <param name="fileName">Name of the file from which the extension is retrieved.</param>
    let changeExtension extension fileName =
        Path.ChangeExtension(fileName, extension)

    /// <summary>
    /// Tests whether the file has specified extensions (containing the leading '.')
    /// </summary>
    ///
    /// <param name="extension">The extension to fine containing the leading '.'.</param>
    /// <param name="fileName">Name of the file from which the extension is retrieved.</param>
    let hasExtension extension (fileName: string) =
        String.Equals(Path.GetExtension fileName, extension, StringComparison.OrdinalIgnoreCase)

    /// <summary>
    /// Get the directory of the specified path
    /// </summary>
    ///
    /// <param name="path">The path from which the directory is retrieved.</param>
    let getDirectory (path: string) = Path.GetDirectoryName path

    /// <summary>
    /// The directory separator string. On most systems <c>/</c> or <c>\</c>
    /// </summary>
    let directorySeparator =
        let dirSepChar = Path.DirectorySeparatorChar
        dirSepChar.ToString()

    /// <summary>
    /// Gets the absolute path for the given path
    /// </summary>
    ///
    /// <param name="p">The path to get its absolute path</param>
    let getFullName p = Path.GetFullPath p

    /// <summary>
    /// Replaces any occurence of the currentDirectory with "."
    /// </summary>
    ///
    /// <param name="path">The path to operate on</param>
    let inline shortenCurrentDirectory path =
        String.replace (Directory.GetCurrentDirectory()) "." path

    /// <summary>
    /// Produces relative path when possible to go from baseLocation to targetLocation.
    /// </summary>
    ///
    /// <param name="baseLocation">The root folder</param>
    /// <param name="targetLocation">The target folder</param>
    /// <returns>The relative path relative to baseLocation</returns>
    /// <exception cref="ArgumentNullException">base or target locations are null or empty</exception>
    let private ProduceRelativePath baseLocation targetLocation =
        if String.isNullOrEmpty baseLocation then
            raise (ArgumentNullException "baseLocation")

        if String.isNullOrEmpty targetLocation then
            raise (ArgumentNullException "targetLocation")

        if not <| Path.IsPathRooted baseLocation then
            baseLocation
        else if not <| Path.IsPathRooted targetLocation then
            targetLocation
        else if
            String.Compare(Path.GetPathRoot baseLocation, Path.GetPathRoot targetLocation, true)
            <> 0
        then
            targetLocation
        else if String.Compare(baseLocation, targetLocation, true) = 0 then
            "."
        else
            let resultPath = ref "."

            let targetLocation =
                if targetLocation |> String.endsWith directorySeparator then
                    targetLocation
                else
                    targetLocation + directorySeparator

            let baseLocation =
                if baseLocation |> String.endsWith directorySeparator then
                    ref (baseLocation.Substring(0, baseLocation.Length - 1))
                else
                    ref baseLocation

            while not
                  <| targetLocation.StartsWith(
                      baseLocation.Value + directorySeparator,
                      StringComparison.OrdinalIgnoreCase
                  ) do
                resultPath.Value <- resultPath.Value + directorySeparator + ".."
                baseLocation.Value <- Path.GetDirectoryName baseLocation.Value

                if baseLocation.Value |> String.endsWith directorySeparator then
                    baseLocation.Value <- baseLocation.Value.Substring(0, baseLocation.Value.Length - 1)

            resultPath.Value <-
                (resultPath.Value + targetLocation.Substring(baseLocation.Value.Length))
                |> String.replace (directorySeparator + directorySeparator) directorySeparator
            // preprocess .\..\ case
            if (sprintf ".%s..%s" directorySeparator directorySeparator) <* resultPath.Value then
                resultPath.Value.Substring(2, resultPath.Value.Length - 3)
            else
                resultPath.Value.Substring(0, resultPath.Value.Length - 1)

    /// <summary>
    /// Replaces the absolute path with a relative path
    /// </summary>
    let toRelativeFrom =
        /// A cache of relative path names.
        /// [omit]
        let relativePaths = ConcurrentDictionary<string * string, string>()

        /// Replaces the absolute path to a relative path.
        let inline toRelativePath basePath value =
            let key = (basePath, value)
            relativePaths.GetOrAdd(key, (fun _ -> ProduceRelativePath basePath value))

        toRelativePath

    /// <summary>
    /// Replaces the absolute path with a relative path
    /// </summary>
    ///
    /// <param name="path">The path to operate on</param>
    let toRelativeFromCurrent path =
        let currentDir = normalizeFileName <| Directory.GetCurrentDirectory()
        toRelativeFrom currentDir path

    /// <summary>
    /// Convert the given windows path to a path in the current system
    /// </summary>
    ///
    /// <param name="windowsPath">The path to operate on</param>
    let convertWindowsToCurrentPath (windowsPath: string) =
        if (windowsPath.Length > 2 && windowsPath[1] = ':' && windowsPath[2] = '\\') then
            windowsPath
        else
            windowsPath.Replace(@"\", directorySeparator)
