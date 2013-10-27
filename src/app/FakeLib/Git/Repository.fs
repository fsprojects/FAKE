[<AutoOpen>]
/// Contains functions which allow basic operations on git repositories.
/// All operations assume that the CommandHelper can find git.exe.
module Fake.Git.Repository

open Fake
open System.IO

/// Clones a git repository
let clone workingDir repoUrl toPath =  gitCommand workingDir (sprintf "clone %s %s" repoUrl toPath)

/// Inits a git repository
let init repositoryDir bare shared =
    match bare, shared with
    | true, true -> gitCommand repositoryDir "init --bare --shared=all"
    | true, false -> gitCommand repositoryDir "init --bare"
    | false, true -> gitCommand repositoryDir "init --shared=all"
    | _ -> gitCommand repositoryDir "init"

/// Cleans a directory by removing all files and sub-directories.
/// ## Parameters
///
///  - `path` - The path of the directory to clean.
let fullclean repositoryDir =
    let di = directoryInfo repositoryDir
    if di.Exists then
        logfn "Deleting contents of %s" repositoryDir
        // delete all files
        Directory.GetFiles(repositoryDir, "*.*", SearchOption.TopDirectoryOnly)
          |> Seq.iter (fun file -> 
                let fi = fileInfo file
                fi.IsReadOnly <- false
                fi.Delete())
    
        // deletes all subdirectories
        let rec deleteDirs actDir =
            let di = directoryInfo actDir
            if di.Name = ".git" then () else
            try
                Directory.GetDirectories(actDir) |> Seq.iter deleteDirs
                Directory.Delete(actDir,true)
            with exn -> ()
    
        Directory.GetDirectories repositoryDir 
          |> Seq.iter deleteDirs      
    else
        CreateDir repositoryDir
    
    // set writeable
    File.SetAttributes(repositoryDir,FileAttributes.Normal)        