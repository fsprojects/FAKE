# Running canopy tests with FAKE

FAKE can be used to run a variety of different testing frameworks. 
In this tutorial we are looking at [Canopy](http://lefthandedgoat.github.io/canopy/) support.

![alt text](pics/canopy/logo.jpg "Running canopy tests from FAKE")

## Setup your canopy project
Consider a simple canopy `program.fs` file:

	#r "canopy.dll"

	open canopy
	open runner
	open System

	//overwrite default path
	canopy.configuration.phantomJSDir <- @".\"

	//start an instance of the browser
	start phantomJS

	"taking canopy for a spin" &&& fun _ ->

	    //go to url
	    url "http://localhost:81/canopy/testpages/"

	    //assert that the element with an id of 'welcome' has the text 'Welcome'
	    "#welcome" == "Welcome"

	//run all tests
	run()

	quit()

Although [Selenium](http://docs.seleniumhq.org/) (which is the framework behind canopy) supports all browsers you might want to run your tests with the headless browser PhantomJS. To grab the latest version just install the NuGet package: 

	install-package PhantomJS

Normally canopy loads `PhantomJS.exe` from `C:\` but in our case we want to use the installed one so we have to override the path in our test script and set the current path as location of `PhantomJS.exe`.


## Run canopy tests in FAKE
The target in FAKE basically hosts the website in IIS Express and starts the canopy tests. IIS Express requires a configuration template (`"iisexpress-template.config"`) which can be copied from `%ProgramFiles%\IIS Express\AppServer\applicationhost.config`. 

This sample target will require the [`FAKE.IIS package`](http://fsharp.github.io/FAKE/iis.html) to be installed and referenced in your script, though the package isn't required to run Canopy tests.

	Target "CanopyTests" (fun _ ->
		let hostName = "localhost"
		let port = 81

	    let config = createConfigFile(project, 1, "iisexpress-template.config", websiteDir + "/" + project, hostName, port)
	    let webSiteProcess = HostWebsite id config 1

	    let result =
	        ExecProcess (fun info ->
	            info.FileName <- (buildDir @@ "CanopyTests.exe")
	            info.WorkingDirectory <- buildDir
	        ) (System.TimeSpan.FromMinutes 5.)

	    ProcessHelper.killProcessById webSiteProcess.Id
	 
	    if result <> 0 then failwith "Failed result from canopy tests"
	)

Please note that HostWebsite starts the IIS Express process asynchronous and does NOT wait until the IIS Express successfully started. [Issue #403](https://github.com/fsharp/FAKE/issues/403)
