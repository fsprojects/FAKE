# Running targets in "FAKE - F# Make"

## Listing targets
Before running any specific target it is useful to know all the targets that are available in a build script.
FAKE can list all the avaiable targets including the dependencies by running the following command:

* Fake.exe *YourBuildScript* -lt

The option *-lt* stands for "list targets". It is an abbreviation of the option *--listTargets*.

## Running specific targets

FAKE has a special param "target" which can be used to run specific targets in a build. We assume the following build script:

	// include Fake libs
	#I @"tools\FAKE"
	#r "FakeLib.dll"
	open Fake 

	// *** Define Targets ***
	Target "Clean" (fun () -> 
		trace " --- Cleaning stuff --- "
	)

	Target "Build" (fun () -> 
		trace " --- Building the app --- "
	)

	Target "Deploy" (fun () -> 
		trace " --- Deploying app --- "
	)

	// *** Define Dependencies ***
	"Clean"
	  ==> "Build"
	  ==> "Deploy"

	// *** Start Build ***
	RunTargetOrDefault "Deploy"

Now we have the following options:

* Fake.exe "target=Build" --> starts the *Build* target and runs the dependency *Clean*
* Fake.exe Build --> starts the *Build* target and runs the dependency *Clean*
* Fake.exe Build --single-target --> starts only the *Build* target and runs no dependencies
* Fake.exe Build -st --> starts only the *Build* target and runs no dependencies
* Fake.exe --> starts the Deploy target (and runs the dependencies *Clean* and *Build*)

## Final targets

Final targets can be used for TearDown functionality. 
These targets will be executed even if the build fails but have to be activated via ActivateFinalTarget().

	FinalTarget "CloseSomePrograms" (fun _ ->
		// close stuff and release resources
	)

	// Activate FinalTarget somewhere during build
	ActivateFinalTarget "CloseSomePrograms"


## Build failure targets

Build failure targets can be used to execute tasks after a build failure.
These targets will be executed only after a build failure but have to be activated via ActivateBuildFailureTarget().

	BuildFailureTarget "ReportErrorViaMail" (fun _ ->
		// send mail about the failure
	)

	// Activate BuildFailureTarget somewhere during build
	ActivateBuildFailureTarget "ReportErrorViaMail"
