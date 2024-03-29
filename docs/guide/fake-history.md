# FAKE History Overview

FAKE 5 is considered a turning point for FAKE in which it was a huge undertaking in modularizing and 
porting FAKE 4 and earlier to dotnet-core. At the same time we want to use that opportunity to pay old API debts.

On top of that compatibility, was always a major concern when updating FAKE,
therefore we needed a clear [*migration path*](/guide/fake-migrate-to-fake-5.html) for existing projects.

Currently FAKE 5 is released and shifted to a modular design and usage, in which users now use the modules
they only need, and with the power of the FAKE runner they can run scripts as fast as possible by utilizing
the different features that the FAKE runner offers.

FAKE 5 started the transition by porting the majority of the modules from legacy codebase to the new FAKE API
design and guideline. Continuing on that effort, FAKE 6 now completes the majority of the transition and set the
foundation for future releases of FAKE. FAKE 6 ports more modules to the new FAKE API design, remove the obsolete
and deprecated APIs and replacing their usage with altervatives, enhancing the FAKE website.  


So before we go into the details of what changed and what the vision is going forward let me say:

> Every help matters. I've tried - but the code-base is just too large to handle.
> I think we now have a good foundation to finish the remaining work.
> Therefore I decided to make the finish line of FAKE 5 a community effort.
> If your favorite module is missing, please port it and send a PR (see Open Work section below).

## How stable is it?

We had quite a long alpha, beta and rc-phase so we are positive that it is ready.
There might be some missing modules as we have not 100% coverage of the old API.
If you are blocked by missing modules please consider porting it and sending a PR.

## Modules? Packages? Paket?

In the past it was cumbersome and hard to extend your own builds with customized logic or share logic across builds easily.
Paket loading scripts helped a lot in that issue space, but now you have quite a lot infrastructure for simply running a build script.
So in a sense it made sense to combine all good parts of paket, dotnet-core and the huge fake library to go to the next level. The result is a standalone, dependency free version of Fake which is modular and extendible. On top of that you can now use all the Fake modules in your "regular" F# projects!

You only need to know the simple [*paket.dependencies*](http://fsprojects.github.io/Paket/dependencies-file.html) syntax (basically a list of nuget packages) and are able to add [*custom logic*](/guide/fake-custom-modules.html) to your script.
If you are in the scripting area there is not even a need to save `paket.dependencies` as a separate file. Just put it on top of the script. Fake will take care of the rest.

## Open Work / Help wanted

This section describes work that still need to be done to finally complete the transition 100%.

* Porting modules (See Modules -> Legacy), please read the migrate modules section in the [*contribution guide*](/guide/contributing.html).
* Missing Obsolete Attributes in the old API (please help when you migrate and notice anything)
* Bootstrapping via Script and System to update scripts automatically + lock FAKE version
* Concept and work on how we want do document modules and module overviews (website is generated automatically)
* Missing documentation
* Update documentation
* Fixing issues with the new website

## New API-Design

The old API suffered from several issues:

* `AutoOpen` polluted the "Fake" namespace.

  One problem with this that some modules introduced generic function names which are perfectly valid for other modules as well. Now the second module needs to introduce a "quirk" name.

* No visible structure and unified naming made it incredible hard to find an API.

For example consider the old API `CreateDirectory` which is a globally available.
It needs to have its context "Directory" in the name, but this means longer method names.
Imagine `Directory.Create` it's not really that much longer - but the IDE can support me a lot more.
This old API design lead to ambiguities and even user issues.

Therefore we propose a new [*API design in the contributors guide*](/guide/contributing.html)
