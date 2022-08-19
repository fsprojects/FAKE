# NuGet package restore

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help!</p>
</div>

If you are using a source control system like [git](http://git-scm.com/) you probably don't want to store all binary dependencies in it.
With FAKE you can use [NuGet](http://nuget.codeplex.com/) to download all dependent packages during the build.

## Setting the stage for NuGet

In order to download the packages during the build we need to add NuGet.exe to our repository.
You can download the "NuGet.exe Command Line Tool" from the [release page](https://github.com/NuGet/Home/releases).

## Restore packages from the build script

Modify your build script and add **RestorePackages()** near the beginning of the script.
This will use the default parameters to retrieve all NuGet packages specified in *"./\*\*/packages.config"* files.

If you need to use different parameters please use the [RestorePackage](apidocs/v5/legacy/fake-restorepackagehelper.html) task directly.

## Download latest version of FAKE via NuGet

If you don't want to store FAKE.exe and its components in your repository, you can use a batch file which downloads it before the build:

<pre><code class="language-bash">
    @echo off
    cls
    "tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
    "tools\FAKE\tools\Fake.exe" build.fsx
    pause
</code></pre>
# Creating NuGet packages

## Creating a .nuspec template

The basic idea to create nuget packages is to create a .nuspec template and let FAKE fill out the missing parts.
The following code shows such .nuspec file from the [OctoKit](https://github.com/octokit/octokit.net) project.

<pre data-src="nuspec-example.xml"><code class="language-xml">

</code></pre>
The .nuspec template contains some placeholders like `@build.number@` which can be replaced later by the build script.
It also contains some specific information like the copyright which is not handled by FAKE.

The following table gives the correspondence between the placeholders and the fields of the record type used by the NuGet task.

Placeholder | replaced by (`NuGetParams` record field)
:--- | :---
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
`@contentFiles@` | a list of [contentFiles](https://docs.microsoft.com/en-us/nuget/reference/nuspec#using-the-contentfiles-element-for-content-files) to be included in the nuget package
`@files@` | a list of source, target, and exclude strings for files to be included in the nuget package

## Setting up the build script

In the build script you need to create a target which executes the [NuGet task](apidocs/v5/legacy/fake-nugethelper.html):

```fsharp
open Fake.DotNet.NuGet

Target.create "CreatePackage" (fun _ ->
    copyFiles packagingOutputDirectory allFilesToPackage
    NuGet.NuGet (fun p ->
        { p with
            Version = buildVersion
            Authors = authors
            Project = projectName
            Summary = projectSummary
            Description = projectDescription
            WorkingDir = packagingDirectory
            OutputPath = artifactOutputDirectory
            AccessKey = myAccessKey
            Publish = true })
        "template.nuspec"
)
```

There are a couple of interesting things happening here. In this sample FAKE created:

* a copy of the .nuspec file
* filled in all the specified parameters
* created the NuGet package
* pushed it to [nuget.org](http://www.nuget.org) using the given `myAccessKey`.

## Handling package dependencies

If your project depends on other projects it is possible to specify these dependencies in the .nuspec definition (see also [Nuget docs](http://docs.nuget.org/docs/reference/nuspec-reference#Specifying_Dependencies_in_version_2.0_and_above)).
Here is a small sample which sets up dependencies for different framework versions:

```fsharp
open Fake.DotNet.NuGet

NuGet.NuGet (fun p ->
    { p with
        Authors = authors
        // fallback - for all unspecified frameworks
        Dependencies = [
            "Octokit", "0.1"
            "Rx-Main", NuGet.GetPackageVersion "./packages" "Rx-Main"]
        DependenciesByFramework = [
            { FrameworkVersion = "net40"
                Dependencies = [
                    "Octokit", "0.1"
                    "Rx-Main", NuGet.GetPackageVersion "./packages/" "Rx-Main"
                    "SignalR", NuGet.GetPackageVersion "./packages/" "SignalR" ]}
            { FrameworkVersion  = "net45"
                Dependencies = [
                    "Octokit", "0.1"
                    "SignalR", NuGet.GetPackageVersion "./packages/" "SignalR"]}
        ]
        // ...
        Publish = true })
    "template.nuspec"
```

## Explicit assembly references

If you want to have auxiliary assemblies next to the ones that get referenced by the target project, you can place  all the needed files in the `lib` directory and explicitly specify which of them should be referenced (see [Nuget docs](http://docs.nuget.org/docs/reference/nuspec-reference#Specifying_Explicit_Assembly_References_in_version_2.5_and_above)) via the `References` and `ReferencesByFramework` fields.
Here is a code snippet showing how to use these:

```fsharp
open Fake.DotNet.NuGet

NuGet.NuGet (fun p ->
    { p with
         Authors = authors
         // ...
         References = ["a.dll"]
         ReferencesByFramework =
             [{ FrameworkVersion  = "net40"; References = ["b.dll"]}
              { FrameworkVersion  = "net45"; References = ["c.dll"]}]
         // ...
         Publish = false })
    "template.nuspec"
```

## Explicit file specifications

If you want to specify exactly what files are packaged and where they are placed in the resulting NuGet package you can specify the Files property directly.  This is exactly like having the Files element of a nuspec filled out ahead of time.
Here is a code snippet showing how to use this:

```fsharp
open Fake.DotNet.NuGet

// Here we are specifically only taking the js and css folders from our project and placing them in matching target folder in the resulting nuspec.
// Note that the include paths are relative to the location of the .nuspec file
// See [Nuget docs](http://docs.nuget.org/docs/reference/nuspec-reference#Specifying_Files_to_Include_in_the_Package) for more detailed examples of how to specify file includes, as this follows the same syntax.
NuGet.NuGet (fun p ->
    {p with
        // ...
        Files = [
            (@"tools\**\*.*", None, None)
            (@"bin\Debug\*.dll", Some "lib", Some "badfile.css;otherbadfile.css")
        ]
        // ...
    })
    "template.nuspec"
```

## NuGet ContentFiles

ContentFiles in NuGet are static files that the NuGet client will make available to a project for inclusion in the project.  For more information see this [blog post](https://blog.nuget.org/20160126/nuget-contentFiles-demystified.html) explaining the difference and relationship to the Files element.

The ContentFiles param supports all the documented nuspec contentFiles [attributes](https://docs.microsoft.com/en-us/nuget/reference/nuspec#using-the-contentfiles-element-for-content-files).

It takes a value of type `list<string*string option*string option*bool option*bool option>` where each tuple part maps respectively to the following [attributes]((https://docs.microsoft.com/en-us/nuget/reference/nuspec#using-the-contentfiles-element-for-content-files)):

* include
* exclude
* buildAction
* copyToOutput
* flatten

Here is a code snippet showing how to use ContentFiles:

```fsharp
NuGet (fun p ->
    {p with
        // ...
        ContentFiles = [
            (@"**/*.md", Some @"**/Exclude/*.md", Some @"Content", None, None)
        ]
        // ...
    })
    "template.nuspec"
```
