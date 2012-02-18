## FAKE.Deploy - Sample app

### Initial deployment

* Run 02_build.bat - This will start the build with FAKE:
	* It compiles the website project
	* It compiles the test project
	* It runs the tests with mspec
	* It creates a nuget package containing:
		* the website itself
		* the install.fsx file and the referenced FAKE libs
		* Cassini - a mini webserver.
* Run 03_listen.bat - This will start the Fake.Deploy agent as console.
* Run 04_deploy.bat - This will the deploy process by POSTing to http://localhost:8085/fake, which runs install.fsx:
	* Unpacks the package in ./Work/
	* Copies the website into a separate folder
	* Copies Cassini into a separate folder
	* Starts Casssini on the website folder
* Take a look at the deployed website - probably at http://localhost:32768/

### Pushing an update

* Increment the version no. in version.txt
* Run 02_build.bat
* Run 04_deploy.bat
* Now take another look at the deployed website