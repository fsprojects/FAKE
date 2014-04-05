# Compiling TypeScript applications

FAKE can be used to build a variety of different application types. 
In this tutorial we are looking at the TypeScript support.

Consider a greetings.ts file:

    [lang=typescript]
    interface Person {
        firstname: string;
        lastname: string;
     }
     
     function greeter(person : Person) {
         return "Hello, " + person.firstname + " " + person.lastname;
     }

     var user = {firstname: "Jane", lastname: "User"};

     document.body.innerHTML = greeter(user);

## Running the TypeScript compiler

    

You can download the free professional edition of TeamCity from [http://www.jetbrains.com/teamcity/](http://www.jetbrains.com/teamcity/). After the installation process you should be ready to configure your first build.

## Creating a FAKE project on TeamCity

Now create a new project and add a build configuration:

![alt text](pics/teamcity/createproject.png "Creating a FAKE project on TeamCity")

You also need to set the artifacts paths:

![alt text](pics/teamcity/generalsettings.png "Set the settings")

## Attach a VCS root

The next step is to attach a VCS root. For this sample we will use the official FAKE repository at [https://github.com/forki/FAKE/](https://github.com/forki/FAKE/).

![alt text](pics/teamcity/vcsroot.png "Set up the VCS root")
![alt text](pics/teamcity/auth.png "Set up the VCS root authentication")
![alt text](pics/teamcity/checkoutrules.png "Set up the VCS root checkout rules")

## Creating the build step

Now is the time to create two build steps. The first one downloads FAKE via Nuget and the second one runs the build script:

![alt text](pics/teamcity/buildstep1.png "Download FAKE")
![alt text](pics/teamcity/buildstep2.png "Run FAKE script")

If you want you could also add a build trigger to your build script:

![alt text](pics/teamcity/trigger.png "Set up the build trigger")

## Running the build

Now if everything is configured correctly, you can run your build and the output should look like:

![alt text](pics/teamcity/output.png "Build output")

You can also inspect the NUnit results:

![alt text](pics/teamcity/nunit.png "NUnit results")