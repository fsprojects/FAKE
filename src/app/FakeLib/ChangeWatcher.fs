[<AutoOpen>]
/// This module contains helpers to react to file system events.
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

type WatchChangesOption =
    { IncludeSubdirectories: bool }

let private handleWatcherEvents (status : FileStatus) (onChange : FileChange -> unit) (e : FileSystemEventArgs) =
    onChange ({ FullPath = e.FullPath
                Name = e.Name
                Status = status })

let private calcDirsToWatch fileIncludes =
    let dirsToWatch = fileIncludes.Includes |> Seq.map (fun file -> Globbing.getRoot fileIncludes.BaseDirectory file)

    // remove subdirectories from watch list so that we don't get duplicate file watchers running
    dirsToWatch
    |> Seq.filter (fun d ->
                    dirsToWatch
                    |> Seq.exists (fun p -> d.StartsWith p && p <> d)
                    |> not)
    |> Seq.toList

/// Watches the for changes in the matching files.
/// Returns an IDisposable which allows to dispose all FileSystemWatchers.
///
/// ## Parameters
///  - `onChange` - function to call when a change is detected.
///  - `fileIncludes` - The glob pattern for files to watch for changes.
///
/// ## Sample
///
///     Target "Watch" (fun _ ->
///         use watcher = !! "c:/projects/watchDir/*.txt" |> WatchChanges (fun changes ->
///             // do something
///         )
///
///         System.Console.ReadLine() |> ignore
///
///         watcher.Dispose() // if you need to cleanup the watches.
///     )
///
let WatchChangesWithOptions options (onChange : FileChange seq -> unit) (fileIncludes : FileIncludes) =
    let dirsToWatch = fileIncludes |> calcDirsToWatch

    tracefn "dirs to watch: %A" dirsToWatch

    // we collect changes in a mutable ref cell and wait for a few milliseconds to
    // receive all notifications when the system sends them repetedly or sends multiple
    // updates related to the same file; then we call 'onChange' with all cahnges
    let unNotifiedChanges = ref List.empty<FileChange>
    // when running 'onChange' we ignore all notifications to avoid infinite loops
    let runningHandlers = ref false

    let timer = new System.Timers.Timer(50.0)
    timer.AutoReset <- false
    timer.Elapsed.Add(fun _ ->
        lock unNotifiedChanges (fun () ->
            if not (Seq.isEmpty !unNotifiedChanges) then
                let changes =
                    !unNotifiedChanges
                    |> Seq.groupBy (fun c -> c.FullPath)
                    |> Seq.map (fun (name, changes) ->
                           changes
                           |> Seq.sortBy (fun c -> c.Status)
                           |> Seq.head)
                unNotifiedChanges := []
                try
                    runningHandlers := true
                    onChange changes
                finally
                    runningHandlers := false ))

    let acumChanges (fileChange : FileChange) =
        // only record the changes if we are not currently running 'onChange' handler
        if not !runningHandlers && fileIncludes.IsMatch fileChange.FullPath then
            lock unNotifiedChanges (fun () ->
              unNotifiedChanges := fileChange :: !unNotifiedChanges
              // start the timer (ignores repeated calls) to trigger events in 50ms
              (timer:System.Timers.Timer).Start() )

    let watchers =
        dirsToWatch |> List.map (fun dir ->
                           tracefn "watching dir: %s" dir

                           let watcher = new FileSystemWatcher(FullName dir, "*.*")
                           watcher.EnableRaisingEvents <- true
                           watcher.IncludeSubdirectories <- options.IncludeSubdirectories
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

    { new System.IDisposable with
          member this.Dispose() =
              for watcher in watchers do
                  watcher.EnableRaisingEvents <- false
                  watcher.Dispose()
              timer.Dispose() }


let WatchChanges = WatchChangesWithOptions { IncludeSubdirectories = true }
