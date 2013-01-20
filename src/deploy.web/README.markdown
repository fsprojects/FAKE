#FAKE.Deploy Management Website

## Features ##

  * Agent Management	
  * Environment Management
  * User Management
  * Action Auditing
  
## Debugging FAKE.Deploy ##

Go to the solution property page and configure Visual Studio to start multiple projects:

  * Fake.Deploy
  * Fake.Deploy.Web

### RavenDB Data Provider

The RavenDB data provider requires a running instance of raven. If you restore the nuget packages, this should also bring down
the Raven.Server package. 

Additionally for the RavenDBMembership provider to run it will need to be copied to the bin folder of web app, so it can be
resolved. 

## Building ##