# Deployment using FAKE

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>Fake.Deploy is no longer part of FAKE 5 and is considered obsolete and fully replaced by modern deployment systems (puppet, chef, PowerShell DSC, ...). <a href"https://github.com/fsharp/FAKE/issues/1820">Announcement</a>
    You can still use fake scripts on those alternative deployment systems. Just download the fake binaries and run your scripts.</p>
</div>

This introduction assumes Fake.Deploy.exe is available in the current directory or path.

## Introduction

The FAKE deployment tool allows users to deploy applications to remote computers and to run scripts on these remote agents. A typical scenario maybe as follows:


* Build an application -> run tests -> create artifacts and save on build server (Classical FAKE build workflow)
* Extract artifacts from build server and create a NuGet deployment package
* Push the NuGet package to the desired computer this will run the package's FAKE script on the remote machine

## Installing Fake deployment services

In order to deploy application to a remote computer a deployment agent needs to be running on that server.

The simplest way to install the agent on a remote server is to use the [Nuget command line tool](http://docs.nuget.org/consume/installing-nuget) to download the FAKE nuget and extract the binaries into a _fake/tools_ subfolder.  Once  _nuget.exe_ is available on the path, use the following command in a command shell:

    [lang=batchfile]
    nuGet.exe Install FAKE -ExcludeVersion

Adding the _.../fake/tools_ folder to the path will simplify executing the Fake.Deploy.exe.

To run an agent in a console, simply run:

    Fake.Deploy

To install a windows service on that agent:

   * Open a command prompt with Administrator Privilege
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

   * Open a command prompt with Administrator Privilege
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

    Fake.Deploy /deploy C:/Appdev/MyDeployment.nupkg

To run the same package on a remote computer (e.g. integration-1) you can run:

    Fake.Deploy /deployRemote http://integration-1:8080/fake C:/Appdev/MyDeployment.nupkg

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

## Security
To turn on authentication in Fake.Deploy set the configuration value for 'Authorization' to 'On' (default is 'Off') in fake.deploy.config.
When you set this value to 'On' you must also set the configuration key 'AuthorizedKeysFile' to point to a file that that contains the mapping between keys and usernames.

    <add key="Authorization" value="On" />
    <add key="AuthorizedKeysFile" value="c:\fake_deploy\authorized_keys" />

If you deploy from a fake buildscript you need to make a call to `FakeDeployAgentHelper.authenticate` before you perfrom any other call to the agent.
`authenticate` takes 4 parameters, `server`, `userId`, `pathToPrivateKey` and the `password` for the private key

### AuthorizedKeysFile
Is a rsa-pub key file where each line represents one user.

    ssh-rsa AAAAB3NzaC1yc2EAAAABJ_some_parts_removed_E91p+8JLFCaF3tLc8Aw== Test@Fake.org

Each row has 3 values separated by space

    * which type of key it is, it must be 'ssh-rsa'
    * public key in base64 format
    * username the key maps to.

### How to authorize against Fake.Deploy
First you must perform a HTTP-GET to:
        fake/login/yourUserId

From the response from this request, you take the body and base64 decode it, then you sign that value with your private key.
Then you submit a HTTP-POST to
        /fake/login.
There are 2 values you need to supply in the HTTP-POST.

1. `challenge` which should have the same base64 encoded value you received in the body of get fake/login/userId.
2. `signature` which should contain the signature you created using the `challenge` value and your private key.

The body of the response from the HTTP-POST contains a guid, this guid should be passed in the header with the name `AuthToken`.
To logout make a HTTP-GET to:
        fake/logout


### How do I generate a key on Windows?
Get [PuTTYgen](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html).
Run it.
Make sure the key is 'SSH-2 RSA' and number of bits is (at least) 2048.
Click `Generate` and follow the instructions to genereate the key.
When the key is generated, change `Key comment` to be your username and set a passphrase.
Then save the public key, you need to put this into Fake.Deploy's authorized keys file.
To save the private key, click `Conversions` menu option and then click `Export OpenSSH key`

### Linux
 ssh-keygen -t rsa -b 2048 -N password
