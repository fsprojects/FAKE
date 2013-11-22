#FAKE.Deploy Management Website

## Features ##

  * Agent Management
  * Environment Management
  * User Management
  * Action Auditing (coming soon...)

## Application Setup

When the application is first started it will go to a one time setup view.

Here you will be asked for the Admin account credentials and also which provider you wish to use (see below for per provider setup details)




## Debugging FAKE.Deploy ##

Go to the solution property page and configure Visual Studio to start multiple projects:

  * Fake.Deploy
  * Fake.Deploy.Web

Additionally, you need to make sure the desired data provider is built and available in the ~/App_Data/Providers directory. Otherwise you may get missing method exceptions.

### RavenDB Data Provider

The RavenDB data provider requires a running instance of raven. If you restore the nuget packages, this should also bring down
the Raven.Server package. So you can run Raven Server in console mode.

As an example to setup the RavenDB provider with a Raven server instance at localhost:8081 you would need to enter the following in the setup page.

	DataProvider -> RavenDB
	DataProviderParameters -> Url=http://localhost:8081
	MembershipProvider -> RavenDB
	MembershipProviderParameters -> Url=http://localhost:8081

### File Data Provider

  DataProvider -> File
  DataProviderParameters -> datafolder=C:\Data
  MembershipProvider -> File
  MembershipProviderParameters -> datafolder=C:\Data

