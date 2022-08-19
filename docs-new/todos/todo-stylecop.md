# Adding StyleCop to a FAKE build script

This article explains the basics of how your source code can by analyzed using **StyleCop**.

## Setting up StyleCop

The **StyleCop** functionality is written in the default `Fake` assembly, so writing a target for **Code Inspection** could look like this:

```fsharp
Target "Inspect" (fun _ ->
    StyleCop 
        (fun p -> 
            { p with 
                SolutionFiles = [ "MySolution.sln" ] })
)
```

That's it!
Now **StyleCop** will inspect all the source files in your solution.

## Move Advanced Options

### Letting the Build Fail

The **StyleCop** module has the functionality to let the build fail if any violations occur. This can be configured by setting the `ErrorLevel` value in the `StyleCopParams`:

```fsharp
Target "Inspect" (fun _ ->
    StyleCop 
        (fun p -> 
            { p with 
                SolutionFiles = [ "MySolution.sln" ]
                ErrorLevel = Fail })
)
```

Default this `ErrorLevel` is set to `Warning` but setting it to `Fail` will let the build fail.

### Using StyleCop Settings File

The **StyleCop** module has the functionality to use as custom settings file with your own rules of how **StyleCop** must analyze your source files:

```fsharp
Target "Inspect" (fun _ ->
    StyleCop 
        (fun p -> 
            { p with 
                SolutionFiles = [ "MySolution.sln" ]
                SettingsFile = "Settings.StyleCop" })
)
```

Default **StyleCop** don't uses any settings file.