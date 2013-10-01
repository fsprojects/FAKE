# Deployment using FAKE

    * Assumes Fake.Deploy.exe is available in the current directory or path.

## Introduction

The FAKE deployment tool allows users to deploy applications to remote computers and to run scripts on these remote agents. A typical scenario maybe as follows: 


* Build an application -> run tests -> create artifacts and save on build server (Classical FAKE build workflow)
* Extract artifacts from build server and create a NuGet deployment package
* Push the NuGet package to the desired computer this will run the package's FAKE script on the remote machine

## Installing Fake deployment services

In order to deploy application to a remote computer a deployment agent needs to be running on that server.

To run an agent in a console, simply run:
    
    Fake.Deploy

To install a windows service on that agent:
 
   * Open a command prompt with Administrator Priviledges
   * Run Fake.Deploy /install

By default the service starts a listener on port 8080. This can however be configured by editing the Fake.Deploy.exe.config file
and changing
    
    <add key="ServerName" value="localhost" />
    <add key="Port" value="8080" />

to the desired value. If you use the asterisk as port no. then Fake.Deploy will assign the first open port behind of 8080.

To ensure the service is running you can navigate to http://{computer}:{port}/fake/ and you should be presented with a page giving the 
status if the service

## Uninstalling Fake deployment services

To uninstall an agent

   * Open a command prompt with Administrator Priviledges
   * Run Fake.Deploy /uninstall     

## Running a FAKE Deployment Package

## Getting help

If you want to learn about Fake.Deploy's command line switches then run:

    Fake.Deploy /help

## Creating a Deployment package

Since Fake.Deploy uses Nuget packages for deployment you only need to create one of those and include a .fsx file in the root folder of the package.

Instructions for creating nuget packages can be found [at the NuGet document page](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package)  

## Running deployment

Fake deployment packages can be run manually on the current machine or they can be pushed to an agent on a remote machine.

To run a package on the local machine located at C:\Appdev\MyDeployment.nupkg you would run the following command:

    Fake.Deploy /deploy C:\Appdev\MyDeployment.nupkg
    
To run the same package on a remote computer (e.g. integration-1) you can run:

    Fake.Deploy /deployRemote http://integration-1:8080 C:\Appdev\MyDeployment.nupkg 

It's also possible to just make a HTTP-POST with the package to http://integration-1:8080/fake

This will push the directory to the given url. It is worth noting that the port may well be different, as this depends on the configuration of the 
listening agent (see. Installing Fake deployment service)

## Getting information about the deployments

    The following assumes you have Fake.Deploy running.

It's easy to get information about the deployments. Just make a HTTP request to server with:
    
    fake/deployments/                     -> gives all releases
    fake/deployments?status=active        -> gives all active releases
    fake/deployments/{app}                -> gives all releases of app
    fake/deployments/{app}?status=active  -> gives the active release of the app

## Rollback of releases

If you want to perform a rollback of a release so do a HTTP-PUT to:

    fake/deployments/{app}?version={version} -> rolls the app back to the given version
    fake/deployments/{app}?version=HEAD~2    -> relative rollback of the app (two versions earlier)