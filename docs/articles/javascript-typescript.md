# Compiling TypeScript applications

FAKE can be used to build a variety of different application types. In this tutorial we are 
looking at the TypeScript support.

To see the available TypeScript compiler APIs in FAKE, please see the [`API-Reference`](/reference/fake-javascript-typescript.html) 
for the TypeScript module.


Consider a `greetings.ts` file:

```typescript
    interface Person {
        firstname: string;
        lastname: string;
    }
     
    function greeter(person : Person) {
        return "Hello, " + person.firstname + " " + person.lastname;
    }

    var user = {firstname: "Jane", lastname: "User"};

    document.body.innerHTML = greeter(user);
```

Now create a `build.fsx` and run it via the FAKE runner:

```fsharp
	#r "paket:
	source https://api.nuget.org/v3/index.json
	nuget Fake.JavaScript.TypeScript //"

	open Fake.JavaScript

	Target "CompileTypeScript" (fun _ ->
	    !! "src/**/*.ts"
             |> TypeScript.compile (fun p -> { p with TimeOut = TimeSpan.MaxValue })
	)

	RunTargetOrDefault "CompileTypeScript"
```

This small script will run all `*.ts` files through the TypeScript compiler and put them into the ./out/ folder. 
In this case we will find a `greetings.js`:

```javascript
	function greeter(person) {
		return "Hello, " + person.firstname + " " + person.lastname;
	}

	var user = { firstname: "Jane", lastname: "User" };

	document.body.innerHTML = greeter(user);
```
