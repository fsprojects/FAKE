FAKE v6 is finally here -- this new major release lays the foundation for future releases of FAKE
and move a step closer to complete the transition that started in FAKE v5. We have removed the
obsolete API's and replaced there usages with alternatives, updated dependencies, polished some
API's, and refreshed the website!

![FAKE v6 Home Page Hero Section]({{root}}content/img/fakev6-release/fake-v6-hero-section.png)

The above screenshot is from the new FAKE website!

FAKE v6 focuses on FAKE modules and repository. We wanted this release to lay foundation for future
development and releases of FAKE to be much easier and streamlined. In addition, this release move one
step closer to complete the transition that started from FAKE v5 to move FAKE codebase to a modular approach.

The main points that this release addresses are the following:
1. Update dependencies of repository.
2. Remove obsolete APIs and replace their usage with alternative suggested API's,
3. FAKE build enhancements,
4. Update FAKE website to use new FSDocs library and give it a new fresh look,
5. General Enhancements
6. Add Fantomas tool to FAKE codebase as a styling and formatter tool.

Following sections explain each point in more details.


## Update dependencies of repository

FAKE was lacking behind the tools it uses as dependencies, especially, F# compiler services and Paket.
FAKE v6 cleans the dependencies in `paket.dependencies` and upgrade them to newer versions. However,
not every upgrade is smooth and direct! After upgrading dependencies, including F# compiler services 
to latest major version, 41. The runner using reference assemblies for Net Standard 2.0 stopped working.

As a recap, in FAKE v5 we supported .NET 6 reference assemblies in FAKE runner in addition to .NET STANDARD
2.0, so uses can opt-in to use .NET 6 references assemblies when running scripts using FAKE runner. However,
updating the F# compiler services dependency, broke the support for .NET STANDARD 2.0 reference assemblies in
the runner. We tried to solve it, but with no luck. The error we get is the following:  

