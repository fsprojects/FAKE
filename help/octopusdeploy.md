# Automating Deployment using FAKE and Octopus Deploy

[Octopus Deploy](http://octopusdeploy.com/) is a great tool for simple and user-friendly release management.

## Installing Octopus Deploy

You can download the [free community edition](http://octopusdeploy.com/purchase) of Octopus Deploy from [http://octopusdeploy.com/downloads](http://octopusdeploy.com/downloads) - and then follow the [Installation Instructions](http://octopusdeploy.com/documentation/install/octopus) to get yourself up and running.

You will also need to install and configure a [Tentacle](http://octopusdeploy.com/documentation/install/tentacle) which you will deploy your software and services to.

## Octopus Deploy HTTP API and Octopus Tools

Octopus Deploy has a REST-style [HTTP API](http://octopusdeploy.com/documentation/api) available at `http://your-octopus-server/api` which we will be using via the [Octopus Tools](https://github.com/OctopusDeploy/Octopus-Tools), controlled from a FAKE script.

You should add the [OctopusTools NuGet](http://www.nuget.org/packages/OctopusTools/) package to your solution, which you can also [resolve from a FAKE script](nuget.html) - which you will need in order to use the [OctoTools](apidocs/fake-octotools.html) from a FAKE script.