[<AutoOpen>]
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

///<summary>Cleans a directory by removing all files and sub-directories.</summary>
///<param name="path">The path of the directory to clean.</param>
///<user/>
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
            Directory.Delete(actDir,true)
    
        Directory.GetDirectories repositoryDir 
          |> Seq.iter deleteDirs      
    else
        CreateDir repositoryDir
    
    // set writeable
    File.SetAttributes(repositoryDir,FileAttributes.Normal)        