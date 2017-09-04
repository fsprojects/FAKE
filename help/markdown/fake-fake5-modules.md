# FAKE 5 - Custom Modules

## Requirements to modules

You can use any NuGet packages which are compatible with `netcore10`, for example all packages targeting `netstandard16` or lower.
 
## Declaring module dependencies

### Reference paket group

Create a new file `paket.dependencies` and add the following content

```
// [ FAKE GROUP ]
group NetcoreBuild
    source https://nuget.org/api/v2

    nuget Fake.Core.Targets prerelease
```

Now you can directly use `open Fake.Core` and use the [targets module](core-targets.html).

For example create a new file `build.fsx` with

```fsharp
// Use this for IDE support. Not required by FAKE 5. Change "build.fsx" to the name of your script.
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core

Target.Create "MyBuild" (fun _ ->
    printfn "message from MyBuild target"
)

Target.RunOrDefault "MyBuild"

```

and execute `fake run build.fsx`. This works for all FAKE 5 modules, just add other `nuget Fake.Other.Module prerelease` files to the `paket.dependencies` file.

Please read introduction about [Paket](https://fsprojects.github.io/Paket/) for more information about the `paket.dependencies` file.

> Note: This works because by default FAKE 5 searches for a group annotated with the `// [ FAKE GROUP ]` comment.


## Declaring FAKE 5 Header

To get more control over this FAKE 5 behavior you can explicitely set a header to the build script.

### General Fake HEADER

To tell Fake which dependencies are needed a script can start with a header as well:

```fsharp
(* -- Fake Dependencies ***header***
*** Dependencies ***
-- Fake Dependencies -- *)
#load "./.fake/build.fsx/intellisense.fsx"
```

The last line `#load` is not requiredby FAKE 5, however
this way the file can still be edited in editors (after restoring packages initially).
Fake will write an `intellisense.fsx` file for you importing all required references.
Please note that as of right now, Fake doesn't write anything useful into this file, yet.

There are two headers known by Fake:

### Reference paket group

To reference a FAKE group explicitely you can put the following at the top of your build script

	[lang=fsharp]
    (* -- Fake Dependencies paket.dependencies
    file ./paket.dependencies
    group netcorebuild
    -- Fake Dependencies -- *)
    #load "./.fake/build.fsx/intellisense.fsx"

This header will reference a `paket.dependencies` file and a group within.

### Inline dependencies

To write your build dependencies in-line you can put the following at the top of your build script

	[lang=fsharp]
    (* -- Fake Dependencies paket-inline
    source https://nuget.org/api/v2

    nuget Fake.Core.Targets prerelease
    -- Fake Dependencies -- *)
    #load "./.fake/build.fsx/intellisense.fsx"

This has the advantage that your build-script is now "standalone" and no separate `paket.dependencies` is required.
We still recommend to add (`git add -f`, because usually you have `.fake` folder gitignored) the generated `paket.lock` (in `.fake/<scriptName>/`) to your repository to have reproducable script runs.


## Using module dependencies

You use the modules as documented in their corresponding help section.

But usually it's:
 - `open Fake.<Namespace>` for example `open Fake.Core`
 - Using the features