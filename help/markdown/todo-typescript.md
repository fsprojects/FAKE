# Compiling TypeScript applications

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help!</p>
</div>

FAKE can be used to build a variety of different application types. 
In this tutorial we are looking at the TypeScript support.

Consider a greetings.ts file:

    [lang=javascript]
    interface Person {
        firstname: string;
        lastname: string;
    }
     
    function greeter(person : Person) {
        return "Hello, " + person.firstname + " " + person.lastname;
    }

    var user = {firstname: "Jane", lastname: "User"};

    document.body.innerHTML = greeter(user);

Now create a build.fsx and run it via FAKE.exe:

	#I @"../../tools/FAKE/tools/"
	#r @"FakeLib.dll"

	open Fake
	open System
	open TypeScript

	Target "CompileTypeScript" (fun _ ->
	    !! "**/*.ts"
		  |> TypeScriptCompiler (fun p -> { p with OutputPath = "./out" }) 
	)

	RunTargetOrDefault "CompileTypeScript"


This small script will run all *.ts files through the TypeScript compiler and put them into the ./out/ folder. In this case we will find a greetings.js:

    [lang=javascript]
	function greeter(person) {
		return "Hello, " + person.firstname + " " + person.lastname;
	}

	var user = { firstname: "Jane", lastname: "User" };

	document.body.innerHTML = greeter(user);

If you need more details please see the [API docs for the TypeScript](apidocs/v5/legacy/fake-typescript.html) task.
