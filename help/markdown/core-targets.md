# Running targets in "FAKE - F# Make"

**Note:  This documentation is for FAKE 5. The old documentation can be found [here](legacy-core-targets.html)! **

[API-Reference](apidocs/fake-core-target.html), [Operators](apidocs/fake-core-targetoperators.html)

## Listing targets

Not yet available in FAKE 5

> Note: This feature still makes sense, but a good CLI has not been found yet, please propose one.
> For not you can run the target with name '--listTargets' or '-lt'. `fake run build.fsx -t '--list-Targets'`

## Running specific targets

FAKE has a special param "target" which can be used to run specific targets in a build. We assume the following build script (`build.fsx`):

	// include Fake modules, see Fake modules section
	
	open Fake.Core

	// *** Define Targets ***
	Target.Create "Clean" (fun _ -> 
		trace " --- Cleaning stuff --- "
	)

	Target.Create "Build" (fun _ -> 
		trace " --- Building the app --- "
	)

	Target.Create "Deploy" (fun _ -> 
		trace " --- Deploying app --- "
	)

	open Fake.Core.TargetOperators

	// *** Define Dependencies ***
	"Clean"
	  ==> "Build"
	  ==> "Deploy"

	// *** Start Build ***
	Target.RunOrDefault "Deploy"

> Warning: Previous versions of FAKE 5 used `(fun () -> ...)` instead of `(fun _ -> ...)`.
> We decided to change the syntax here to introduce some parameters or other features at a later point.
> Using the current parameter object is not supported yet.

Now we have the following options:

* `fake run build.fsx -t "Build"` --> starts the *Build* target and runs the dependency *Clean*
* `fake run build.fsx -t "Build"` --single-target --> starts only the *Build* target and runs no dependencies
* `fake run build.fsx -st -t Build` --> starts only the *Build* target and runs no dependencies
* `fake run build.fsx` --> starts the Deploy target (and runs the dependencies *Clean* and *Build*)

## Final targets

Final targets can be used for TearDown functionality. 
These targets will be executed even if the build fails but have to be activated via Target.ActivateFinal().

	Target.CreateFinal "CloseSomePrograms" (fun _ ->
		// close stuff and release resources
	)

	// Activate Final target somewhere during build
	Target.ActivateFinal "CloseSomePrograms"


## Build failure targets

Build failure targets can be used to execute tasks after a build failure.
These targets will be executed only after a build failure but have to be activated via ActivateBuildFailure().

	Target.CreateBuildFailure "ReportErrorViaMail" (fun _ ->
		// send mail about the failure
	)

	// Activate Build Failure Target somewhere during build
	Target.ActivateBuildFailure "ReportErrorViaMail"

## Visualising target dependencies

> WARNING: This is not supported, feel free to contribute.

FAKE can output the graph of target dependencies in the [DOT](http://www.graphviz.org/doc/info/lang.html)
format, which can then be rendered to a PNG-file by [Graphviz](http://www.graphviz.org).

Specifying the command line option *--dotGraph* (short version: *-dg*) makes FAKE write
the dependency graph to the standard output *instead* of building anything. This option only works when
the build script contains a call like this:

```
Target.RunOrDefault "Default"
``` 

### Example

Say, the build script `build.fsx` defines the target dependencies as follows:

```
// The rest of the script is omitted...

"BuildJs"
  ==> "WebPackage" <=> "AdminPackage" <=> "WebApiPackage"
  ==> "MainAppPackage"
```

The following command saves the target dependency graph in the `graph.png` file (PowerShell syntax):

```
FAKE.exe build.fsx -dg | & 'C:/Program Files (x86)/Graphviz2.38/bin/dot.exe' -Tpng -o ./graph.png
```

resulting in an image like this:

![graph](pics/specifictargets/graph.png "Dependency graph")


## Using FAKE's parallel option

Since multithreading is beneficial (especially for large projects) FAKE allows to specify the
number of threads used for traversing the dependency tree.
This option of course only affects independent targets whereas dependent targets will
still be exectued in order.


### Setting the number of threads
The number of threads used can be set using the environment variable ``parallel-jobs``.
This can be achieved in various ways where the easiest one is to use FAKE's built-in support for 
setting environment variables:

``fake *YourBuildScript* -e parallel-jobs 8``

Note that the dependency tree will be traversed as usual whenever setting ``parallel-jobs`` to a value ``<= 1`` or omiting it entirely.

## Issues
* Running targets in parallel is of course only possible when the target-functions themselves are thread-safe.
* Parallel execution may also cause races on stdout and build-logs may therefore be quite obfuscated.
* Error detection may suffer since it's not possible to determine a first error when targets are running in parallel

Due to these limitations it is recommended to use the standard sequential build whenever checking for errors (CI, etc.)
However when a fast build is desired (and the project is e.g. known to build successfully) the parallel option might be helpful

## Example

When using this parallel option, Fake resolves the build dependency hierearchies from the described paths and builds independend paths as parallel if you have multiple CPUs available.
For example this dependency tree:
	
	"Task 1"
	 ==> "Task A2"
	 ==> "Task 3"

	"Task 1"
	 ==> "Task B2"
	 ==> "Task 3"

	"Task C2"
	 ==> "Task 3"

	"Task 3"
	 ==> "Task A4"

	"Task 3"
	 ==> "Task B4"
...would be treated as follows:

![](pics/parallel/ParallelExample.png)

This is in addition to that that MsBuild may use multiple threads when building one solution having multiple independent project-files.

# Soft dependencies

Typically you will define dependencies among your targets using the `==>` and `<==` operators, and these 
dependencies define the order in which the targets are executed during a build.

You can also define soft dependencies among targets using the  `?=>` and `<=?` operators.  For example, you might
say that target B has a soft dependency on target A: 
    
    "A" ?=> "B"
    // Or equivalently
    "B" <=? "A"

With this soft dependency, running B will not require that A be run first. However it does mean that *if* A is run 
(due to other dependencies) it must be run before B.

## Example

	// *** Define Targets ***
	Target.Create "Clean" (fun () -> 
		trace " --- Cleaning stuff --- "
	)

	Target.Create "Build" (fun () -> 
		trace " --- Building the app --- "
	)

	Target.Create "Rebuild" DoNothing

	// *** Define Dependencies ***
	"Build" ==> "Rebuild"
	"Clean" ==> "Rebuild"
	// Make sure "Clean" happens before "Build", if "Clean" is executed during a build.
	"Clean" ?=> "Build"