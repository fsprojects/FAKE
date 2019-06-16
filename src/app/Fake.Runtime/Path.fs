/// Contains basic functions for string manipulation.

module Fake.Runtime.Path
open System
open System.IO

let internal isCaseInSensitive = Paket.Utils.isWindows

let internal normalizeFileName fileName =
    if isCaseInSensitive then
        // fixes https://github.com/fsharp/FAKE/issues/2314
        let dir = Path.GetDirectoryName fileName
        let name = Path.GetFileName fileName
        if String.IsNullOrEmpty dir then name.ToLowerInvariant()
        else Path.Combine(dir, name.ToLowerInvariant())
    else fileName

// Normalizes path for different OS
let inline normalizePath (path : string) = 
    path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)

let getCurrentDirectory () =
    System.IO.Directory.GetCurrentDirectory()

let private nugetDir = Path.GetFullPath(Paket.Constants.UserNuGetPackagesFolder).TrimEnd([|'/' ;'\\'|]) + string Path.DirectorySeparatorChar
let fixPathForCache scriptPath (s:string) =
    let norm = Path.GetFullPath s
    let scriptDir = Path.GetDirectoryName (Path.GetFullPath scriptPath) + "/"
    if norm.StartsWith(nugetDir, if isCaseInSensitive then StringComparison.OrdinalIgnoreCase else StringComparison.Ordinal) then
      sprintf "nugetcache:///%s" (norm.Substring(nugetDir.Length).Replace("\\", "/"))
    else
      let scriptDir = Uri(scriptDir)
      let other = Uri(norm)
      let rel = scriptDir.MakeRelativeUri(other)
      if rel.IsAbsoluteUri then rel.AbsoluteUri
      else
        sprintf "scriptpath:///%s" rel.OriginalString

let readPathFromCache scriptPath (s:string) =
    let uri = Uri(s)
    let scriptDir = Path.GetDirectoryName(Path.GetFullPath scriptPath)
    if uri.Scheme = "nugetcache" then
        let rel = uri.AbsolutePath.TrimStart [| '/'|]
        Path.Combine (nugetDir, rel)
    elif uri.Scheme = "scriptpath" then
        let rel = uri.OriginalString.Substring("scriptpath:///".Length)
        Path.Combine(scriptDir, rel)
    else
        uri.AbsolutePath
    |> Path.GetFullPath
