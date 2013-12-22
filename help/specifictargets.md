# Running specific targets in "FAKE - F# Make"

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

* Fake.exe "target=Build" --> starts only the *Build* target (and runs the dependency *Clean*)
* Fake.exe Build --> starts only the *Build* target (and runs the dependency *Clean*)
* Fake.exe --> starts the Deploy target (and runs the dependencies *Clean* and *Build*)
