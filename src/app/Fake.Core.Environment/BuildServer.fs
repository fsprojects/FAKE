namespace Fake.Core

open System

/// The server type option.
type BuildServer =
    | TeamFoundation
    | TeamCity
    | CCNet
    | Jenkins
    | Travis
    | AppVeyor
    | GitLabCI
    | GitHubActions
    | Bamboo
    | BitbucketPipelines
    | LocalBuild

/// The trace mode option.
type TraceMode =
    | Console
    | Xml

[<AbstractClass>]
type BuildServerInstaller () =
    abstract member Install : unit -> unit
    abstract member Detect : unit -> bool

/// <summary>
/// Contains functions which allow build scripts to interact with a build server.
/// </summary>
[<RequireQualifiedAccess>]
module BuildServer =
    /// <summary>
    /// A constant label for local builds
    /// </summary>
    /// [omit]
    let localBuildLabel = "LocalBuild"

    /// <summary>
    /// Defines the XML output file - used for build servers like CruiseControl.NET.
    /// This output file can be specified by using the *logfile* build parameter.
    /// </summary>
    let mutable xmlOutputFile = Environment.environVarOrDefault "logfile" "./output/Results.xml"

    /// <summary>
    /// Build number retrieved from Bamboo
    /// </summary>
    /// [omit]
    let bambooBuildNumber = Environment.environVar "bamboo_buildNumber"

    /// <summary>
    /// Checks if we are on Bamboo
    /// </summary>
    /// [omit]
    let isBambooBuild =
        String.IsNullOrEmpty bambooBuildNumber |> not

    /// <summary>
    /// Checks if we are on Team Foundation
    /// </summary>
    /// [omit]
    let isTFBuild =
        let tfbuild = Environment.environVar "TF_BUILD"
        not (isNull tfbuild) && tfbuild.ToLowerInvariant() = "true"

    /// <summary>
    /// Build number retrieved from Team Foundation
    /// </summary>
    /// [omit]
    let tfBuildNumber = Environment.environVar "BUILD_BUILDNUMBER"

    /// <summary>
    /// Build number retrieved from TeamCity
    /// </summary>
    /// [omit]
    let tcBuildNumber = Environment.environVar "BUILD_NUMBER"

    /// <summary>
    /// Build number retrieved from Travis
    /// </summary>
    /// [omit]
    let travisBuildNumber = Environment.environVar "TRAVIS_BUILD_NUMBER"

    /// <summary>
    /// Checks if we are on GitLab CI
    /// </summary>
    /// [omit]
    let isGitlabCI = Environment.environVar "CI_SERVER_NAME" = "GitLab CI" || Environment.environVar "GITLAB_CI" = "true"

    /// <summary>
    /// Build number retrieved from GitLab CI
    /// </summary>
    /// [omit]
    let gitlabCIBuildNumber =
        if isGitlabCI then
            // https://github.com/fsharp/FAKE/issues/2290
            let s = Environment.environVar "CI_PIPELINE_ID"
            if String.IsNullOrEmpty s then
                let id = Environment.environVar "CI_BUILD_ID"
                if isNull id then "" else id
            else s
        else ""

    // <summary>
    /// Checks if we are on GitHub Actions
    /// </summary>
    /// [omit]
    let isGitHubActionsBuild =
        Environment.environVarAsBoolOrDefault "GITHUB_ACTIONS" false

    /// <summary>
    /// Build number retrieved from GitHub Actions
    /// </summary>
    /// [omit]
    let gitHubActionsBuildNumber =
        if isGitHubActionsBuild
        then Environment.environVar "GITHUB_RUN_NUMBER"
        else ""

    /// <summary>
    /// Build number retrieved from Jenkins
    /// </summary>
    /// [omit]
    let jenkinsBuildNumber = tcBuildNumber

    /// <summary>
    /// CruiseControl.NET Build label
    /// </summary>
    /// [omit]
    let ccBuildLabel = Environment.environVar "CCNETLABEL"

    /// <summary>
    /// AppVeyor build number
    /// </summary>
    /// [omit]
    let appVeyorBuildVersion = Environment.environVar "APPVEYOR_BUILD_VERSION"

    /// <summary>
    /// The current build server
    /// </summary>
    let buildServer =
        if Environment.hasEnvironVar "JENKINS_HOME" then Jenkins
        elif Environment.hasEnvironVar "TEAMCITY_VERSION" then TeamCity
        elif not (String.IsNullOrEmpty ccBuildLabel) then CCNet
        elif not (String.IsNullOrEmpty travisBuildNumber) then Travis
        elif not (String.IsNullOrEmpty appVeyorBuildVersion) then AppVeyor
        elif isGitlabCI then GitLabCI
        elif isGitHubActionsBuild then GitHubActions
        elif isTFBuild then TeamFoundation
        elif isBambooBuild then Bamboo
        elif Environment.hasEnvironVar "BITBUCKET_COMMIT" then BitbucketPipelines
        else LocalBuild

    /// <summary>
    /// The current build version as detected from the current build server.
    /// </summary>
    let buildVersion =
        let getVersion = Environment.environVarOrDefault "buildVersion"
        match buildServer with
        | Jenkins -> getVersion jenkinsBuildNumber
        | TeamCity -> getVersion tcBuildNumber
        | CCNet -> getVersion ccBuildLabel
        | Travis -> getVersion travisBuildNumber
        | AppVeyor -> getVersion appVeyorBuildVersion
        | GitLabCI -> getVersion gitlabCIBuildNumber
        | GitHubActions -> getVersion gitHubActionsBuildNumber
        | TeamFoundation -> getVersion tfBuildNumber
        | Bamboo -> getVersion bambooBuildNumber
        | LocalBuild -> getVersion localBuildLabel
        | BitbucketPipelines -> getVersion ""

    /// <summary>
    /// Is true when the current build is a local build.
    /// </summary>
    let isLocalBuild = LocalBuild = buildServer

    let mutable ansiColorSupport =
        match buildServer with
        | Jenkins -> false
        | TeamCity -> true
        | CCNet -> false
        | Travis -> true
        | AppVeyor -> true
        | GitLabCI -> true
        | GitHubActions -> true
        | TeamFoundation -> false
        | Bamboo -> false
        | BitbucketPipelines -> false
        | LocalBuild -> false


    /// <summary>
    /// Install given list of build servers
    /// </summary>
    ///
    /// <param name="servers">The list of build servers to install</param>
    let install (servers: BuildServerInstaller list) =
        servers
        |> List.iter (fun f ->
            if f.Detect() then
                f.Install())
