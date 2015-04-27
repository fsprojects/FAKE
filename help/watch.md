# Watching for file changes with "FAKE - F# Make"

FAKE makes it easy to setup monitoring for filesystem changes. Using the standard glob patterns you can
watch for changes, and automatticly run a function or another target.

## Using WatchChanges

Add a new target named "Watch" to your build:

    Target "GenerateDocs" (fun _ ->
        printfn "Generating docs."
    )

    Target "Watch" (fun _ ->
        use watcher = !! "docs/**/*.*" |> WatchChanges (fun changes -> 
            printfn "%A" changes
            RunTarget "GenerateDocs"
        )
    
        System.Console.ReadLine() |> ignore //Needed to keep FAKE from exiting
    
        watcher.Dispose() // Use to stop the watch from elsewhere, ie another task.
    )

Now run build.fsx and make some changes to the docs directory. They should be printed out to the console as they happen,
and the GenerateDocs target should be rerun.

If you need to watch only a subset of the files, say you want to rerun tests as soon as the compiled dlls change:

    Target "RunTests" (fun _ ->
        printfn "Running tests."
    )
    
    Target "Watch" (fun _ ->
        use watcher = !! "tests/**/bin/debug/*.dll" |> WatchChanges (fun changes -> 
            printfn "%A" changes
            RunTarget "RunTests"
        )
    
        System.Console.ReadLine() |> ignore //Needed to keep FAKE from exiting
    
        watcher.Dispose() // Use to stop the watch from elsewhere, ie another task.
    )