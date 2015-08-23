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
	Target "Clean" (fun () -> 
		trace " --- Cleaning stuff --- "
	)

	Target "Build" (fun () -> 
		trace " --- Building the app --- "
	)

	Target "Rebuild" DoNothing

	// *** Define Dependencies ***
	"Build" => "Rebuild"
	"Clean" => "Rebuild"
	// Make sure "Clean" happens before "Build", if "Clean" is is executed during a build.
	"Clean" ?=> "Build"
   
