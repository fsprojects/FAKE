# Creating NuGet packages

## Creating a .nuspec template

The basic idea to create nuget packages is to create a .nuspec template and let FAKE fill out the missing parts.
The following code shows such .nuspec file from the [OctoKit](https://github.com/octokit/octokit.net) project.
	
	[lang=xml]
	<?xml version="1.0" encoding="utf-8"?>
	<package xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
	  <metadata xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">    
		<id>@project@</id>
		<version>@build.number@</version>
		<authors>@authors@</authors>
		<owners>@authors@</owners>
		<summary>@summary@</summary>
		<licenseUrl>https://github.com/octokit/octokit.net/blob/master/LICENSE.txt</licenseUrl>
		<projectUrl>https://github.com/octokit/octokit.net</projectUrl>
		<iconUrl>https://f.cloud.github.com/assets/19977/1441274/160fba8c-41a9-11e3-831d-61d88fa886f4.png</iconUrl>
		<requireLicenseAcceptance>false</requireLicenseAcceptance>
		<description>@description@</description>
		<releaseNotes>@releaseNotes@</releaseNotes>
		<copyright>Copyright GitHub 2013</copyright>    
		<tags>GitHub API Octokit</tags>
		@dependencies@
	  </metadata>
	</package>

## Setting up the build script

In the build script you need to create a target which executes the [NuGet task](apidocs/fake-nugethelper.html):

	Target "CreatePackage" (fun _ ->
	    // Copy all the package files into a package folder
		CopyFiles packagingDir allPackageFiles

		NuGet (fun p -> 
			{p with
				Authors = authors
				Project = projectName
				Description = projectDescription                               
				OutputPath = packagingRoot
				Summary = projectSummary
				WorkingDir = packagingDir
				Version = buildVersion
				AccessKey = myAccesskey
				Publish = true }) 
				"myProject.nuspec"
	)

There are a couple of interesting things happening here. In this sample FAKE created:
  * a copy of the .nuspec file
  * filled in all the specified parameters
  * created the NuGet package
  * pushed it to [nuget.org](http://www.nuget.org).