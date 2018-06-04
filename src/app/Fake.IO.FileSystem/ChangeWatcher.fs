/// This module contains helpers to react to file system events.
namespace Fake.IO

open System.IO
open Fake.IO
open System.Threading
open System

type FileStatus =
    | Deleted
    | Created
    | Changed

type FileChange =
    { FullPath : string
      Name : string
      Status : FileStatus }

/// This module is part of the `Fake.IO.FileSystem` package
///
/// ## Sample
///
///     Target.create "Watch" (fun _ ->
///         use watcher = !! "c:/projects/watchDir/*.txt" |> ChangeWatcher.run (fun changes ->
///             // do something
///         )
///
///         System.Console.ReadLine() |> ignore
///
///         watcher.Dispose() // if you need to cleanup the watcher.
///     )
///
module ChangeWatcher =

    type Options =
        { IncludeSubdirectories: bool }

    let private handleWatcherEvents (status : FileStatus) (onChange : FileChange -> unit) (e : FileSystemEventArgs) =
        onChange ({ FullPath = e.FullPath
                    Name = e.Name
                    Status = status })

    /// Watches for changes in the matching files.
    /// Returns an IDisposable which allows to dispose all internally used FileSystemWatchers.
    ///
    /// ## Parameters
    ///  - `onChange` - function to call when a change is detected.
    ///  - `fileIncludes` - The glob pattern for files to watch for changes.
    let runWithOptions (foptions:Options -> Options) (onChange : FileChange seq -> unit) (fileIncludes : IGlobbingPattern) =
        let options = foptions { IncludeSubdirectories = true }
        let dirsToWatch = fileIncludes |> GlobbingPattern.getBaseDirectoryIncludes

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
        let timer = Lazy<IDisposable>(Func<IDisposable> (fun ()-> 
            // NOTE: that the timer starts immidiatelly when constructed
            // we could delay this by sending it how many ms it should delay
            // itself
            // The timer here has a period of 50 ms:
            new Timer(timerCallback, Object(), 0, 50) :> IDisposable
            ), LazyThreadSafetyMode.ExecutionAndPublication)

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
                  // only dispose the timer if it has been constructed
                  if timer.IsValueCreated then timer.Value.Dispose() }


    let run (onChange : FileChange seq -> unit) (fileIncludes : IGlobbingPattern) = runWithOptions id onChange fileIncludes
