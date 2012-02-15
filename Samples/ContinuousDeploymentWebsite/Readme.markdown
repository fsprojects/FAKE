## FAKE.Deploy - Sample app

   Run 03_deploy.bat

This will do the following:

* It starts the 03_deploy.fsx with FAKE
* It compiles the website project
* It compiles the test project
* It runs the tests with mspec
* It creates a nuget package containing:
  * the website itself
  * the install.fsx file and the referenced FAKE libs
  * Cassini - a mini webserver
* It starts the (local) deploy process, which will do the following:
  * Unpack the package in ./Work/
  * Copy the website into a separate folder
  * Start Casssini on that folder