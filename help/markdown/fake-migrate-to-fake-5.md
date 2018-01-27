# The FAKE 5 Migration Guide

## Intro

In this tutorial you will learn how to migrate your existing build scripts to the new FAKE 5 dotnet-core version.

First we want you to know that there are two versions of FAKE 5. One is just an update to the regular FAKE 4, but also contains the new API.
We will call this the "legacy FAKE version" it is just like the FAKE you are already used to. The second version is the "new/dotnetcore/standalone FAKE 5" or just "FAKE 5".
This "new" version has several advantages:

* It can run without Mono or .Net installed
* It is extendible via paket
* Paket bootstrapper / build.cmd and build.sh are no longer required (you can still use them)
* This will be the only Version available in FAKE 6

Therefore you have the FAKE 5 timeframe to update your build scripts to the new version. If you have any issues in the migration process, please see [how to fill issues or discuss about your issues](/contributing.html) (don't be shy about contributing ;)).

## Migration Guide

Upgrading to FAKE 5 is a multi step process and has various manual steps in between. **If you do these steps out of order it will be a lot harder for you to migrate the script successfully**. Here are the steps:

- Update to legacy FAKE 5. This should not be breaking. If it breaks you please open an issue.

  - With Paket: add `prerelease` after `nuget FAKE` in paket.dependencies file then `.paket/paket.exe update`, check that paket.lock references FAKE version 5 

- Fix all the (obsolete) warnings in your build-script to use the new API (see the [Use the new FAKE-API](#Use-the-new-FAKE-API) section).
  This should still not break your build. If things break here or you have difficulties after reading the 'Use the new FAKE-API' section
  please open an issue.
  - Becareful if you update only some warning, it could break. For example, if you use `Target.Create`, but continue to use old operators definition, you will probably experiment some errors like "Target [...] is not defined".  
- Change to the new version of FAKE 5.
  
  - This is for example done by installing FAKE as dependency on your build infrastructure.
    There are a variety of installing options available. (TODO: Link to 'installing FAKE' section)
  - Add a FAKE header (TODO: add Link), and tell FAKE which features/packages you want to use in the dependencies file or in-line.
    See the [Adding FAKE dependencies](#Adding-FAKE-dependencies) section below.
  - Run the build with the new version of FAKE :). You might want to read the [CLI migration](#CLI-Migration) section
  
  If things break in the last step please let us know as well.

### Use the new FAKE-API

After upgrading to legacy FAKE 5 the warnings should tell you exactly what you do. If there is stuff missing or a warning message should be improved let us know.
Some warnings indicate how we want the new FAKE version to be used.

The most important part to know is that basically every feature/function changes its location and sometimes they were even grouped in different modules
as the old API was growing several years now and we never could do breaking changes.

> IMPORTANT NOTE: If you feel like an API is still in the wrong place. Please let us know and open an issue.
  We can make breaking changes as long as we are in the alpha/beta phase.

> IMPORTANT NODE: It's obvious from the NOTE above, but we are still figuring out the new API (even if I'm quite happy with its current state).
  So things might break in the alpha phase. However this doesn't mean you should be afraid to use the new FAKE version.
  In fact due to the modular design we hopefully can make breaking changes a lot more often, without breaking your builds!
  So please try it out and if stuff breaks let us know :).
  The good thing is you can always "lock" the versions of the FAKE modules until you are ready to upgrade.

In this new work you should write "Module.Method a b" instead of "MethodModule a b". Which means in the old world we had lots of methods like
"ReadFile argument" (the module probably even opened via `[<AutoOpen>]`), which is considered bad style now.
In the new world we would open the `Fake.IO.FileSystem` namespace to indicate that we are using the file-system.
At the same time we would write `File.Read argument`, which is only a bit longer but now the IDE can help you a lot better and the code looks a lot more ideomatic and clean.

> If you still find places where we use the "bad" style in the new API, let us know (open an issue).

The "open Fake" and AutoOpen modules are completely obsolete. 
We urge you to finish your API-Migration (after fixing all warnings) by removing "open Fake".
This removes a lot of automatically opened stuff and if your build fails you have probably stuff where we forgot to add the obsolete warning (let us know) or that 
stuff you are using was not migrated yet (let us know or send a PR, TODO: Add link to guideline).

### Add FAKE dependencies

All your dependencies no longer are bundled with the FAKE nuget package (or the FAKE distribution for that matter). This has some advantages:
- You can extend the build with your own packages easily
- You only pay what you use
- You can use the FAKE libries in your regular scripts (as all FAKE modules are regular NuGet packages you can use in your projects as well).

The drawback however is that you now need to know where stuff lives and add those packages to your build.

 Already a paket user?

For now its quite simple: The namespace or module name is used as the package name, just search for the package and then
add it to the dependencies file to a new group (for example netcorebuild).

 Not a paket user?

We currently don't have any simple advice but to read the dependencies file docs and go to the 'Already a paket user?' section.

Once you added all your dependencies to a particular new group (for example netcorebuild). You can add the following header to your build script

    (* -- Fake Dependencies paket.dependencies
    file ./paket.dependencies
    group netcorebuild
    -- Fake Dependencies -- *)

Now you can install and run the netcore version of FAKE and it will restore and use the dependencies (you might need to do a `./paket update` before FAKE can properly restore the packages).

### CLI Migration

Yes we even broke the CLI. The old CLI was actually a mixture of two different CLI styles, confused a lot of users and to be honest was an ugly hack.
It was obvious that we would not even try to make things compatible with it in any way.
However your changes should only be minimal in the most cases. I'd say in 80% its just about adding the `run` verb between the `fake.exe` and the build script.
`fake build.fsx` will be `fake run build.fsx`. Running a particular target is as easy `fake target` will be `fake run build.fsx --target target`.
For a full reference use `--help` or the documentation.

If you used special cases which aren't mentioned here please edit this page or open an issue.

