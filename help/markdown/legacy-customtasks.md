# Writing custom C# tasks for FAKE

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation for FAKE 5 can be found <a href="fake-fake5-custom-modules.html">here </a></p>
</div>

"FAKE - F# Make" is intended to be an extensible build framework and therefor it should be as easy as possible to create custom tasks. 
This tutorial shows how to create a (very simple) custom task in C#.

## Creating a custom task

Open Visual Studio and create a new C# class library called my MyCustomTask and create a class called RandomNumberTask:

	[lang=csharp]
	using System;

	namespace MyCustomTask
	{
		public class RandomNumberTask
		{
			public static int RandomNumber(int min, int max)
			{
				var random = new Random();
				return random.Next(min, max);
			}
		}
	}

## Using the custom task

Compile the project and put the generated assembly into the *tools/FAKE* path of your project. Now you can use your CustomTask in the build script:


	// include Fake libs
	#I @"tools\FAKE"
	#r "FakeLib.dll"

	// include CustomTask
	#r "MyCustomTask.dll"
	open Fake 

	// open CustomNamespace
	open MyCustomTask

	// use custom functionality
	RandomNumberTask.RandomNumber(2,13)
	  |> tracefn "RandomNumber: %d"

If you want to use FAKE's standard functionality (like [globbing](http://en.wikipedia.org/wiki/Glob_(programming))) within your CustomTask project, just reference FakeLib.dll and [explore the FAKE namespace](apidocs/v5/legacy/index.html).
