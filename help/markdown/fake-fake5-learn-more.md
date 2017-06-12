# FAKE 5 - Learn more

> This is especially about the FAKE 5 update, [here you can learn about FAKE](fake-what-is-fake.html)

FAKE 5 is a huge undertaking in modularizing and porting FAKE 4 to dotnet-core.
At the same time we want to use that opportunity to pay old API depths.

On top of that compatibility was always a major concern when updating FAKE,
therefore we need a clear [migration path](fake-migrate-to-fake-5.html) for existing projects.

So before I go into the details of what changed and what the vision is going forward let me say:

> Every help matters. I've tried - but the code-base is just too large to handle.
> I think we now have a good foundation to finish the remaining work.
> Therefore I decided to make the finish line of FAKE 5 a community effort.
> If your favorite module is missing, please port it and send a PR (see Open Work section below).

## How stable is it? What is alpha? Should I use it?

First let me say that there are two distributions of FAKE 5.

 - The first one is an update to the 'regular' FAKE 4 package on NuGet.
   It's very safe to update to this package as mentioned in the [migrate to fake 5 guide](fake-migrate-to-fake-5.html).
   The reason it is considered "alpha" is because all "legacy"-APIs have been marked as obsolete, but new APIs have not been stabelized jet.
   So just ignore the `Obsolete` warnings and upgrade. If you start fixing the obsolete warnings we MIGHT break your script in an update.
   Hope this makes it clear why the Fake-5 nuget package is marked as alpha.
   Alpha stage will end when all `Obsolete` attributes are correct and the new API is considered stable.
 - The second one is the new dotnet-core version of FAKE.
   This version is brand-new, was migrated to a new platform and has several API changes which are not stabelized jet.
   So it's considered an alpha.
   But please use it: In the alpha phase we wan't to learn how we can handle breaking changes in the new modularized system and need actual users we break. Note that Fake 5 uses paket.lock for it's modules so you can always go back to a working version and report the issue. 

> Fake 5 and this website are already released with Fake 5, so it might be more stable than you think. It's just that 
> Some APIs are still missing and others might be renamed within the alpha phase.

## Modules? Packages? Paket?

In the past it was cumbersome and hard to extend your own builds with customized logic or share logic across builds easily.
Paket loading scripts helped a lot in that issue space, but now you have quite a lot infrastructor for simply running a build script.
So in a sense it made sense to combine all good parts of paket, dotnet-core and the huge fake library to go to the next level. The result is a standalone, dependency free version of Fake which is modular and extendible. On top of that you can now use all the Fake 5 modules in your "regular" F# projects!

You only need to know the simple [paket.dependencies](http://fsprojects.github.io/Paket/dependencies-file.html) syntax (basically a list of nuget packages) and are able to add custom logic to your script.
If you are in the scripting area there is not even a need to save `paket.dependencies` as a separate file. Just put it on top of the script. Fake will take care of the rest.

## Open Work / Help wanted

This section describes work that still need to be done to finally release FAKE 5.

 - Regulary updated issue with smaller (but blocking) work-items: https://github.com/fsharp/FAKE/issues/1523
 - Porting modules (See Modules -> Legacy), please read the migrate modules section in the [contribution guide](contributing.html).
 - Missing Obsolete Attributes in the old API (please help when you migrate and notice anything)
 - Bootstrapping via Script and System to update scripts automatically + lock FAKE version
 - Concept and work on how we want do document modules and module overviews (website is generated automatically)
 - Missing documentation
 - Update documentation
 - Fixing issues with the new website
 - Implement a special formatting for the obsolete warnings -> parse them -> suggest new API dependencies file.

## New API-Design

The old API suffered from several issues:

 - AutoOpen polluted the "Fake" namespace.

   One problem with this that some modules introduced generic function names which are perfectly valid for other modules as well. Now the second module needs to introduce a "quirk" name.

 - No visible structure and unified naming made it incredible hard to find an API.

For example consider the old API `CreateDirectory` which is a globally available.
It needs to have its context "Directory" in the name, but this means longer method names.
Imagine `Directory.Create` it's not really that much longer - but the IDE can support me a lot more.
This old API design lead to ambiguities and even user issues.

Therefore we propose a new [API design in the contributors guide](contributing.html)
