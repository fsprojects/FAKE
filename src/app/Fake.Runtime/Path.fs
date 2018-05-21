/// Contains basic functions for string manipulation.

module Fake.Runtime.Path
open System
open System.IO

// Normalizes path for different OS
let inline normalizePath (path : string) = 
    path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)

let getCurrentDirectory () =
    System.IO.Directory.GetCurrentDirectory()

let private nugetDir = Path.GetFullPath(Paket.Constants.UserNuGetPackagesFolder).TrimEnd([|'/' ;'\\'|]) + "/"
let fixPathForCache scriptPath (s:string) =
    let norm = Path.GetFullPath s
    let scriptDir = Path.GetFullPath(Path.GetDirectoryName scriptPath)
    if norm.StartsWith(nugetDir, if Paket.Utils.isWindows then StringComparison.OrdinalIgnoreCase else StringComparison.Ordinal) then
      sprintf "nugetcache://%s" (norm.Substring (nugetDir.Length))
    else
      let scriptDir = Uri(Path.GetDirectoryName scriptDir + "/")
      let other = Uri(s)
      let rel = scriptDir.MakeRelativeUri(other)
      if rel.IsAbsoluteUri then rel.AbsoluteUri
      else
        sprintf "scriptpath:///%s" rel.OriginalString

let readPathFromCache scriptPath (s:string) =
    let uri = Uri(s)
    let scriptDir = Path.GetFullPath(Path.GetDirectoryName scriptPath)
    if uri.Scheme = "nugetcache" then
        let rel = uri.AbsolutePath.TrimStart [| '/'|]
        Path.Combine (nugetDir, rel)
    elif uri.Scheme = "scriptpath" then
        let rel = uri.OriginalString.Substring("scriptpath:///".Length)
        Path.Combine(Path.GetDirectoryName scriptDir, rel)
    else
        uri.AbsolutePath
