[<AutoOpen>]
/// This module contains helper function to create and extract zip archives.
module Fake.ChangeWatcher

open System.IO

type FileStatus = 
    | Deleted
    | Created
    | Changed

type FileChange = 
    { FullPath : string
      Name : string
      Status : FileStatus }

let private handleWatcherEvents (status : FileStatus) (onChange : FileChange -> unit) (e : FileSystemEventArgs) = 
    onChange ({ FullPath = e.FullPath
                Name = e.Name
                Status = status })

/// Watches the for changes matching in the mathcing files.
/// ## Parameters
///  - `onChange` - function to call when a change is detected.
///  - `fileIncludes` - The glob pattern for files to watch for changes.
let WatchChanges (onChange : FileChange seq -> unit) (fileIncludes : FileIncludes) = 
    let dirsToWatch = fileIncludes.Includes |> Seq.map (fun file -> Globbing.getRoot fileIncludes.BaseDirectory (file))
    
    // remove children directoryss from watch list so that we dont get duplicate file watchers running
    let dirsToWatch = 
        dirsToWatch |> Seq.filter (fun d -> 
                           dirsToWatch
                           |> Seq.exists (fun p -> p.StartsWith d && p <> d)
                           |> not)
    
    let unNotifiedChanges = ref List.empty<FileChange>
    
    let acumChanges (fileChange : FileChange) = 
        if fileIncludes.IsMatch fileChange.FullPath then 
            lock unNotifiedChanges (fun () -> unNotifiedChanges := [ fileChange ] @ !unNotifiedChanges)
    
    let timer = new System.Timers.Timer(5.0)
    timer.Elapsed.Add(fun _ -> 
        lock unNotifiedChanges (fun () -> 
            if !unNotifiedChanges
               |> Seq.length
               > 0 then 
                let changes = 
                    !unNotifiedChanges
                    |> Seq.groupBy (fun c -> c.FullPath)
                    |> Seq.map (fun (name, changes) -> 
                           changes
                           |> Seq.sortBy (fun c -> c.Status)
                           |> Seq.head)
                unNotifiedChanges := List.empty<FileChange>
                onChange changes))
    printfn "dirs to watch: %A" dirsToWatch
    let watchers = 
        dirsToWatch |> Seq.map (fun dir -> 
                           printfn "watching dir: %s" dir
                           let watcher = new FileSystemWatcher(FullName dir, "*.*")
                           watcher.EnableRaisingEvents <- true
                           watcher.IncludeSubdirectories <- true
                           watcher.Changed.Add(handleWatcherEvents Changed acumChanges)
                           watcher.Created.Add(handleWatcherEvents Created acumChanges)
                           watcher.Deleted.Add(handleWatcherEvents Deleted acumChanges)
                           watcher.Renamed.Add(fun (e : RenamedEventArgs) -> 
                               acumChanges { FullPath = e.OldFullPath
                                             Name = e.OldName
                                             Status = Deleted }
                               acumChanges { FullPath = e.FullPath
                                             Name = e.Name
                                             Status = Created })
                           watcher)
    watchers
    |> Seq.length
    |> ignore //force iteration
    timer.Start()
    fun () -> 
        for watcher in watchers do
            watcher.EnableRaisingEvents <- false
            watcher.Dispose()
        timer.Dispose()