```fsharp
MissingMethodException: Method not found: 'Void Microsoft.FSharp.Core.PrintfFormat`5..ctor(System.String)'.. Actual value was false but had expected it to be true.
```

The error is a strange one since the `printfn` method in F# didn’t change over the years, so not finding it is a 
bit strange. We debugged it but didn’t reach a point, **so we decided to drop the support for Net Standard reference 
assemblies from runner**. This move has the effect of requiring that, .NET SDK v6 to exists on the machine to be able
to use FAKE runner.

By that, FAKE runner will have a minimum requirement of .NET v6 SDK. This new requirement has been added to the README 
file of the repository under a new section called *requirements*. Also, we updated the contribution guide to add the
other possible ways that FAKE modules can be used in addition to using FAKE runner. These new approaches are being
adopted by community to overcome some limitations on the runner. Especially, when upgrading to a new version of .NET.

> We choose .NET 6 reference assemblies since .NET 6 is the current LTS release of .NET


## Obsolete APIs

A lot of modules in FAKE uses obsolete API's from other part of the codebase, especially from the process module.
FAKE v6 removed the obsolete API's in the process module and replace their usage in other parts of the codebase with
alternatives.

Not only API's from process module were removed, but other API's that were marked with obsolete attribute as well.
We have documented all these changes and grouped them by module. Please refer to the last section on this page
to see the changes in each module [<ins>[Modules Changes](#Modules-Changes)</ins>].


## FAKE Build Enhancements

The build file in FAKE was cluttered by a lot of things that are now not used, so dealing with the file is somehow
a little hard. For example, the script supports running the build on multiple CI providers, which was the case before
we switch to GitHub actions. But now the support for other providers is not needed, hence we can remove a good amount of
unused code. This with a long of other changes we did to the script. 

Another major change was to remove all the handling for `AssemblyInfo.fs` files from script and modules. In which FAKE now uses
a `Directory.Build.props` file for build information instead of scattered `AssemblyInfo.fs` files. The main reason we did this change
was that, when we open an `AssemblyInfo.fs` file in one of the modules, the information is old and irrelevant! - the dates and the assembly
version. They are from the first time the module is published. The reader need to go to build script to understand what is going on
and how new data is set. In addition, the build script modify the GIT tree to remove these files from GIT when adding the new information
and then revert changes again once done. The `Directory.Build.props` file removes all of these steps in favor of one file that .NET
take and use in build, and the file is always up-to-date with information from build script directly.

Following points present the changes went in FAKE's `build.fsx` file and build process of FAKE in general:

1. Added `Directory.Build.props` file at the root of the repository as a root level MSBuild properties file for projects in FAKE.sln
This file include top level MSBuild parameters, including:
    1. Property `SourceRoot` which is set root directory of FAKE repository
    2. `SourceRevisionId` which is set to current date and time. Used in FAKE runner hints
    3. `IncludeSourceRevisionInInformationalVersion` which is set to `true`. This field is used in combination with `SourceRevisionId` 
        which is set to current date and time. So MSBuild append the value in `SourceRevisionId` to `Version` property when specified
        - before we used NuGet version which has build date info as well. In FAKE, we have used `SourceRevisionId` to only set build 
        time to be used in FAKE runner hints., but to be available in runtime for FAKE runner. 
        [<ins>Please see this link for relation between them</ins>](https://docs.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#generateassemblyinfo)
    4. Add the FAKE logo to output NuGet packages
    5. Add README file to output NuGet packages
2. Added another `Directory.Build.props` in `src\app` with specific information for FAKE source projects. This file include
properties for NuGet pack to use, like project description, authors, logo, README, etc..
Guidelines in [<ins>this link</ins>](https://docs.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices) were followed in the process
3. Removed all the `AssemblyInfo.fs` files in projects in FAKE.sln in favor of `Directory.Build.props` files added.
4. Since now we have removed `AssemblyInfo` files from FAKE projects, we need to remove any code that reference them, following are the affected places:
    1. Target module, which uses information in `AssemblyInfo` to display version of target module in FAKE. Replaced with
        
        ```fsharp
        let targetAssembly = Assembly.GetAssembly(typeof<Target>)
        printfn "Target Module Version: %s" (targetAssembly.GetName().Version.ToString())
        ```
        
    2. `Fake.Runtime.Environment` which contains a reference to `AssemblyInfo` file to display FAKE version, replaced with:
        
        ```fsharp
        fakeContextAssembly().GetName().Version.ToString()
        ```
        
    3. `Fake.Runtime.FakeRuntimeHints` which displays the upgrade hint for FAKE runner. Now this gets the build time of the assembly. To get assembly
    build time we have added the `SourceRevisionId`  variable in build (see point #1 b and c) above, and extract it in FAKE hint file.
5. Enhanced `build.fsx` script targets as follows:
    1. Removed targets: 
        
        `SetAssemblyInfo`
        `UnskipAssemblyInfo`
        `UnskipAndRevertAssemblyInfo`
        `Release_Staging`
        
    2. Renamed targets:
        
        `BootstrapTestDotNetCore` → `BootstrapFake`
        `DotNetCoreCreateChocolateyPackage` → `DotNetCreateChocolateyPackage`        
        `DotNetCorePushChocolateyPackage` → `DotNetPushChocolateyPackage`
        `DotNetCoreCreateDebianPackage` → `DotNetCreateDebianPackage`
        `_DotNetPackage` → `DotNetCreateNuGetPackage`
        `DotNetCorePushNuGet` → `DotNetPushToNuGet`
        `PrintColors` → `BootstrapFake_PrintColors` 
        `FailFast` → `BootstrapFake_FailFast` 
        `_StartDnc` → `BeforeBuild`
        `_AfterBuild` → `AfterBuild`
        `FastRelease` → `GitHubRelease` 
    3. New targets:
        `CheckFormatting`
    4. Removed `AssemblyInfo` variables and related code in favor of `Directory.Build.props`
    5. Removed code for other build servers handling since now we use GitHub actions only for releases
6. Removed the Net475 target from FAKE projects, since `Netstandard` 2.0 already covers it. Please check [this link](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
7. Update `Fake-choco-template` file to use new FAKE logo image.


## FAKE new Website

During the dependency update, we faced an issue in FSDocs (formerly FSFormatting). FAKE was using 4.x version of the
library, and currently released version is 16.x! The library has changed a lot in which a lot of the steps FAKE
uses to generate the website were removed from the library. For instance, using Razor pages as templates, the API and 
command line usage.

This push to a major re-implementation and re-design of the FAKE website from the ground up. We took the chance to do all
of that during the update of FSDocs library!

So, from FAKE v6, we now have a new module; `Fake.DotNet.Fsdocs`, and `Fake.DotNet.FSFormatting` has been removed.
The new module uses the new API from FSDocs and it is used in generating the new website.

The stack and details of the new website are added to the [<ins>contribution guide page</ins>](/guide/contributing.html#Contributing-Documentation), please refer to that page to get
more details new site.

One other major detail is that, FSDocs now defaults to XML syntax as a documentation blocks for code.
On the other hand, FAKE codebase uses markdown syntax. Which caused problems in generating the new site
and F# best practice now uses XMl syntax as a documentation blocks as explained in 
[Recommended XML doc extensions for F# documentation tooling](https://github.com/fsharp/fslang-design/blob/main/tooling/FST-1031-xmldoc-extensions.md).
So we took the chance once again to convert the documentation from markdown syntax to XML syntax. 
For example, a doc block like this one:
```f#
/// Creates a GitHub Release for the specified repository and tag name
    /// Creates a draft GitHub Release for the specified repository and tag name
    /// ## Parameters
    /// - `owner` - the repository's owner
    /// - `repoName` - the repository's name
    /// - `tagName` - the name of the tag to use for this release
    /// - `prerelease` - indicates whether the release will be created as a prerelease
    /// - `notes` - collection of release notes that will be inserted into the body of the release
    /// - `client` - GitHub API v3 client
    ///
    /// # Sample
    /// Target.create "GitHubRelease" (fun _ ->
    ///            let token =
    ///                match Environment.environVarOrDefault "github_token" "" with
    ///                | s when not (System.String.IsNullOrWhiteSpace s) -> s
    ///                | _ -> failwith "please set the github_token environment variable to a github personal access token with repro access."
    ///
    ///            let files =
    ///                runtimes @ [ "portable"; "packages" ]
    ///                |> List.map (fun n -> sprintf "release/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)
    ///
    ///            GitHub.createClientWithToken token
    ///            |> GitHub.draftNewRelease gitOwner gitName release.NugetVersion (release.SemVer.PreRelease &lt;&gt; None) release.Notes
    ///            |> GitHub.uploadFiles files
    ///            |> GitHub.publishDraft
    ///            |> Async.RunSynchronously)
