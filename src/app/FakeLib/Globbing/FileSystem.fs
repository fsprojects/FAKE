namespace Fake

// Copied from https://github.com/colinbull/FSharp.Enterprise/blob/master/src/FSharp.Enterprise/FileSystem.fs
module FileSystem = 
    
    open System
    open System.IO
    open System.Runtime.Caching

    let fullPathRelativeTo rootDir file = 
        match Path.IsPathRooted(file) with
        | true -> file
        | false -> 
             match rootDir with
             | Some(root) -> Path.Combine(root, file)
             | None -> Path.GetFullPath(file)
        |> fun x -> new FileInfo(x)

    let tryReadFile deserialiser (path:string) =
        if File.Exists(path)
        then Some (File.ReadAllText(path) |> deserialiser)
        else None

    let write serialiser (path:string,payload:'a) =
        File.WriteAllText(path, serialiser payload)
    
    [<AutoOpen>]
    module Search = 
        
        type T = {
            Recurse : bool
            Root : string
            FilePattern : string
        }

        let private cleanUpPath path = 
            if String.IsNullOrEmpty(path) 
            then "/" else path

        let private parse (str:string) =
            let fname = Path.GetFileName(str)
            let recursiveDir = str.Replace(fname, "").Trim([|'\\';'/'|])  
            let dir, filePattern =     
                let dir = recursiveDir.Replace("**", "") |> cleanUpPath
                if Directory.Exists(Path.GetDirectoryName(dir))
                then 
                    let fi = FileInfo(str)
                    if fi.Attributes = FileAttributes.Directory
                    then fi.FullName, "*.*" 
                    else fi.Directory.FullName, fname
                else dir, fname
            let isRecursive = recursiveDir.EndsWith("**")
            let tempPath = 
                if isRecursive then recursiveDir.Replace("**", "") else recursiveDir
                |> cleanUpPath
            let root = fullPathRelativeTo (Some <| Path.GetDirectoryName(Reflection.Assembly.GetEntryAssembly().Location)) tempPath
            let result =  { Recurse = isRecursive; Root = root.FullName; FilePattern = filePattern }
            result
           

        let DefaultSearcher (searchParams:T) =
            if Directory.Exists(searchParams.Root)
            then
                if searchParams.Recurse
                then Directory.EnumerateFileSystemEntries(searchParams.Root, searchParams.FilePattern, SearchOption.AllDirectories)
                else Directory.EnumerateFileSystemEntries(searchParams.Root, searchParams.FilePattern)
            else Seq.empty

        let findFiles searcher pattern  =
            parse pattern |> (defaultArg searcher DefaultSearcher)

    let FileIO serialise deserialise = 
        IO.IO
            (tryReadFile deserialise)
            (write serialise)  
            File.Delete 
            (findFiles None)

    let CachedFileIO serialise deserialise expiry = 
        IO.CachedIO 
            expiry
            (tryReadFile deserialise)
            (write serialise)  
            File.Delete 
            (findFiles None)
