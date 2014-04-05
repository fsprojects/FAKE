# Compiling TypeScript applications

FAKE can be used to build a variety of different application types. 
In this tutorial we are looking at the TypeScript support.

Consider a greetings.ts file:

    [lang=typescript]
    interface Person {
        firstname: string;
        lastname: string;
    }
     
    function greeter(person : Person) {
        return "Hello, " + person.firstname + " " + person.lastname;
    }

    var user = {firstname: "Jane", lastname: "User"};

    document.body.innerHTML = greeter(user);

Now create a build.fsx and run it via Fake.exe:

	#I @"../../tools/FAKE/tools/"
	#r @"FakeLib.dll"

	open Fake
	open System
	open TypeScript

	Target "CompileTypeScript" (fun _ ->
	    !! "**/*.ts"
		  |> TypeScriptCompiler (fun p -> { p with TimeOut = TimeSpan.MaxValue }) 
	)

	RunTargetOrDefault "CompileTypeScript"

If you need more details please see the [API docs for the TypeScript](apidocs/fake-typescript.html) task.