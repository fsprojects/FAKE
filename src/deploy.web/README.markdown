#FAKE.Deploy Management Website

## Features ##

  * Agent Management	
  * Environment Management
  * User Management
  * Action Auditing

## Application Setup

When the application is first started it will go to a one time setup view. 

Here you will be asked for the Admin account credentials and also which provider you wish to use (currently only raven db provider)

As an example to setup the RavenDB provider you would need to enter the following

	DataProvider -> RavenDB
	DataProviderParameters -> Url=http://localhost:8081
	MembershipProvider -> RavenDB
	MembershipProviderParameters -> Url=http://localhost:8081

  
## Debugging FAKE.Deploy ##

Go to the solution property page and configure Visual Studio to start multiple projects:

  * Fake.Deploy
  * Fake.Deploy.Web

### RavenDB Data Provider

The RavenDB data provider requires a running instance of raven. If you restore the nuget packages, this should also bring down
the Raven.Server package. So you can run Raven Server in console mode.  


## Building ##