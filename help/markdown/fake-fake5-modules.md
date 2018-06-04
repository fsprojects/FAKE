# FAKE 5 - Custom Modules

## Requirements to modules

You can use any NuGet packages which are compatible with `netcoreapp20`, for example all packages targeting `netstandard20` or lower.
 
## Declaring module dependencies

### Reference paket group

Create a new file `paket.dependencies` and add the following content

```
// [ FAKE GROUP ]
group NetcoreBuild
    source https://api.nuget.org/v3/index.json

    nuget Fake.Core.Target prerelease
```

Now you can directly use `open Fake.Core` and use the [Target module](core-targets.html).

For example create a new file `build.fsx` with

```fsharp
// Use this for IDE support. Not required by FAKE 5. Change "build.fsx" to the name of your script.
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core

Target.create "MyBuild" (fun _ ->
    printfn "message from MyBuild target"
)

Target.runOrDefault "MyBuild"

```

and execute `fake run build.fsx`. This works for all FAKE 5 modules, just add other `nuget Fake.Other.Module prerelease` files to the `paket.dependencies` file.

Please read introduction about [Paket](https://fsprojects.github.io/Paket/) for more information about the `paket.dependencies` file.

<div class="alert alert-info">
    <h5>NOTE</h5>
    This works because by default FAKE 5 searches for a group annotated with the <code>// [ FAKE GROUP ]</code> comment.
</div> 

## Declaring FAKE 5 dependencies within the script

To be more independent from paket infrastructure (stand-alone-scripts and similar situations) there is a way to specify dependencies from within the script itself.

<div class="alert alert-info">
    <h5>INFO</h5>
    We use the new syntax specified in <a href="https://github.com/fsharp/fslang-design/blob/master/tooling/FST-1027-fsi-references.md">FST-1027</a>.
    However, to be fully compatible with existing tooling and infrastructure make sure to add `//` at the end of the `#r` string.
    See <a href="https://github.com/fsharp/FAKE/pull/1770"/> for details.
</div> 


### Reference a paket group

To reference a FAKE group explicitely you can put the following at the top of your build script

```fsharp
#r "paket: groupref netcorebuild //"
#load "./.fake/build.fsx/intellisense.fsx"
```

This header will reference the `paket.dependencies` file and the `netcorebuild` group within.

The last line `#load` is not required by FAKE 5, however
this way the file can still be edited in editors (after restoring packages initially).
Fake will write an `intellisense.fsx` file for you importing all required references.

<div class="alert alert-info">
    <h5>INFO</h5>
    Note that in this scenario the <code>// [ FAKE GROUP ]</code> comment mentioned above is not required.
</div>
### Inline dependencies

To write your build dependencies in-line you can put the following at the top of your `build.fsx` script

```fsharp
#r "paket:
nuget Fake.Core.Target prerelease //"
#load "./.fake/build.fsx/intellisense.fsx"

```

This has the advantage that your build-script is now "standalone" and no separate `paket.dependencies` is required.
Fake will generate a `build.fsx.lock` file with the "locked" dependencies to have reproducable script runs.
If you need to update your dependencies just delete the `build.fsx.lock` file and run fake again.

## Using module dependencies

You use the modules as documented in their corresponding help section.

But usually it's:

* `open Fake.<Namespace>` for example `open Fake.Core`
* Using the features
