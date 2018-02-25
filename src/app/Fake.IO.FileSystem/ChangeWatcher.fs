[<AutoOpen>]
/// This module contains helpers to react to file system events.
module Fake.IO.FileSystem.ChangeWatcher

open System.IO
open Fake.Core
open Fake.IO

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
let WatchChangesWithOptions options (onChange : FileChange seq -> unit) (fileIncludes : IGlobbingPattern) =
    let dirsToWatch = fileIncludes |> GlobbingPattern.GetBaseDirectoryIncludes

    //tracefn "dirs to watch: %A" dirsToWatch

    // we collect changes in a mutable ref cell and wait for a few milliseconds to
    // receive all notifications when the system sends them repetedly or sends multiple
    // updates related to the same file; then we call 'onChange' with all cahnges
    let unNotifiedChanges = ref List.empty<FileChange>
    // when running 'onChange' we ignore all notifications to avoid infinite loops
    let runningHandlers = ref false
    let timerCallback = fun _ ->
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
                    runningHandlers := false )
    // lazy evaluation of timer in order to only start timer once requested
    let timer = lazy( new System.Threading.Timer(timerCallback, System.Object(), 0, 50) )

    let acumChanges (fileChange : FileChange) =
        // only record the changes if we are not currently running 'onChange' handler
        if not !runningHandlers && fileIncludes.IsMatch fileChange.FullPath then
            lock unNotifiedChanges (fun () ->
              unNotifiedChanges := fileChange :: !unNotifiedChanges
              // start the timer (ignores repeated calls) to trigger events in 50ms
              (timer.Value |> ignore) )

    let watchers =
        dirsToWatch |> List.map (fun dir ->
                           //tracefn "watching dir: %s" dir

                           let watcher = new FileSystemWatcher(Path.getFullName dir, "*.*")
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
              timer.Value.Dispose() }


let WatchChanges (onChange : FileChange seq -> unit) (fileIncludes : IGlobbingPattern) = WatchChangesWithOptions { IncludeSubdirectories = true } onChange fileIncludes
