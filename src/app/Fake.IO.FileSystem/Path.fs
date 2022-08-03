namespace Fake.IO

/// Contains helper function which allow to deal with files and directories.
[<RequireQualifiedAccess>]
module Path =

    open Fake.Core
    open Fake.Core.String.Operators
    open System
    open System.IO
    open System.Collections.Concurrent

    /// Combines two path strings using Path.Combine. Trims leading slashes of path2.
    /// This makes `combineTrimEnd "/test" "/sub"` return `/test/sub`
    ///
    /// ## Parameters
    /// - `path1` - The first path to combine
    /// - `path2` - The second path to combine
    let inline combineTrimEnd path1 (path2: string) =
        Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])

    /// Combines two path strings using Path.Combine
    ///
    /// ## Parameters
    /// - `path1` - The first path to combine
    /// - `path2` - The second path to combine
    let inline combine path1 path2 = Path.Combine(path1, path2)

    /// Detects whether the given path is a directory.
    ///
    /// ## Parameters
    /// - `path` - The path to check
    let isDirectory path =
        let attr = File.GetAttributes path
        attr &&& FileAttributes.Directory = FileAttributes.Directory

    /// Detects whether the given path is a file.
    ///
    /// ## Parameters
    /// - `path` - The path to check
    let isFile path = isDirectory path |> not

    /// Normalizes a filename.
    ///
    /// ## Parameters
    /// - `fileName` - The file name to normalize
    let normalizeFileName (fileName: string) =
        let dirSepChar = Path.DirectorySeparatorChar
        let dirSep = dirSepChar.ToString()

        fileName
            .Replace("\\", dirSep)
            .Replace("/", dirSep)
            .TrimEnd(dirSepChar)
            .ToLower()

    /// Detects whether the given path does not contains invalid characters.
    ///
    /// ## Parameters
    /// - `fileName` - The path to operate on
    let isValidPath (path: string) =
        Path.GetInvalidPathChars()
        |> Array.exists (fun char -> path.Contains(char.ToString()))
        |> not

    /// Change the extension of the file.
    /// 
    /// ## Parameters
    /// - `extension` - The new extension containing the leading '.'.
    /// - `fileName` - Name of the file from which the extension is retrieved.
    let changeExtension extension fileName =
        Path.ChangeExtension(fileName, extension)

    /// Tests whether the file has specified extensions (containing the leading '.')
    /// 
    /// ## Parameters
    /// - `extension` - The extension to fine containing the leading '.'.
    /// - `fileName` - Name of the file from which the extension is retrieved.
    let hasExtension extension (fileName: string) =
        String.Equals(Path.GetExtension fileName, extension, StringComparison.OrdinalIgnoreCase)

    /// Get the directory of the specified path
    /// 
    /// ## Parameters
    /// - `path` - The path from which the directory is retrieved.
    let getDirectory (path: string) = Path.GetDirectoryName path

    /// The directory separator string. On most systems `/` or `\`
    let directorySeparator =
        let dirSepChar = Path.DirectorySeparatorChar
        dirSepChar.ToString()

    /// Gets the absolute path for the given path
    /// ## Parameters
    /// - `p` - The path to get its absolute path
    let getFullName p = Path.GetFullPath p

    /// Replaces any occurence of the currentDirectory with "."
    ///
    /// ## Parameters
    /// - `path` - The path to operate on
    let inline shortenCurrentDirectory path =
        String.replace (Directory.GetCurrentDirectory()) "." path

    /// <summary>Produces relative path when possible to go from baseLocation to targetLocation.</summary>
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
                  <| targetLocation.StartsWith(baseLocation.Value + directorySeparator, StringComparison.OrdinalIgnoreCase) do
                resultPath.Value <- resultPath.Value + directorySeparator + ".."
                baseLocation.Value <- Path.GetDirectoryName baseLocation.Value

                if baseLocation.Value |> String.endsWith directorySeparator then
                    baseLocation.Value <- baseLocation.Value.Substring(0, baseLocation.Value.Length - 1)

            resultPath.Value <- (resultPath.Value + targetLocation.Substring(baseLocation.Value.Length))
               |> String.replace (directorySeparator + directorySeparator) directorySeparator
            // preprocess .\..\ case
            if (sprintf ".%s..%s" directorySeparator directorySeparator) <* resultPath.Value then
                resultPath.Value.Substring(2, resultPath.Value.Length - 3)
            else
                resultPath.Value.Substring(0, resultPath.Value.Length - 1)

    /// Replaces the absolute path with a relative path
    let toRelativeFrom =
        /// A cache of relative path names.
        /// [omit]
        let relativePaths = ConcurrentDictionary<string * string, string>()

        /// Replaces the absolute path to a relative path.
        let inline toRelativePath basePath value =
            let key = (basePath, value)
            relativePaths.GetOrAdd(key, (fun _ -> ProduceRelativePath basePath value))

        toRelativePath

    /// Replaces the absolute path with a relative path
    /// 
    /// ## Parameters
    /// - `path` - The path to operate on
    let toRelativeFromCurrent path =
        let currentDir = normalizeFileName <| Directory.GetCurrentDirectory()
        toRelativeFrom currentDir path

    /// Convert the given windows path to a path in the current system
    ///
    /// ## Parameters
    /// - `windowsPath` - The path to operate on
    let convertWindowsToCurrentPath (windowsPath: string) =
        if (windowsPath.Length > 2 && windowsPath[1] = ':' && windowsPath[2] = '\\') then
            windowsPath
        else
            windowsPath.Replace(@"\", directorySeparator)
