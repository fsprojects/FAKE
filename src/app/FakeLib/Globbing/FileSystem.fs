/// This module contains a file pattern globbing implementation.
[<AutoOpen>]
module Fake.FileSystem

open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions

/// Internal representation of a file set.
type FileIncludes = 
    { BaseDirectory : string
      Includes : string list
      Excludes : string list }
    
    /// Adds the given pattern to the file includes
    member this.And pattern = { this with Includes = this.Includes @ [ pattern ] }
    
    /// Ignores files with the given pattern
    member this.ButNot pattern = { this with Excludes = pattern :: this.Excludes }
    
    /// Sets a directory as BaseDirectory.
    member this.SetBaseDirectory(dir : string) = { this with BaseDirectory = dir.TrimEnd(Path.DirectorySeparatorChar) }
    
    /// Checks if a particular file is matched
    member this.IsMatch (path : string) =
        let fullDir pattern = 
            if Path.IsPathRooted(pattern) then
                pattern
            else
                System.IO.Path.Combine(this.BaseDirectory, pattern)

        let included = 
            this.Includes
            |> Seq.exists(fun fileInclude ->
                Globbing.isMatch (fullDir fileInclude) path
            )
        let excluded = 
            this.Excludes
            |> Seq.exists(fun fileExclude ->
                Globbing.isMatch (fullDir fileExclude) path
            )

        included && not excluded

    interface IEnumerable<string> with
        
        member this.GetEnumerator() = 
            let hashSet = HashSet()
            
            let excludes = 
                seq { 
                    for pattern in this.Excludes do
                        yield! Globbing.search this.BaseDirectory pattern
                }
                |> Set.ofSeq
            
            let files = 
                seq { 
                    for pattern in this.Includes do
                        yield! Globbing.search this.BaseDirectory pattern
                }
                |> Seq.filter (fun x -> not (Set.contains x excludes))
                |> Seq.filter (fun x -> hashSet.Add x)
            
            files.GetEnumerator()
        
        member this.GetEnumerator() = (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

let private defaultBaseDir = Path.GetFullPath "."

/// Include files
let Include x = 
    { BaseDirectory = defaultBaseDir
      Includes = [ x ]
      Excludes = [] }

/// Sets a directory as baseDirectory for fileIncludes. 
let SetBaseDir (dir : string) (fileIncludes : FileIncludes) = fileIncludes.SetBaseDirectory dir

/// Add Include operator
let inline (++) (x : FileIncludes) pattern = x.And pattern

/// Exclude operator
let inline (--) (x : FileIncludes) pattern = x.ButNot pattern

/// Includes a single pattern and scans the files - !! x = AllFilesMatching x
let inline (!!) x = Include x

let private couldNotMatch =
    failwithf "Could not find anything matching %s"

/// Include a single pattern, throwing an exception if there are no matches
let (!!+) glob =
    let files = !! glob
    if Seq.isEmpty files
    then couldNotMatch glob
    else files

let private tooManymatches =
    failwithf "Found %i files matching %s; expected exactly one"

/// Include a single pattern and return a single match, throwing an exception
/// otherwise if there are zero or multiple matches.
let (!!!) glob =
    let files = !!+ glob
    let count = Seq.length files
    if count > 1
    then tooManymatches count glob
    else Seq.head files

/// Looks for a tool first in its default path, if not found the in ./packages/ and then
/// in all subfolders of the root folder - returns the tool file name.
let findToolInSubPath toolname defaultPath =
    try
        let tools = !! (defaultPath @@ "/**/" @@ toolname)
        if  Seq.isEmpty tools then 
            let packages = !! ("./packages/**/" @@ toolname)
            if Seq.isEmpty packages then
                let root = !! ("./**/" @@ toolname)
                Seq.head root
            else
                Seq.head packages
        else
            Seq.head tools
    with
    | _ -> defaultPath @@ toolname

/// Looks for a tool in all subfolders - returns the folder where the tool was found.
let findToolFolderInSubPath toolname defaultPath =
    try
        let tools = !! ("./**/" @@ toolname)
        if Seq.isEmpty tools then defaultPath
        else 
            let fi = FileInfo (Seq.head tools)
            fi.Directory.FullName
    with
    | _ -> defaultPath
