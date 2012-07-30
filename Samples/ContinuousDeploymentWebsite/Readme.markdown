## FAKE.Deploy - Sample app

### Initial deployment

* Run 01_build.bat - This will start the build with FAKE:
	* It compiles the website project and the lib
	* It compiles the test project
	* It runs the tests with mspec
	* It creates a nuget package containing:
		* the website itself
		* the install.fsx file and the referenced FAKE libs
		* Cassini - a mini webserver.
* Run 02_listen.bat - This will start the Fake.Deploy agent as console which listens at [http://localhost:8085/fake/](http://localhost:8085/fake/). Try visting [http://localhost:8085/fake/deployments](http://localhost:8085/fake/deployments).
* Run 03_deploy.bat - This will the deploy process by POSTing to the agent, which will run install.fsx:
	* Unpacks the package in ./deployments/Fake_Website/active/
	* Copies the website into a separate folder
	* Copies Cassini into a separate folder
	* Starts Casssini on the website folder
* Take a look at the deployed website - probably at [http://localhost:32768/](http://localhost:32768/)

### Pushing an update

* Increment the version no. in version.txt
* Run 01_build.bat
* Run 03_deploy.bat
* Now take another look at the deployed website - it should show the new version no.
* The old package can be found in ./deployments/Fake_Website/backups/

### Rollback to version 0.1

* Run 04_rollback.bat
* Now take another look at the deployed website - should be rolled back to 0.1