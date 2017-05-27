# Creating NuGet packages

**Note:  This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help! **

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
		<iconUrl>https://github.com/octokit/octokit.net/icon.png</iconUrl>
		<requireLicenseAcceptance>false</requireLicenseAcceptance>
		<description>@description@</description>
		<releaseNotes>@releaseNotes@</releaseNotes>
		<copyright>Copyright GitHub 2013</copyright>    
		<tags>GitHub API Octokit</tags>
		@dependencies@
		@references@
	  </metadata>
	  @files@
	</package>

The .nuspec template contains some placeholders like `@build.number@` which can be replaced later by the build script.
It also contains some specific information like the copyright which is not handled by FAKE.

The following table gives the correspondence between the placeholders and the fields of the record type used by the NuGet task.

Placeholder | replaced by (`NuGetParams` record field)
--- | ---
`@build.number@` | `Version`
`@authors@` | `Authors`
`@project@` | `Project`
`@summary@` | `Summary`
`@description@` | `Description`
`@tags@` | `Tags`
`@releaseNotes@` | `ReleaseNotes`
`@copyright@` | `Copyright`
`@dependencies@` | a combination of `Dependencies` and `DependenciesByFramework`
`@references@` | a combination of `References` and `ReferencesByFramework`
`@files@` | a list of source, target, and exclude strings for files to be included in the nuget package

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
 * pushed it to [nuget.org](http://www.nuget.org) using the given `myAccessKey`.

## Handling package dependencies

If your project dependends on other projects it is possible to specify these dependencies in the .nuspec definition (see also [Nuget docs](http://docs.nuget.org/docs/reference/nuspec-reference#Specifying_Dependencies_in_version_2.0_and_above)). 
Here is a small sample which sets up dependencies for different framework versions:

        NuGet (fun p -> 
                {p with
                        Authors = authors
                        // ...
                        Dependencies =  // fallback - for all unspecified frameworks
					["Octokit", "0.1"
					"Rx-Main", GetPackageVersion "./packages/" "Rx-Main"]
                        DependenciesByFramework =
                                        [{ FrameworkVersion  = "net40"
                                        Dependencies = 
                                                ["Octokit", "0.1"
                                                 "Rx-Main", GetPackageVersion "./packages/" "Rx-Main"
                                                 "SignalR", GetPackageVersion "./packages/" "SignalR"]}
                                        { FrameworkVersion  = "net45"
                                        Dependencies = 
                                                ["Octokit", "0.1"
                                                 "SignalR", GetPackageVersion "./packages/" "SignalR"]}]
                        // ...
                        Publish = true }) 
                        "myProject.nuspec"

## Explicit assembly references

If you want to have auxiliary assemblies next to the ones that get referenced by the target project, you can place  all the needed files in the `lib` directory and explicitly specify which of them should be referenced (see [Nuget docs](http://docs.nuget.org/docs/reference/nuspec-reference#Specifying_Explicit_Assembly_References_in_version_2.5_and_above)) via the `References` and `ReferencesByFramework` fields.
Here is a code snippet showing how to use these:

    NuGet (fun p -> 
        {p with
            Authors = authors
            // ...
            References = ["a.dll"]
            ReferencesByFramework =
                [{ FrameworkVersion  = "net40"; References = ["b.dll"]}
                 { FrameworkVersion  = "net45"; References = ["c.dll"]}]
            // ...
            Publish = false })
            "template.nuspec"

## Explicit file specifications

If you want to specify exactly what files are packaged and where they are placed in the resulting NuGet package you can specify the Files property directly.  This is exactly like having the Files element of a nuspec filled out ahead of time.
Here is a code snippet showing how to use this:

	// Here we are specifically only taking the js and css folders from our project and placing them in matching target folder in the resulting nuspec.
	// Note that the include paths are relative to the location of the .nuspec file
	// See [Nuget docs](http://docs.nuget.org/docs/reference/nuspec-reference#Specifying_Files_to_Include_in_the_Package) for more detailed examples of how to specify file includes, as this follows the same syntax.
	NuGet (fun p ->
		{p with
			// ...
			Files = [
				(@"tools\**\*.*", None, None)
				(@"bin\Debug\*.dll", Some "lib", Some "badfile.css;otherbadfile.css")
			]
			// ...
		})
		"template.nuspec"