```

Is converted to the following XML syntax:
```f#
/// <summary>
/// Creates a draft GitHub Release for the specified repository and tag name
/// </summary>
///
/// <param name="owner">The repository's owner</param>
/// <param name="repoName">The repository's name</param>
/// <param name="tagName">The name of the tag to use for this release</param>
/// <param name="prerelease">Indicates whether the release will be created as a prerelease</param>
/// <param name="notes">Collection of release notes that will be inserted into the body of the release</param>
/// <param name="client">GitHub API v3 client</param>
/// <example>
/// <code lang="fsharp">
///         Target.create "GitHubRelease" (fun _ ->
///            let token =
///                match Environment.environVarOrDefault "github_token" "" with
///                | s when not (System.String.IsNullOrWhiteSpace s) -> s
///                | _ -> failwith "please set the github_token environment variable to a github personal access token with repro access."
///
///            let files =
///                runtimes @ [ "portable"; "packages" ]
///                |> List.map (fun n -> sprintf "release/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)
///
///            GitHub.createClientWithToken token
///            |> GitHub.draftNewRelease gitOwner gitName release.NugetVersion (release.SemVer.PreRelease &lt;&gt; None) release.Notes
///            |> GitHub.uploadFiles files
///            |> GitHub.publishDraft
///            |> Async.RunSynchronously)
/// </code>
/// </example>
```

## General Enhancements

1. Removed the following files or directories since they are not used;
    1. `[fake.sh](http://fake.sh)` file
    2. `release-website.cmd` file 
    3. `.circleci` directory
2. Enhance documentation pages and fix any broken links on them.
3. Enhanced Chocolatey NuSpec file `Fake-choco-template.nuspec` by using new logo image of FAKE and referencing repository under FSProjects organization
4. Added publishing of build artifacts to `build_and_test` GitHub workflow to be able to get artifacts, especially website for build actions.


## FAKE now uses Fantomas as a styling tool

FAKE v6 adds [<ins>Fantomas</ins>](https://fsprojects.github.io/fantomas/docs/index.html) as a styling and code formatter tool.
The tool uses the official F# style guide. Additions to FAKE repository include a new build target to check formatting of the codebase,
the `CheckFormatting` target. More details are added to [<ins>contribution guide page</ins>](/guide/contributing.html#Style-Guidelines), 
please refer to that page to get more details.

## Modules Changes

Following sub-sections present changes made to FAKE modules.

<details>
<summary>Modules Changes:</summary>

### `Fake.Api.HockeyApp`

→ Removed the module since HockeyApp has been discontinued and now Microsoft has replaced it with AppCenter.

### `Fake.Api.GitHub`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs 

### `Fake.Api.Slack`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Removed `System.Net.Http` from proj file since it was used for Net Framework. Now module uses `System.Net`

### `Fake.Azure.CloudServices`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs 

→ Replaced `Process.shellExec` with `CreateProcess` API

### `Fake.Azure.Emulators`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Process.execSimple` with `CreateProcess` API

### `Fake.Azure.Kudu`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs 

→ Enahnce code by using a type for Zip deploy parameters

→ Replaced `Process.execWithResult` with `CreateProcess` API

### `Fake.Azure.WebJobs`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs 

→ Removed `System.Net.Http` from proj file since it was used for Net Framework. Now module uses `System.Net`

### `Fake.Build.CMake`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Changed `Fake.Build.CMake.getGenerateArguments` and make it `internal` in favor of removing omit comment and replaced with visible to.

### `Fake.BuildServer.AppVeyor`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Process.quoteIfNeeded` with `StringBuilder.appendQuotedIfNotNull` since `quoteIfNeeded` is deprecated

→ Removed `PullRequestRepoName` environment variable since it is not defined by AppVeyor anymore

→ Removed `PullRequestRepoBranch` environment variable since it is not defined by AppVeyor anymore

### `Fake.BuildServer.Bitbucket`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Removed `RepoOwner` environment variable since it is not defined by Bitbucket anymore

### `Fake.BuildServer.GitHubActions`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.BuildServer.GitLab`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.BuildServer.TeamCity`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.BuildServer.TeamFoundation`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs 

→ Enhance code by adding a new type `LogDetailData` to hold internal data before logging instead of calling internal method with 10 or more parameters. Now it uses a type

### `Fake.BuildServer.Travis`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Core.CommandLineParsing`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Core.Context`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Removed `Fake.Core.Context.getFakeVar` since it is deprecated, alternative is `Fake.Core.FakeVar.getFakeVar`

→ Removed `Fake.Core.Context.removeFakeVar` since it is deprecated, alternative is `Fake.Core.FakeVar.removeFakeVar`

→ Removed `Fake.Core.Context.setFakeVar` since it is deprecated, alternative is `Fake.Core.FakeVar.setFakeVar`

→ Removed `Fake.Core.Context.fakeVar` since it is deprecated, alternative is `Fake.Core.FakeVar.fakeVar`

→ Removed `Fake.Core.Context.fakeVarAllowNoContext` since it is deprecated, alternative is `Fake.Core.FakeVar.fakeVarAllowNoContext`

### `Fake.Core.DependencyManager.Paket`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Core.Environment`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Removed `Fake.Core.BuildServer.verbose` since it is deprecated, alternative is `Trace.isVerbose ()` 

→ Removed `Fake.Core.Environment.environVarsWithMode` since it is deprecated, alternative is `Fake.Core.Environment.environVars` 

→ Removed `Fake.Core.Environment.setBuildParam` since it is deprecated, alternative is `Fake.Core.Environment.setEnvironVar` 

→ Removed `Fake.Core.Environment.hasBuildParam` since it is deprecated, alternative is `Fake.Core.Environment.hasEnvironVar` 

→ Removed `Fake.Core.Environment.getBuildParamOrDefault` since it is deprecated, alternative is `Fake.Core.Environment.environVarOrDefault` 

→ Removed `Fake.Core.Environment.getBuildParam` since it is deprecated, alternative is `Fake.Core.Environment.environVarOrDefault` 

→ Removed `Fake.Core.Environment.isDotnetCore` since it is deprecated, alternative is `Fake.Core.Environment.isDotNetCore`

### `Fake.Core.FakeVar`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Core.Process`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Removed `Fake.Core.ProcStartInfo.Empty` since it is deprecated, alternative is `ProcStartInfo.Create`

→ Removed `Fake.Core.CreateProcess.fromRawWindowsCommandLine` since it is deprecated, alternative is `Fake.Core.CreateProcess.fromRawCommandLine`

→ Removed `Fake.Core.Process.startProcess` since it is deprecated, alternative is `Fake.Core.Process.rawStartProcess`

→ Removed `Fake.Core.Process.getProc` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.execRaw` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.execWithResult` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.execSimple` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.execElevated` since it is deprecated, no alternative since it is not possible in .NET Core anymore

→ Removed `Fake.Core.Process.fireAndForget` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.directExec` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.start` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.quote` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.quoteIfNeeded` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.toParam` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.UseDefaults` since it is deprecated, alternative is built-in `id`

→ Removed `Fake.Core.Process.stringParam` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.multipleStringParams` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.optionParam` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.boolParam` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.parametersToString` since it is deprecated, alternative is `Arguments` and `Args` modules/types

→ Removed `Fake.Core.Process.findFiles` since it is deprecated, alternative is `ProcessUtils.findFiles`

→ Removed `Fake.Core.Process.tryFindFile` since it is deprecated, alternative is `ProcessUtils.tryFindFile`

→ Removed `Fake.Core.Process.findFile` since it is deprecated, alternative is `ProcessUtils.findFile`

→ Removed `Fake.Core.Process.findFilesOnPath` since it is deprecated, alternative is `ProcessUtils.findFilesOnPath`

→ Removed `Fake.Core.Process.tryFindFileOnPath` since it is deprecated, alternative is `ProcessUtils.tryFindFileOnPath`

→ Removed `Fake.Core.Process.appSettings` since it is deprecated, no alternative since it is not possible in .NET Core anymore

→ Removed `Fake.Core.Process.tryFindTool` since it is deprecated, alternative is `ProcessUtils.tryFindTool`

→ Removed `Fake.Core.Process.tryFindPath` since it is deprecated, alternative is `ProcessUtils.tryFindPath`

→ Removed `Fake.Core.Process.findPath` since it is deprecated, alternative is `ProcessUtils.findPath`

→ Removed `Fake.Core.Process.asyncShellExec` since it is deprecated, alternative is `CreateProcess` API

→ Removed `Fake.Core.Process.killProcessById` since it is deprecated, alternative is `Fake.Core.Process.killById`

→ Removed `Fake.Core.Process.getProcessesByName` since it is deprecated, alternative is `Fake.Core.Process.getAllByName`

→ Removed `Fake.Core.Process.killProcess` since it is deprecated, alternative is `Fake.Core.Process.killAllByName`

→ Removed `Fake.Core.Process.ProcStartInfo` since it is deprecated, alternative is `Fake.Core.ProcStartInfo`

→ Removed `Fake.Core.Process.ExecParams` since it is deprecated, alternative is `Fake.Core.ExecParams`

→ Removed `Fake.Core.Process.ProcessResult` since it is deprecated, alternative is `Fake.Core.ProcessResult`

→ Removed `Fake.Core.Process.ConsoleMessage` since it is deprecated, alternative is `Fake.Core.ConsoleMessage`

→ Changed implementation of  `shellExec` to use `CreateProcess` API

→ Changed implementation of  `AsyncExec` to use `CreateProcess` API

### `Fake.Core.ReleaseNotes`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Core.SemVer`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Core.String`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Core.Target`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Removed `Fake.Core.TargetModule.DoNothing` since it is deprecated, alternative is `ignore`

→ Removed `Fake.Core.TargetModule.runAndGetContext` since it is deprecated, alternative is `Target.WithContext.run`

→ Removed `Fake.Core.TargetModule.Description` since it is deprecated, alternative is `Fake.Core.TargetModule.description`

→ Update target module version option to use reflection on target type to from Target module assembly instead of using informational version. Since now we use `Directory.Build.props` instead of `AssemblyInfo` files. 

### `Fake.Core.Tasks`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Core.Trace`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Removed `Fake.Core.Trace.openTag` since it is deprecated, alternative is `traceTag` and `use` to properly call `closeTag` in case of exceptions

→ Removed `Fake.Core.Trace.closeTag` since it is deprecated, alternative is `traceTag` and `use` to properly call `closeTag` in case of exceptions

→ Removed `Fake.Core.Trace.traceStartTarget` since it is deprecated, alternative is `traceTarget`  and `use` to properly call `traceEndTask` in case of exceptions

→ Removed `Fake.Core.Trace.traceEndTarget` since it is deprecated, alternative is `traceTarget`  and `use` to properly call `traceEndTask` in case of exceptions

→ Removed `Fake.Core.Trace.traceStartTask` since it is deprecated, alternative is `traceTask`  and `use` to properly call `traceEndTask` in case of exceptions

→ Removed `Fake.Core.Trace.traceEndTask` since it is deprecated

### `Fake.Core.UserInput`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Core.Vault`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Core.Xml`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Renamed `getChilds` to `getSubNodes` to match naming of related method `getSubNode`

→ Enabled and remove obsolete warning on `XslTransformer` , `XslTransform` & `XmlTransform` for Net Standard since they are now available for Net Standard.

### `Fake.Documentation.DocFx`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated.

→ Replaced `Process.execSimple` with `CreateProcess` API since it is deprecated.

### `Fake.DotNet.AssemblyInfo`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Removed `StringAttributeEx` since it was deprecated, alternative is `StringAttributeWithStatic`

### `Fake.DotNet.Cli`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Removed `Fake.DotNet.Cli.Preview2ToolingOptions` since it is deprecated

→ Removed `Fake.DotNet.Cli.LatestPreview4ToolingOptions` since it is deprecated

→ Removed `Fake.DotNet.Cli.RC4_004771ToolingOptions` since it is deprecated

→ Removed `Fake.DotNet.Cli.RC4_004973ToolingOptions` since it is deprecated

→ Removed `Fake.DotNet.Cli.Release_1_0_4` since it is deprecated

→ Removed `Fake.DotNet.Cli.Release_2_0_0` since it is deprecated

→ Removed `Fake.DotNet.Cli.Release_2_0_3` since it is deprecated

→ Removed `Fake.DotNet.Cli.Release_2_1_4` since it is deprecated

→ Removed `Fake.DotNet.Cli.Release_2_1_300_RC1` since it is deprecated

→ Removed `Fake.DotNet.Cli.Release_2_1_300` since it is deprecated

→ Removed `Fake.DotNet.DotNet.InfoOptions.Default` since it is deprecated, alternative is `Create`

→ Removed `Fake.DotNet.DotNet.RestoreOptions.Default` since it is deprecated, alternative is `Options.Create` 

→ Replaced `Process.execSimple` in `install` API with `CreateProcess` API

→ Removed `Fake.DotNet.DotNet.PublishOptions.Default` since it is deprecated, alternative is `PublishOptions.Create`

→ Removed `Fake.DotNet.DotNet.BuildOptions.Default` since it is deprecated, alternative is `BuildOptions.Create`

→ Removed `Fake.DotNet.DotNet.PackOptions.Default` since it is deprecated, alternative is `PackOptions.Create`

→ Removed `Fake.DotNet.DotNet.TestOptions.Default` since it is deprecated, alternative is `TestOptions.Create`

→ Removed `Fake.DotNet.DotNet.Options.Default` since it is deprecated, alternative is `Options.Create`

### `Fake.DotNet.Fsc`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `SourceCodeServices` with `FSharpDiagnostic` as a result of updating F# compiler services. And map `FSharpDiagnosticSeverity.Hidden` and `FSharpDiagnosticSeverity.Info` as warning messages

### `Fake.DotNet.FSFormatting`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Deprecated module APIs in favor of new module `Fake.DotNet.Fsdocs` which will support newest `fsdocs` tool

### `Fake.DotNet.Fsdocs`

→ New module added

→ Added `[Fsdocs.build](http://Fsdocs.build)` API to call build command of Fsdocs to process files and generate documentation

→ Added `[Fsdocs.watch](http://Fsdocs.watch)` API to call watch command of Fsdocs to watch generated documentation

### `Fake.DotNet.Fsi`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Replaced `SourceCodeServices` with `FSharpDiagnostic` as a result of updating F# compiler services. And map `FSharpDiagnosticSeverity.Hidden` and `FSharpDiagnosticSeverity.Info` as warning messages

→ Replaced `Process.findPath` in `pathToFsiExe` with `CreateProcess` API since it is deprecated

→ Replaced `Process.execWithResult` in `execRaw` with `CreateProcess` API since it is deprecated

### `Fake.DotNet.FxCop`

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.DotNet.ILMerge`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

### `Fake.DotNet.Mage`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Process.execSimple` in `call` with `CreateProcess` API since it is deprecated

### `Fake.DotNet.MSBuild`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Removed `BuildException` since it is replaced with `MSBuildException`

→ Removed `MSBuildExeFromVsWhere` since it is for legacy FAKE

→ Replaced `Process.tryFindFileOnPath` in `exactPathOrBinaryOnPath` with `ProcessUtils.tryFindFileOnPath` since it is deprecated 

→ Replaced `Process.tryFindFileOnPath` in `msBuildExe` with `ProcessUtils.tryFindFileOnPath` since it is deprecated 

→ Replaced `Process.tryFindFile` in `msBuildExe` with `ProcessUtils.tryFindFile` since it is deprecated

→ Removed `Fake.DotNet.MSBuildParams.Empty` since it is deprecated, alternative is `Fake.DotNet.MSBuildParams.Create`

→ Changed `Fake.DotNet.MSBuild.msBuildExe` and removed obsolete attribute since it is used in other areas in FAKE 

→ Renamed `xname` with `xName` to follow FAKE API guideline

→ Removed `processReferences` since it is deprecated

→ Changed `Fake.DotNet.MSBuild.getProjectReferences` and removed obsolete attribute since it is used in other areas in FAKE. Marked it as private

→ Removed `serializeMSBuildParams` since it is deprecated

→ Replaced `Process.execWithResult` and `Process.execSimple` with `CreateProcess` API since they are deprecated

→ Enhanced code quality by extracting MSBuild execution to an internal method instead of duplicating call

→ Replaced `Process.toParam` with `StringBuilder.appendQuotedIfNotNull` since `quoteIfNeeded` is deprecated

→ Opened `buildWithRedirect` as a public API

### `Fake.DotNet.NuGet`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Replaced `Process.execWithResult` and `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.DotNet.Paket`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.DotNet.Testing.Coverlet`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.DotNet.Testing.DotCover`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.DotNet.Testing.Expecto`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.DotNet.Testing.MSpec`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

### `Fake.DotNet.Testing.MSTest`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Changed `Fake.DotNet.Testing.MSTest.msTestExe` by marking it as internal

→ Replaced `Process.tryFindFile` with `ProcessUtils.tryFindLocalTool` since it is deprecated

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.DotNet.Testing.NUnit`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Replaced `Tools.findToolFolderInSubPath` and `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

→ Replaced `Process.execRaw` and `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.DotNet.Testing.OpenCover`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.DotNet.Testing.SpecFlow`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

→ Removed SpecFlow APIs for SpecFlow prior to v2.4, and replaced `SpecFlowNext` APIs with `SpecFlow`

### `Fake.DotNet.Testing.VSTest`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Changed VSTest module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

### `Fake.DotNet.Testing.XUnit2`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

→ Replaced `Process.execWithResult` and `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.DotNet.Xamarin`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

→ Replaced `Process.execWithResult` with `CreateProcess` API since they are deprecated

### `Fake.DotNet.Xdt`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Installer.InnoSetup`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.Installer.Squirrel`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

### `Fake.Installer.Wix`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.IO.FileSystem`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Replaced ref-cell operators with alternatives since they are deprecated

→ Removed `FileIncludes` module since it is deprecated. Including its `Include` and `SetBaseDir` APIs, alternative is `GlobbingPattern` module

→ Removed `Fake.IO.Globbing.Tools` module since it is deprecated and replaced with Process module. Including its `@@` operator and `findToolInSubPath`, `tryFindToolFolderInSubPath`, & `findToolFolderInSubPath` APIs

→ Added tracing statements - `Trace.traceVerbose` - to `Shell` module APIs

→ Removed `Fake.IO.Shell.CopyFile` since it is deprecated, alternative is `Fake.IO.Shell.copyFile`

→ Removed `Fake.IO.Shell.CopyFileIntoSubFolder` since it is deprecated, alternative is `Fake.IO.Shell.copyFileIntoSubFolder`

→ Removed `Fake.IO.Shell.CopyFileWithSubfolder` since it is deprecated, alternative is `Fake.IO.Shell.copyFileWithSubfolder`

→ Removed `Fake.IO.Shell.Copy` since it is deprecated, alternative is `Fake.IO.Shell.copy`

→ Removed `Fake.IO.Shell.CopyTo` since it is deprecated, alternative is `Fake.IO.Shell.copyTo`

→ Removed `Fake.IO.Shell.CopyCached` since it is deprecated, alternative is `Fake.IO.Shell.copyCached`

→ Removed `Fake.IO.Shell.Rename` since it is deprecated, alternative is `Fake.IO.Shell.rename`

→ Removed `Fake.IO.Shell.SilentCopy` since it is deprecated, alternative is `Fake.IO.Shell.silentCopy`

→ Removed `Fake.IO.Shell.CopyFiles` since it is deprecated, alternative is `Fake.IO.Shell.copyFiles`

→ Removed `Fake.IO.Shell.CopyDir` since it is deprecated, alternative is `Fake.IO.Shell.copyDir`

→ Removed `Fake.IO.Shell.CleanDir` since it is deprecated, alternative is `Fake.IO.Shell.cleanDir`

→ Removed `Fake.IO.Shell.CleanDirs` since it is deprecated, alternative is `Fake.IO.Shell.cleanDirs`

→ Removed `Fake.IO.Shell.DeleteDir` since it is deprecated, alternative is `Fake.IO.Shell.deleteDir`

→ Removed `Fake.IO.Shell.DeleteDirs` since it is deprecated, alternative is `Fake.IO.Shell.deleteDirs`

→ Removed `Fake.IO.Shell.AppendTextFilesWithEncoding` since it is deprecated, alternative is `Fake.IO.Shell.appendTextFilesWithEncoding`

→ Removed `Fake.IO.Shell.AppendTextFiles` since it is deprecated, alternative is `Fake.IO.Shell.appendTextFiles`

→ Removed `Fake.IO.Shell.CompareFiles` since it is deprecated, alternative is `Fake.IO.Shell.compareFiles`

→ Removed `Fake.IO.Shell.GeneratePatchWithFindOldFileFunction` since it is deprecated, alternative is `Fake.IO.Shell.generatePatchWithFindOldFileFunction`

→ Removed `Fake.IO.Shell.GeneratePatch` since it is deprecated, alternative is `Fake.IO.Shell.generatePatch`

→ Removed `Fake.IO.Shell.TestDir` since it is deprecated, alternative is `Fake.IO.Shell.testDir`

→ Removed `Fake.IO.Shell.TestFile` since it is deprecated, alternative is `Fake.IO.Shell.testFile`

→ Removed `Fake.IO.Shell.CopyRecursive` since it is deprecated, alternative is `Fake.IO.Shell.copyRecursive`

→ Removed `Fake.IO.Shell.CopyRecursiveTo` since it is deprecated, alternative is `Fake.IO.Shell.copyRecursiveTo`

→ Removed `Fake.IO.Shell.CopyRecursive2` since it is deprecated, alternative is `Fake.IO.Shell.copyRecursive2`

→ Removed `Fake.IO.Shell.MoveFile` since it is deprecated, alternative is `Fake.IO.Shell.moveFile`

→ Removed `Fake.IO.Shell.WriteConfigFile` since it is deprecated, alternative is `Fake.IO.Shell.writeConfigFile`

→ Removed `Fake.IO.Shell.ReplaceInFiles` since it is deprecated, alternative is `Fake.IO.Shell.replaceInFiles`

→ Removed `Fake.IO.Shell.RegexReplaceInFileWithEncoding` since it is deprecated, alternative is `Fake.IO.Shell.regexReplaceInFileWithEncoding`

→ Removed `Fake.IO.Shell.RegexReplaceInFilesWithEncoding` since it is deprecated, alternative is `Fake.IO.Shell.regexReplaceInFilesWithEncoding`

→ Changed `Fake.IO.File.getEncoding` by removing case for UTF7 since it is deprecated and marked as unsafe.

### `Fake.IO.Zip`

### `Fake.JavaScript.Npm`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Process.tryFindFileOnPath` with `ProcessUtils.tryFindFileOnPath` since it is deprecated

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.JavaScript.TypeScript`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Enhanced code by extracting logic to resolve TypeScript compiler to a dedicated method

→ Update TypeScript parameters default values to reflect compiler defaults, including `ECMAScript` from `ES3` to `ECMAScript.ESNext`, `ModuleGeneration` from `CommonJs` to `ModuleGeneration.ESNext`

### `Fake.JavaScript.Yarn`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Net.FTP`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Net.Http`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Changed internal `getAsync` to handle HTTP 404 errors gracefully and return a null string to indicate there is no content

### `Fake.netcore`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Runtime`

→ Replaced `SourceCodeServices` with `CodeAnalysis` and `Tokenization` as a result of updating F# compiler services

→ After upgrading F# compiler services, it will try to clean up the script directory after running the script, which will delete the `.fake/script.fsx` directory. A workaround has been impelmnted that let compiler compiles to a temporary directory, then we move the compiled assmebly to FAKE script directory, and then compiler can delete the temporary directory without affecting FAKE cache.

→ Replaced ref-cell operators with alternatives since they are deprecated

### `Fake.Sql.DacPac`

→ Deleted the module since it is not pushed to NuGet and was deprecated in favor of `Fake.Sql.SqlPackage` module which is a redesign of it

### `Fake.Sql.SqlServer`

→ Deleted the module since it is not pushed to NuGet and was deprecated in favor of `Fake.Sql.SqlPackage` module which is a redesign of it

### `Fake.Sql.SqlPackage`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Testing.Common`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Testing.Fixie`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Renamed `Fixie` API to `run` to follow FAKE API guidelines

 

### `Fake.Testing.ReportGenerator`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

### `Fake.Testing.SonarQube`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Tools.Git`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Changed `Branches` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `CommandHelper` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Replaced `Process.findPath` with `ProcessUtils.findPath` since it is deprecated

→ Replaced `Process.execWithResult` in `runGitCommand` with `CreateProcess` API since it is deprecated

→ Replaced `Process.fireAndForget` in `fireAndForgetGitCommand` with `CreateProcess` API since it is deprecated

→ Replaced `Process.directExec` in `directRunGitCommand` with `CreateProcess` API since it is deprecated

→ Changed `Commit` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `CommitMessage` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `FileStatus` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Information` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Merge` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Rebase` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Repository` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Fake.Tools.Git.Repository.fullclean` and marking it as obsolete since it is not honoring FAKE API guidelines. Alternative is `Fake.Tools.Git.Repository.fullClean`

→ Changed `Reset` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `SanityChecks` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `SHA1` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Staging` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Stash` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Changed `Submodule` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

### `Fake.Tools.GitVersion`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Changed `GitVersion` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

### `Fake.Tools.Octo`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Changed `Octo` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Replaced `Tools.findToolFolderInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

### `Fake.Tools.Pickles`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Replaced `Tools.findToolInSubPath` with `ProcessUtils.tryFindLocalTool` since it is deprecated

### `Fake.Tools.Rsync`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Changed write patch option from `--write-bratch=` to `--write-batch=` , fix type

→ Replaced `Process.execRaw` with `CreateProcess` API since they are deprecated

### `Fake.Tools.ServiceControl`

→ Removed module since it is obsolete

### `Fake.Tools.SignTool`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake.Windows.Chocolatey`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

→ Changed `Choco` module by adding `RequiredQualifiedAccess` attribute to follow FAKE API guideline

→ Replaced `Process.execSimple` with `CreateProcess` API since they are deprecated

### `Fake.Windows.Registry`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

→ Add API documentation to module APIs

### `Fake-cli`

→ Removed `AssemblyInfo.fs` file

→ Added Net6 target and removed net472 target

### `Fake.Tracing.NAntXml`

→ Removed module since it is deprecated and not added to FAKE solution

</details>


*That is all to it for FAKE v6, enjoy!*
